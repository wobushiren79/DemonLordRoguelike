using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Mod 构建工具窗口：只打开主项目即可一键完成 MOD 项目的构建与部署——
/// ·「选择 Mod」下拉自动扫描 MOD 项目：已构建产物(Mods/*) + 构建器脚本(Assets/Editor/*ModBuilder.cs)，
///   选中即填入名称与构建方法(约定 {Mod名}ModBuilder.BuildMod，可按 Mod 单独覆盖持久化)；「＋新建 Mod」走手动输入(方法随名自动推导)；
/// · 操作区分组：一键流程(⚡ 构建→导出配置→整体部署) / 分步·构建(① 批量构建,产物 bundle+catalog) /
///   分步·配置(⓪ 仅导出配置+同步 JsonText,秒级 / ③ 仅移动 JsonText) / 分步·部署(② 导出配置+整体移动,含资源包)；
/// · 构建过程带进度条（按上次同 Mod 耗时渐近估算 + 构建日志里程碑跳档：分组同步完成 8% / 导出部署完成 97% / 构建完成 100%）。
/// 全程无需人工打开 MOD 项目编辑器。
/// </summary>
public class ModBuildEditorWindow : EditorWindow
{
    #region 字段

    /// <summary>Mod 项目路径的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyModProjectPath = "ModBuildEditorWindow.ModProjectPath";
    /// <summary>Mod 名称的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyModName = "ModBuildEditorWindow.ModName";
    /// <summary>构建方法的 EditorPrefs 键（手动模式用；选中已有 Mod 时按 Mod 名单独存）</summary>
    private const string EditorPrefsKeyBuildMethod = "ModBuildEditorWindow.BuildMethod";
    /// <summary>Unity.exe 覆盖路径的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyUnityExeOverride = "ModBuildEditorWindow.UnityExeOverride";

    /// <summary>扫描到的已知 Mod 信息（MOD 项目产物 + 构建器 + 主项目部署状态）</summary>
    private class KnownMod
    {
        /// <summary>Mod 名（= Mods 下的目录名）</summary>
        public string name;
        /// <summary>产物已构建（Mods/&lt;name&gt;/catalog.bin 存在）</summary>
        public bool hasProducts;
        /// <summary>存在构建器脚本（Assets/Editor 下 &lt;name&gt;ModBuilder.cs）</summary>
        public bool hasBuilder;
        /// <summary>已部署到主项目（主项目 Mods/&lt;name&gt; 目录存在）</summary>
        public bool deployedToMain;
    }

    /// <summary>MOD 项目路径（Unity 工程根目录）</summary>
    private string modProjectPath = "";
    /// <summary>Mod 名（= Mods 下的目录名）</summary>
    private string modName = "AeonsEchoSpine";
    /// <summary>MOD 项目侧执行的静态构建方法（-executeMethod 参数）</summary>
    private string buildMethod = "AeonsEchoSpineModBuilder.BuildMod";
    /// <summary>Unity.exe 覆盖路径（空=按 MOD 项目版本自动定位）</summary>
    private string unityExeOverride = "";

    /// <summary>扫描到的已知 Mod 列表（产物目录 ∪ 构建器脚本，按名排序）</summary>
    private readonly List<KnownMod> knownMods = new List<KnownMod>();
    /// <summary>当前选中的已知 Mod 索引（-1=新建/手动输入模式）</summary>
    private int selectedKnownModIndex = -1;
    /// <summary>构建方法是否仍与 Mod 名保持自动推导（用户手改后脱离）</summary>
    private bool buildMethodAutoDerived = true;

    /// <summary>批量构建进程（null=空闲）</summary>
    private Process buildProcess;
    /// <summary>构建开始时间（耗时显示用）</summary>
    private DateTime buildStartTime;
    /// <summary>批量构建日志文件路径</summary>
    private string buildLogPath;
    /// <summary>构建成功后是否继续执行导出+移动（一键模式）</summary>
    private bool isOneClickPending;

    /// <summary>构建日志已读取的字节位置（增量读取里程碑用）</summary>
    private long buildLogReadPos;
    /// <summary>日志里程碑抬升的进度下限（0~1，只升不降）</summary>
    private float buildStageFloor;
    /// <summary>当前构建阶段描述（取自最新命中的里程碑）</summary>
    private string buildStageText = "";
    /// <summary>当前构建进度（0~1，GUI 读取）</summary>
    private float buildProgress;

    /// <summary>构建日志里程碑 → 进度下限与阶段描述（子串匹配，两个 Mod 构建器共用同一套措辞）</summary>
    private static readonly (string mark, float floor, string stage)[] BuildLogMilestones =
    {
        ("分组条目同步完成", 0.08f, "Addressables 分组同步完成，构建资源包…"),
        ("自动导出+部署完成", 0.97f, "导出配置+部署完成，收尾中…"),
        ("构建完成：", 1.0f, "构建完成"),
    };

    /// <summary>上次构建耗时的 EditorPrefs 键（按 Mod 存，用于时间法估算进度）</summary>
    private static string GetLastBuildSecondsKey(string name)
    {
        return $"ModBuildEditorWindow.LastBuildSeconds.{name}";
    }

    /// <summary>状态/日志文本（追加式，超长截头）</summary>
    private string logText = "";
    /// <summary>日志区滚动位置</summary>
    private Vector2 scrollLog;

    #endregion

    #region 窗口入口

    [MenuItem("游戏/Mod构建工具")]
    public static void ShowWindow()
    {
        GetWindow<ModBuildEditorWindow>("Mod构建工具", typeof(SceneView)).minSize = new Vector2(460, 540);
    }

    private void OnEnable()
    {
        modProjectPath = EditorPrefs.GetString(EditorPrefsKeyModProjectPath, "");
        modName = EditorPrefs.GetString(EditorPrefsKeyModName, "AeonsEchoSpine");
        buildMethod = EditorPrefs.GetString(EditorPrefsKeyBuildMethod, "AeonsEchoSpineModBuilder.BuildMod");
        unityExeOverride = EditorPrefs.GetString(EditorPrefsKeyUnityExeOverride, "");
        buildMethodAutoDerived = buildMethod == DeriveBuildMethod(modName);
        RefreshKnownMods();
    }

    private void OnDisable()
    {
        EditorApplication.update -= PollBuildProcess;
    }

    #endregion

    #region GUI绘制

    private void OnGUI()
    {
        // 标题
        GUILayout.Space(12);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        GUILayout.Label("Mod 构建工具", titleStyle);
        GUILayout.Label("只开主项目即可构建+部署 MOD 项目（批量模式）", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(12);

        DrawSectionBox("Mod 项目", () =>
        {
            EditorGUILayout.BeginHorizontal();
            DrawTextFieldWithSave("Mod 项目路径", "MOD 项目（Unity 工程）根目录", ref modProjectPath, EditorPrefsKeyModProjectPath);
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("选择 MOD 项目根目录", modProjectPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    modProjectPath = selected.Replace("\\", "/");
                    EditorPrefs.SetString(EditorPrefsKeyModProjectPath, modProjectPath);
                    RefreshKnownMods();
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawModSelector();

            // Unity.exe 定位显示
            string unityExe = ResolveUnityExe(out string resolveMsg);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Unity.exe", "按 MOD 项目 ProjectVersion 自动定位；不一致时手动覆盖"));
            GUILayout.Label(unityExe ?? $"⚠ {resolveMsg}", EditorStyles.miniLabel, GUILayout.MinHeight(18));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("手动指定 Unity.exe", GUILayout.Width(130)))
            {
                string selected = EditorUtility.OpenFilePanel("选择 MOD 项目版本对应的 Unity.exe", "", "exe");
                if (!string.IsNullOrEmpty(selected))
                {
                    unityExeOverride = selected.Replace("\\", "/");
                    EditorPrefs.SetString(EditorPrefsKeyUnityExeOverride, unityExeOverride);
                }
            }
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(unityExeOverride));
            if (GUILayout.Button("清除覆盖", GUILayout.Width(70)))
            {
                unityExeOverride = "";
                EditorPrefs.SetString(EditorPrefsKeyUnityExeOverride, "");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        });

        GUILayout.Space(12);

        DrawSectionBox("操作", () =>
        {
            bool isBuilding = buildProcess != null;
            EditorGUI.BeginDisabledGroup(isBuilding);

            // 一键流程
            DrawMiniHeader("一键流程（① 构建 → ② 导出配置+整体部署）");
            GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
            if (GUILayout.Button(new GUIContent("⚡ 一键构建 + 移动", "构建成功后自动执行导出配置+整体移动"), GUILayout.Height(36)))
            {
                StartBuild(true);
            }

            // 分步 · 构建
            GUILayout.Space(8);
            DrawMiniHeader("分步 · 构建（MOD 项目批量构建，产物 bundle+catalog，耗时可达数分钟）");
            GUI.backgroundColor = new Color(0.40f, 0.70f, 0.90f);
            if (GUILayout.Button(new GUIContent("① 构建 Mod 资源", "批量模式调起 MOD 项目 Unity 执行构建方法"), GUILayout.Height(26)))
            {
                StartBuild(false);
            }

            // 分步 · 配置
            GUILayout.Space(8);
            DrawMiniHeader("分步 · 配置（Excel→JsonText，秒级，不动资源包）");
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.65f);
            if (GUILayout.Button(new GUIContent("⓪ 仅导出配置 + 同步 JsonText", "重新导出 JsonText 并只同步 JsonText 目录"), GUILayout.Height(24)))
            {
                RunExportOnly();
            }
            GUI.backgroundColor = new Color(0.65f, 0.80f, 0.60f);
            if (GUILayout.Button(new GUIContent("③ 仅移动 JsonText（不导出）", "纯拷贝 MOD 项目侧 JsonText 到本项目"), GUILayout.Height(24)))
            {
                MoveJsonTextOnly();
            }
            EditorGUILayout.EndHorizontal();

            // 分步 · 部署
            GUILayout.Space(8);
            DrawMiniHeader("分步 · 部署（产物整体覆盖到主项目 Mods/，含资源包）");
            GUI.backgroundColor = new Color(0.85f, 0.60f, 0.90f);
            if (GUILayout.Button(new GUIContent("② 导出配置 + 整体移动 Mod", "重新导出 JsonText 并整体覆盖部署（含 bundle+catalog）"), GUILayout.Height(26)))
            {
                RunExportAndMove();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // 构建中状态与取消
            if (isBuilding)
            {
                Rect barRect = GUILayoutUtility.GetRect(18, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(barRect, buildProgress, $"构建中 {buildProgress * 100f:F0}% · {buildStageText} · 已耗时 {(DateTime.Now - buildStartTime):mm\\:ss}");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("进度按上次该 Mod 构建耗时估算，日志里程碑到点会自动跳档", EditorStyles.miniLabel);
                if (GUILayout.Button("取消构建", GUILayout.Width(80)))
                {
                    CancelBuild();
                }
                EditorGUILayout.EndHorizontal();
            }
        });

        GUILayout.Space(12);

        DrawSectionBox("状态 / 日志", () =>
        {
            scrollLog = EditorGUILayout.BeginScrollView(scrollLog, GUILayout.MinHeight(120));
            EditorGUILayout.SelectableLabel(logText, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空日志", GUILayout.Width(70)))
            {
                logText = "";
            }
            EditorGUILayout.EndHorizontal();
        });
    }

    /// <summary>
    /// 绘制 Mod 选择器：已扫描到的 Mod 下拉直选（含构建/部署状态行），「＋新建 Mod」切手动输入模式
    /// </summary>
    private void DrawModSelector()
    {
        EditorGUILayout.BeginHorizontal();
        int displayIndex = selectedKnownModIndex >= 0 ? selectedKnownModIndex : knownMods.Count;
        string[] options = new string[knownMods.Count + 1];
        for (int i = 0; i < knownMods.Count; i++)
        {
            options[i] = knownMods[i].hasProducts ? $"{knownMods[i].name}（已构建）" : $"{knownMods[i].name}（未构建）";
        }
        options[knownMods.Count] = "＋ 新建 Mod（手动输入）";
        int newIndex = EditorGUILayout.Popup(new GUIContent("选择 Mod", "已构建/带构建器的 Mod 直接选中即可再构建；列表没有的走「新建」手动输入"), displayIndex, options);
        if (newIndex != displayIndex)
        {
            OnModSelected(newIndex);
        }
        if (GUILayout.Button(new GUIContent("刷新", "重新扫描 MOD 项目的产物目录与构建器脚本"), GUILayout.Width(46)))
        {
            RefreshKnownMods();
        }
        EditorGUILayout.EndHorizontal();

        if (selectedKnownModIndex >= 0)
        {
            // 已有 Mod：名称只读，状态一览，构建方法可按 Mod 覆盖
            KnownMod mod = knownMods[selectedKnownModIndex];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("状态");
            GUILayout.Label($"{(mod.hasProducts ? "✔ 产物已构建" : "✗ 未构建产物")} · {(mod.hasBuilder ? "✔ 构建器" : "✗ 无构建器")} · {(mod.deployedToMain ? "✔ 已部署主项目" : "✗ 未部署")}", EditorStyles.miniLabel, GUILayout.MinHeight(18));
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(new GUIContent("Mod 名称", "Mods 下的目录名，主游戏按此名加载"), modName);
            EditorGUI.EndDisabledGroup();
            string newMethod = EditorGUILayout.TextField(new GUIContent("构建方法", "批量模式 -executeMethod 调用的静态方法（类名.方法名），按 Mod 名单独持久化"), buildMethod);
            if (newMethod != buildMethod)
            {
                OnBuildMethodEdited(newMethod);
            }
        }
        else
        {
            // 新建模式：手动输入名称，构建方法随名自动推导（手改后脱离）
            string newName = EditorGUILayout.TextField(new GUIContent("Mod 名称", "Mods 下的目录名，主游戏按此名加载"), modName);
            if (newName != modName)
            {
                OnModNameEdited(newName);
            }
            string methodTooltip = buildMethodAutoDerived
                ? "按约定 {Mod名}ModBuilder.BuildMod 自动推导（手动修改后脱离推导）"
                : "手动指定中（把值改回 {Mod名}ModBuilder.BuildMod 可恢复自动推导）";
            string newMethod = EditorGUILayout.TextField(new GUIContent("构建方法", methodTooltip), buildMethod);
            if (newMethod != buildMethod)
            {
                OnBuildMethodEdited(newMethod);
            }
        }
    }

    /// <summary>
    /// 绘制文本输入项：值变化时立即写入 EditorPrefs 持久化
    /// </summary>
    private void DrawTextFieldWithSave(string label, string tooltip, ref string value, string prefsKey)
    {
        string newValue = EditorGUILayout.TextField(new GUIContent(label, tooltip), value);
        if (newValue != value)
        {
            value = newValue;
            EditorPrefs.SetString(prefsKey, value);
        }
    }

    /// <summary>
    /// 绘制操作组内的小节标题
    /// </summary>
    private void DrawMiniHeader(string text)
    {
        GUILayout.Label(text, EditorStyles.miniBoldLabel);
    }

    /// <summary>
    /// 绘制分组框（与 GameBuildEditorWindow 风格一致）
    /// </summary>
    private void DrawSectionBox(string header, System.Action content)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            GUILayout.Space(8);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(8, 8, 0, 4)
            };
            GUILayout.Label(header, headerStyle);
            GUILayout.Space(4);
            Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.4f));
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();
            content?.Invoke();
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 追加状态日志（带时间戳，超长截头防膨胀）
    /// </summary>
    private void AppendLog(string message)
    {
        logText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        if (logText.Length > 12000)
        {
            logText = logText.Substring(logText.Length - 9000);
        }
        Repaint();
    }

    #endregion

    #region Mod 扫描与选择

    /// <summary>
    /// 按约定推导构建方法名：{Mod名}ModBuilder.BuildMod
    /// </summary>
    private static string DeriveBuildMethod(string name)
    {
        return $"{name}ModBuilder.BuildMod";
    }

    /// <summary>
    /// 单个 Mod 的构建方法覆盖 EditorPrefs 键（选中已有 Mod 时按此存取）
    /// </summary>
    private static string GetPerModBuildMethodKey(string name)
    {
        return $"ModBuildEditorWindow.BuildMethod.{name}";
    }

    /// <summary>
    /// 重新扫描 MOD 项目的已知 Mod：已构建产物目录(Mods/*) ∪ 构建器脚本(Assets/Editor/*ModBuilder.cs)，并标注主项目部署状态
    /// </summary>
    private void RefreshKnownMods()
    {
        knownMods.Clear();
        if (!string.IsNullOrEmpty(modProjectPath) && Directory.Exists(modProjectPath))
        {
            // 已构建产物：Mods/<name>（含 catalog.bin 才算构建完整）
            string modsDir = Path.Combine(modProjectPath, "Mods");
            if (Directory.Exists(modsDir))
            {
                foreach (string dir in Directory.GetDirectories(modsDir))
                {
                    GetOrAddKnownMod(Path.GetFileName(dir)).hasProducts = File.Exists(Path.Combine(dir, "catalog.bin"));
                }
            }
            // 可构建构建器：Assets/Editor 下 <name>ModBuilder.cs（约定命名，未构建过也可选中）
            string editorDir = Path.Combine(modProjectPath, "Assets", "Editor");
            if (Directory.Exists(editorDir))
            {
                foreach (string file in Directory.GetFiles(editorDir, "*ModBuilder.cs", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string name = fileName.Substring(0, fileName.Length - "ModBuilder".Length);
                    if (name.Length > 0)
                    {
                        GetOrAddKnownMod(name).hasBuilder = true;
                    }
                }
            }
        }
        // 主项目部署状态
        string mainModsDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Mods");
        foreach (KnownMod mod in knownMods)
        {
            mod.deployedToMain = Directory.Exists(Path.Combine(mainModsDir, mod.name));
        }
        knownMods.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        SyncSelectionFromModName();
        Repaint();
    }

    /// <summary>
    /// 按名取已知 Mod，不存在则新建加入
    /// </summary>
    private KnownMod GetOrAddKnownMod(string name)
    {
        KnownMod mod = knownMods.Find(m => m.name == name);
        if (mod == null)
        {
            mod = new KnownMod { name = name };
            knownMods.Add(mod);
        }
        return mod;
    }

    /// <summary>
    /// 按当前 modName 同步选中态（名称命中已知 Mod 则选中，否则落回手动输入模式）
    /// </summary>
    private void SyncSelectionFromModName()
    {
        selectedKnownModIndex = knownMods.FindIndex(m => m.name == modName);
        buildMethodAutoDerived = buildMethod == DeriveBuildMethod(modName);
    }

    /// <summary>
    /// 下拉选择变化：选中已有 Mod 即填入名称与构建方法（该 Mod 的覆盖值 &gt; 约定推导）；选「新建」切手动输入模式
    /// </summary>
    private void OnModSelected(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= knownMods.Count)
        {
            // 新建/手动输入模式：保留当前输入，仅切换编辑态
            selectedKnownModIndex = -1;
            return;
        }
        selectedKnownModIndex = optionIndex;
        modName = knownMods[optionIndex].name;
        EditorPrefs.SetString(EditorPrefsKeyModName, modName);
        string perMod = EditorPrefs.GetString(GetPerModBuildMethodKey(modName), "");
        buildMethod = !string.IsNullOrEmpty(perMod) ? perMod : DeriveBuildMethod(modName);
        buildMethodAutoDerived = buildMethod == DeriveBuildMethod(modName);
    }

    /// <summary>
    /// 手动模式编辑 Mod 名：名称持久化，构建方法处于自动推导态时随名更新
    /// </summary>
    private void OnModNameEdited(string newName)
    {
        modName = newName;
        EditorPrefs.SetString(EditorPrefsKeyModName, modName);
        if (buildMethodAutoDerived)
        {
            buildMethod = DeriveBuildMethod(modName);
            EditorPrefs.SetString(EditorPrefsKeyBuildMethod, buildMethod);
        }
    }

    /// <summary>
    /// 编辑构建方法：已有 Mod 按名单独持久化，手动模式写全局键；改回约定值即恢复自动推导
    /// </summary>
    private void OnBuildMethodEdited(string newMethod)
    {
        buildMethod = newMethod;
        buildMethodAutoDerived = buildMethod == DeriveBuildMethod(modName);
        if (selectedKnownModIndex >= 0)
        {
            EditorPrefs.SetString(GetPerModBuildMethodKey(modName), buildMethod);
        }
        else
        {
            EditorPrefs.SetString(EditorPrefsKeyBuildMethod, buildMethod);
        }
    }

    #endregion

    #region Unity.exe 定位

    /// <summary>
    /// 定位 MOD 项目对应的 Unity.exe：手动覆盖 → 版本与当前编辑器一致用当前 exe → Hub 标准路径
    /// </summary>
    /// <param name="failMsg">定位失败原因</param>
    /// <returns>exe 完整路径；定位失败返回 null</returns>
    private string ResolveUnityExe(out string failMsg)
    {
        failMsg = null;
        if (!string.IsNullOrEmpty(unityExeOverride))
        {
            if (File.Exists(unityExeOverride)) return unityExeOverride;
            failMsg = "覆盖的 Unity.exe 不存在，请重新指定";
            return null;
        }
        string version = ReadModProjectVersion();
        if (version == null)
        {
            failMsg = "读不到 MOD 项目版本（ProjectVersion.txt）";
            return null;
        }
        // 版本与当前编辑器一致时直接用当前 exe（主项目本机最可靠的定位）
        if (version == Application.unityVersion && File.Exists(EditorApplication.applicationPath))
        {
            return EditorApplication.applicationPath;
        }
        // Hub 标准安装路径
        string hubExe = $"C:/Program Files/Unity/Hub/Editor/{version}/Editor/Unity.exe";
        if (File.Exists(hubExe)) return hubExe;
        failMsg = $"按版本 {version} 找不到 Unity.exe，请手动指定";
        return null;
    }

    /// <summary>
    /// 读取 MOD 项目的 Unity 版本（ProjectSettings/ProjectVersion.txt）
    /// </summary>
    /// <returns>版本号（如 6000.3.11f1）；读取失败返回 null</returns>
    private string ReadModProjectVersion()
    {
        if (string.IsNullOrEmpty(modProjectPath)) return null;
        string versionFile = Path.Combine(modProjectPath, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFile)) return null;
        foreach (string line in File.ReadAllLines(versionFile))
        {
            if (line.StartsWith("m_EditorVersion:"))
            {
                return line.Substring("m_EditorVersion:".Length).Trim();
            }
        }
        return null;
    }

    #endregion

    #region ① 批量构建

    /// <summary>
    /// 启动批量构建：用 MOD 项目版本的 Unity 以 -batchmode -executeMethod 执行构建方法，异步等待完成
    /// </summary>
    /// <param name="isOneClick">true=一键模式（构建成功后自动执行导出+移动）</param>
    private void StartBuild(bool isOneClick)
    {
        // 校验 Mod 项目
        if (string.IsNullOrEmpty(modProjectPath) || !Directory.Exists(Path.Combine(modProjectPath, "Assets")))
        {
            AppendLog("✗ Mod 项目路径无效（未找到 Assets 目录）");
            return;
        }
        // 选中的已有 Mod 缺构建器脚本时提示（构建方法可能不存在于 MOD 项目）
        if (selectedKnownModIndex >= 0 && !knownMods[selectedKnownModIndex].hasBuilder)
        {
            AppendLog($"⚠ 未在 MOD 项目 Assets/Editor 找到 {modName}ModBuilder.cs，请确认构建方法 {buildMethod} 存在");
        }
        // 校验 Unity.exe
        string unityExe = ResolveUnityExe(out string resolveMsg);
        if (unityExe == null)
        {
            AppendLog($"✗ Unity.exe 定位失败：{resolveMsg}");
            return;
        }
        // 同一项目不能被两个编辑器实例同时打开（项目锁）
        string lockFile = Path.Combine(modProjectPath, "Temp", "UnityLockfile");
        if (File.Exists(lockFile))
        {
            AppendLog("✗ MOD 项目似乎正被另一个 Unity 实例打开（存在 Temp/UnityLockfile），请先关闭它再构建");
            return;
        }

        buildLogPath = Path.Combine(Path.GetTempPath(), "ModBuildEditorWindow_build.log");
        string arguments = $"-batchmode -projectPath \"{modProjectPath}\" -executeMethod {buildMethod} -quit -logFile \"{buildLogPath}\"";
        AppendLog($"▶ 开始批量构建：{buildMethod}\n    {unityExe} {arguments}");
        if (isOneClick)
        {
            AppendLog("（一键模式：构建成功后将自动执行 ② 导出配置+移动）");
        }

        // 进度状态复位（时间法估算 + 日志里程碑跳档）
        buildLogReadPos = 0;
        buildStageFloor = 0;
        buildProgress = 0;
        buildStageText = "启动 MOD 项目 Unity…";

        var startInfo = new ProcessStartInfo(unityExe, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        buildProcess = Process.Start(startInfo);
        buildStartTime = DateTime.Now;
        isOneClickPending = isOneClick;
        EditorApplication.update += PollBuildProcess;
    }

    /// <summary>
    /// 更新构建进度：增量读构建日志命中的里程碑抬升进度下限（Unity 以共享写占用日志，需 FileShare.ReadWrite）；
    /// 主进度按上次同 Mod 构建耗时渐近估算（不触顶，完成时置满）
    /// </summary>
    private void UpdateBuildProgress()
    {
        try
        {
            if (!string.IsNullOrEmpty(buildLogPath) && File.Exists(buildLogPath))
            {
                long length = new FileInfo(buildLogPath).Length;
                if (length > buildLogReadPos)
                {
                    using (var fs = new FileStream(buildLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(buildLogReadPos, SeekOrigin.Begin);
                        using (var reader = new StreamReader(fs))
                        {
                            string chunk = reader.ReadToEnd();
                            buildLogReadPos = fs.Position;
                            foreach (var (mark, floor, stage) in BuildLogMilestones)
                            {
                                if (chunk.Contains(mark) && floor > buildStageFloor)
                                {
                                    buildStageFloor = floor;
                                    buildStageText = stage;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { /* 日志读取失败不影响构建与进度显示 */ }

        // 时间估算：p = elapsed/(elapsed+0.35×估计耗时)，到估计耗时约 74%，渐近 96% 不触顶
        float estimated = EditorPrefs.GetFloat(GetLastBuildSecondsKey(modName), 180f);
        double elapsed = (DateTime.Now - buildStartTime).TotalSeconds;
        float timeProgress = (float)(elapsed / (elapsed + estimated * 0.35)) * 0.96f;
        buildProgress = Mathf.Max(Mathf.Min(timeProgress, 0.97f), buildStageFloor);
    }

    /// <summary>
    /// 轮询批量构建进程（EditorApplication.update 驱动）：完成后读取日志、按结果继续一键流程并刷新 Mod 列表
    /// </summary>
    private void PollBuildProcess()
    {
        if (buildProcess == null) return;
        UpdateBuildProgress();
        Repaint();// 刷新进度条与耗时显示
        if (!buildProcess.HasExited) return;

        int exitCode = buildProcess.ExitCode;
        buildProcess.Dispose();
        buildProcess = null;
        EditorApplication.update -= PollBuildProcess;
        buildProgress = 1;
        double buildSeconds = (DateTime.Now - buildStartTime).TotalSeconds;
        // 构建结束刷新 Mod 列表（新 Mod 产物出现后可直接选中）
        RefreshKnownMods();

        string logTail = ReadLogTail(buildLogPath, 30);
        if (exitCode == 0)
        {
            // 记住本次耗时，供下次时间法估算进度
            EditorPrefs.SetFloat(GetLastBuildSecondsKey(modName), (float)buildSeconds);
            AppendLog($"✔ 批量构建完成（耗时 {(DateTime.Now - buildStartTime):mm\\:ss}）");
            if (!string.IsNullOrEmpty(logTail))
            {
                AppendLog($"构建日志尾：\n{logTail}");
            }
            if (isOneClickPending)
            {
                isOneClickPending = false;
                RunExportAndMove();
            }
        }
        else
        {
            isOneClickPending = false;
            AppendLog($"✗ 批量构建失败（exit={exitCode}）" + (string.IsNullOrEmpty(logTail) ? "" : $"\n日志尾：\n{logTail}"));
        }
    }

    /// <summary>
    /// 取消进行中的批量构建（杀掉进程）
    /// </summary>
    private void CancelBuild()
    {
        if (buildProcess == null) return;
        try { buildProcess.Kill(); } catch { }
        AppendLog("⚠ 已取消批量构建");
    }

    /// <summary>
    /// 读取日志文件末尾若干行（过滤空行）
    /// </summary>
    private string ReadLogTail(string logPath, int lineCount)
    {
        try
        {
            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath)) return "";
            string[] lines = File.ReadAllLines(logPath);
            int start = Math.Max(0, lines.Length - lineCount);
            return string.Join("\n", lines, start, lines.Length - start).Trim();
        }
        catch (Exception e)
        {
            return $"(读日志失败: {e.Message})";
        }
    }

    #endregion

    #region ② 导出配置 + 移动

    /// <summary>
    /// 导出配置并移动 Mod 到本项目：优先跑主项目生成脚本（export --deploy-main，重新导出 JsonText 再整体覆盖部署）；
    /// 脚本缺失时退化为纯目录拷贝（要求产物含 catalog.bin）
    /// </summary>
    private void RunExportAndMove()
    {
        if (string.IsNullOrEmpty(modProjectPath) || !Directory.Exists(modProjectPath))
        {
            AppendLog("✗ Mod 项目路径无效");
            return;
        }
        string mainRoot = Directory.GetParent(Application.dataPath).FullName;
        string modOutputDir = Path.Combine(modProjectPath, "Mods", modName);
        if (!Directory.Exists(modOutputDir))
        {
            AppendLog($"✗ 找不到 Mod 产物目录：{modOutputDir}（请先执行 ① 构建）");
            return;
        }
        if (!File.Exists(Path.Combine(modOutputDir, "catalog.bin")))
        {
            AppendLog("✗ 产物缺少 catalog.bin（请先执行 ① 构建）");
            return;
        }

        string runPythonPs1 = Path.Combine(mainRoot, ".claude/scripts/run-python.ps1");
        string exportPy = GetGenScriptPath(mainRoot);
        if (File.Exists(runPythonPs1) && File.Exists(exportPy))
        {
            RunPythonExportAndDeploy(mainRoot, runPythonPs1, exportPy);
        }
        else
        {
            AppendLog("（未找到主项目导出脚本，退化为纯目录拷贝）");
            CopyModToMainProject(modOutputDir, Path.Combine(mainRoot, "Mods", modName));
        }
        RefreshKnownMods();
    }

    /// <summary>
    /// 按 Mod 名推导主项目生成脚本路径：gen_{modName去Spine后缀小写}_spine_mod.py
    /// （AeonsEchoSpine→gen_aeonsecho_spine_mod.py；ArkReSpine→gen_arkre_spine_mod.py；非 Spine 结尾的 Mod 名直接用全名小写）
    /// </summary>
    private string GetGenScriptPath(string mainRoot)
    {
        string key = modName.EndsWith("Spine") ? modName.Substring(0, modName.Length - "Spine".Length) : modName;
        return Path.Combine(mainRoot, $".claude/scripts/gen_{key.ToLower()}_spine_mod.py");
    }

    /// <summary>
    /// 经 run-python.ps1 跑生成脚本（同步, 180s 超时），过程与结果写入状态日志
    /// </summary>
    /// <param name="scriptArgs">传给生成脚本的参数（如 export --mod-project "..." [--deploy-main "..."]）</param>
    /// <param name="actionDesc">动作描述（状态日志用）</param>
    /// <returns>是否执行成功</returns>
    private bool RunPythonGenScript(string scriptArgs, string actionDesc)
    {
        string mainRoot = Directory.GetParent(Application.dataPath).FullName;
        string runPythonPs1 = Path.Combine(mainRoot, ".claude/scripts/run-python.ps1");
        string exportPy = GetGenScriptPath(mainRoot);
        if (!File.Exists(runPythonPs1) || !File.Exists(exportPy))
        {
            AppendLog($"✗ 未找到主项目导出脚本：{exportPy}");
            return false;
        }
        AppendLog($"▶ {actionDesc}…");
        string arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{runPythonPs1}\" \"{exportPy}\" {scriptArgs}";
        var startInfo = new ProcessStartInfo("powershell.exe", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            //python 侧经 run-python.ps1 输出 UTF-8（PYTHONUTF8=1），按 UTF-8 解码防中文乱码
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using (var process = Process.Start(startInfo))
        {
            //先读尽两个流再等退出（防管道缓冲填满死锁）
            string stdOut = process.StandardOutput.ReadToEnd();
            string stdErr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(180000))
            {
                try { process.Kill(); } catch { }
                AppendLog($"✗ {actionDesc}超时已终止");
                return false;
            }
            if (process.ExitCode != 0)
            {
                AppendLog($"✗ {actionDesc}失败（exit={process.ExitCode}）：\n{stdOut}\n{stdErr}");
                return false;
            }
            AppendLog($"✔ {stdOut.Trim()}");
            return true;
        }
    }

    /// <summary>
    /// ⓪ 仅导出配置：跑生成脚本 export（不部署）重建 JsonText 后，只把 JsonText 目录拷贝到本项目 Mod 目录（不构建、不动 bundle，秒级）
    /// </summary>
    private void RunExportOnly()
    {
        if (string.IsNullOrEmpty(modProjectPath) || !Directory.Exists(modProjectPath))
        {
            AppendLog("✗ Mod 项目路径无效");
            return;
        }
        bool success = RunPythonGenScript($"export --mod-project \"{modProjectPath}\"", "仅导出配置（Excel→JsonText）");
        if (success)
        {
            CopyJsonTextToMainProject();
        }
    }

    /// <summary>
    /// 仅移动 JsonText：不导出，纯把 MOD 项目侧的 JsonText 目录覆盖拷贝到本项目 Mod 目录（已导出过、只想快速同步时用）
    /// </summary>
    private void MoveJsonTextOnly()
    {
        if (string.IsNullOrEmpty(modProjectPath) || !Directory.Exists(modProjectPath))
        {
            AppendLog("✗ Mod 项目路径无效");
            return;
        }
        CopyJsonTextToMainProject();
    }

    /// <summary>
    /// 把 MOD 项目侧的 JsonText 目录覆盖拷贝到本项目 Mod 目录（先删旧目录防残留失效行）
    /// </summary>
    private void CopyJsonTextToMainProject()
    {
        string mainRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourceJsonText = Path.Combine(modProjectPath, "Mods", modName, "JsonText");
        string targetJsonText = Path.Combine(mainRoot, "Mods", modName, "JsonText");
        if (!Directory.Exists(sourceJsonText))
        {
            AppendLog($"✗ 找不到 JsonText 目录：{sourceJsonText}（请先导出配置）");
            return;
        }
        try
        {
            if (Directory.Exists(targetJsonText))
            {
                Directory.Delete(targetJsonText, true);
            }
            CopyDirectory(sourceJsonText, targetJsonText);
            AppendLog($"✔ JsonText 已同步到：{targetJsonText}");
        }
        catch (Exception e)
        {
            AppendLog($"✗ JsonText 拷贝失败：{e.Message}");
        }
    }

    /// <summary>
    /// 经 run-python.ps1 跑生成脚本 export --deploy-main（导出 JsonText + 整体覆盖部署到本项目）
    /// </summary>
    private void RunPythonExportAndDeploy(string mainRoot, string runPythonPs1, string exportPy)
    {
        RunPythonGenScript($"export --mod-project \"{modProjectPath}\" --deploy-main \"{mainRoot}\"", "导出配置并部署（export --deploy-main）");
    }

    /// <summary>
    /// 纯目录拷贝：先删本项目旧 Mod 目录，再整体复制产物目录
    /// </summary>
    private void CopyModToMainProject(string sourceDir, string targetDir)
    {
        try
        {
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            CopyDirectory(sourceDir, targetDir);
            AppendLog($"✔ Mod 已移动到：{targetDir}");
        }
        catch (Exception e)
        {
            AppendLog($"✗ 移动失败：{e.Message}");
        }
    }

    /// <summary>
    /// 递归复制目录（含子目录与文件）
    /// </summary>
    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }
        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }

    #endregion
}
