using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 打包游戏工具窗口：打包前按勾选项执行资源生成（道具图标/皮肤图标/刷新图集，逻辑复用 GameResourceEditor），随后执行 BuildPlayer 打包
/// </summary>
public class GameBuildEditorWindow : EditorWindow
{
    #region 字段

    /// <summary>打包路径的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyBuildPath = "GameBuildEditorWindow.BuildPath";
    /// <summary>开发包的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyDevelopment = "GameBuildEditorWindow.Development";
    /// <summary>脚本调试的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyAllowDebugging = "GameBuildEditorWindow.AllowDebugging";
    /// <summary>连接 Profiler 的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyConnectProfiler = "GameBuildEditorWindow.ConnectProfiler";
    /// <summary>深度分析的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyDeepProfiling = "GameBuildEditorWindow.DeepProfiling";
    /// <summary>自动运行的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyAutoRun = "GameBuildEditorWindow.AutoRun";
    /// <summary>完成后打开输出目录的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyShowBuiltPlayer = "GameBuildEditorWindow.ShowBuiltPlayer";
    /// <summary>开启 GM 模式的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyEnableGMMode = "GameBuildEditorWindow.EnableGMMode";
    /// <summary>打包后复制 Mod 资源总开关的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyCopyMods = "GameBuildEditorWindow.CopyMods";
    /// <summary>单个 Mod 是否复制的 EditorPrefs 键前缀（完整键 = 前缀 + Mod 名）</summary>
    private const string EditorPrefsKeyPrefixCopyMod = "GameBuildEditorWindow.CopyMod.";

    /// <summary>打包用的游戏场景（正式包固定从该场景出包，与 Build Settings 里的日常测试场景解耦）</summary>
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    /// <summary>GM 模式开关写入的资源文件（打包前按选项写入 0/1，打包结束恢复 0）</summary>
    private const string GMModeAssetPath = "Assets/Resources/GMMode.txt";

    /// <summary>是否生成所有 Spine 道具图标</summary>
    private bool isGenItemIcons = true;

    /// <summary>是否生成所有 Spine 皮肤图标</summary>
    private bool isGenSkinIcons = true;

    /// <summary>是否刷新所有图集</summary>
    private bool isRefreshAtlases = true;

    /// <summary>打包输出目录</summary>
    private string buildPath;

    /// <summary>是否开发包（Development Build）</summary>
    private bool isDevelopment;

    /// <summary>是否允许脚本调试（需勾选开发包）</summary>
    private bool isAllowDebugging;

    /// <summary>是否自动连接 Profiler（需勾选开发包）</summary>
    private bool isConnectProfiler;

    /// <summary>是否启用深度分析（需勾选开发包）</summary>
    private bool isDeepProfiling;

    /// <summary>打包完成后是否自动运行</summary>
    private bool isAutoRun;

    /// <summary>打包完成后是否打开输出目录</summary>
    private bool isShowBuiltPlayer = true;

    /// <summary>是否开启 GM 模式（勾选后正式包中按 F12 也能打开 GM 测试面板，默认关闭）</summary>
    private bool isEnableGMMode;

    /// <summary>打包成功后是否把勾选的 Mod 资源复制到输出目录的 Mods 下</summary>
    private bool isCopyMods;

    /// <summary>项目 Mods 目录下扫描到的 Mod 名列表</summary>
    private readonly List<string> listModNames = new List<string>();

    /// <summary>各 Mod 是否勾选复制（键 = Mod 名，新增 Mod 默认不勾选）</summary>
    private readonly Dictionary<string, bool> dictModSelection = new Dictionary<string, bool>();

    /// <summary>Mod 列表滚动位置</summary>
    private Vector2 scrollPosModList;

    #endregion

    #region 窗口入口

    [MenuItem("游戏/打包游戏")]
    public static void ShowWindow()
    {
        GetWindow<GameBuildEditorWindow>("打包游戏", typeof(SceneView)).minSize = new Vector2(360, 480);
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(buildPath))
        {
            buildPath = EditorPrefs.GetString(EditorPrefsKeyBuildPath, GetDefaultBuildPath());
        }
        isDevelopment = EditorPrefs.GetBool(EditorPrefsKeyDevelopment, false);
        isAllowDebugging = EditorPrefs.GetBool(EditorPrefsKeyAllowDebugging, false);
        isConnectProfiler = EditorPrefs.GetBool(EditorPrefsKeyConnectProfiler, false);
        isDeepProfiling = EditorPrefs.GetBool(EditorPrefsKeyDeepProfiling, false);
        isAutoRun = EditorPrefs.GetBool(EditorPrefsKeyAutoRun, false);
        isShowBuiltPlayer = EditorPrefs.GetBool(EditorPrefsKeyShowBuiltPlayer, true);
        isEnableGMMode = EditorPrefs.GetBool(EditorPrefsKeyEnableGMMode, false);
        isCopyMods = EditorPrefs.GetBool(EditorPrefsKeyCopyMods, false);
        RefreshModList();
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
        GUILayout.Label("打包游戏工具", titleStyle);
        GUILayout.Space(16);

        DrawSectionBox("打包前执行（复用 游戏资源处理 工具）", () =>
        {
            isGenItemIcons = EditorGUILayout.Toggle(new GUIContent("生成所有 Spine 道具图标", "调用 GameResourceEditor.SpineAllItemInit"), isGenItemIcons);
            GUILayout.Space(4);
            isGenSkinIcons = EditorGUILayout.Toggle(new GUIContent("生成所有 Spine 皮肤图标", "调用 GameResourceEditor.SpineAllSkinInit"), isGenSkinIcons);
            GUILayout.Space(4);
            isRefreshAtlases = EditorGUILayout.Toggle(new GUIContent("刷新所有图集", "调用 GameResourceEditor.RefreshAllAtlases"), isRefreshAtlases);
        });

        GUILayout.Space(16);

        DrawSectionBox("打包选项", () =>
        {
            DrawOptionToggle("开发包（Development Build）", "勾选后可调试/分析，包体更大、性能更低", isDevelopment, EditorPrefsKeyDevelopment, v =>
            {
                isDevelopment = v;
                // 取消开发包时联动关闭依赖它的子选项
                if (!isDevelopment)
                {
                    isAllowDebugging = false;
                    isConnectProfiler = false;
                    isDeepProfiling = false;
                    EditorPrefs.SetBool(EditorPrefsKeyAllowDebugging, false);
                    EditorPrefs.SetBool(EditorPrefsKeyConnectProfiler, false);
                    EditorPrefs.SetBool(EditorPrefsKeyDeepProfiling, false);
                }
            });
            // 调试/分析类选项依赖开发包
            EditorGUI.BeginDisabledGroup(!isDevelopment);
            DrawOptionToggle("允许脚本调试", "BuildOptions.AllowDebugging", isAllowDebugging, EditorPrefsKeyAllowDebugging, v => isAllowDebugging = v);
            DrawOptionToggle("自动连接 Profiler", "BuildOptions.ConnectWithProfiler", isConnectProfiler, EditorPrefsKeyConnectProfiler, v => isConnectProfiler = v);
            DrawOptionToggle("深度分析（Deep Profiling）", "BuildOptions.EnableDeepProfilingSupport", isDeepProfiling, EditorPrefsKeyDeepProfiling, v => isDeepProfiling = v);
            EditorGUI.EndDisabledGroup();
            DrawOptionToggle("开启 GM 模式（F12 测试面板）", "勾选后正式包中按 F12 也能打开 GM 测试面板（默认关闭）", isEnableGMMode, EditorPrefsKeyEnableGMMode, v => isEnableGMMode = v);
            DrawOptionToggle("打包完成后自动运行", "BuildOptions.AutoRunPlayer", isAutoRun, EditorPrefsKeyAutoRun, v => isAutoRun = v);
            DrawOptionToggle("打包完成后打开输出目录", "BuildOptions.ShowBuiltPlayer", isShowBuiltPlayer, EditorPrefsKeyShowBuiltPlayer, v => isShowBuiltPlayer = v);
        });

        GUILayout.Space(16);

        DrawSectionBox("Mod 资源复制", () =>
        {
            DrawOptionToggle("打包后复制 Mod 资源到输出目录", "打包成功后，把勾选的 Mod 从项目 Mods 目录复制到输出目录的 Mods 下（先删后拷，与项目内保持一致）", isCopyMods, EditorPrefsKeyCopyMods, v => isCopyMods = v);
            EditorGUI.BeginDisabledGroup(!isCopyMods);
            // 工具行：全选/全不选/刷新列表
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(50)))
            {
                SetAllModSelection(true);
            }
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                SetAllModSelection(false);
            }
            if (GUILayout.Button("刷新列表", GUILayout.Width(70)))
            {
                RefreshModList();
            }
            EditorGUILayout.EndHorizontal();
            // Mod 勾选列表（新增 Mod 默认不勾选）
            if (listModNames.Count == 0)
            {
                EditorGUILayout.LabelField("（项目 Mods 目录下未找到任何 Mod）", EditorStyles.miniLabel);
            }
            else
            {
                scrollPosModList = EditorGUILayout.BeginScrollView(scrollPosModList, GUILayout.MaxHeight(150));
                foreach (var modName in listModNames)
                {
                    bool selected = dictModSelection.TryGetValue(modName, out bool value) && value;
                    bool newSelected = EditorGUILayout.ToggleLeft(modName, selected);
                    if (newSelected != selected)
                    {
                        dictModSelection[modName] = newSelected;
                        EditorPrefs.SetBool(EditorPrefsKeyPrefixCopyMod + modName, newSelected);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUI.EndDisabledGroup();
        });

        GUILayout.Space(16);

        DrawSectionBox("打包路径", () =>
        {
            EditorGUILayout.BeginHorizontal();
            buildPath = EditorGUILayout.TextField(new GUIContent("输出目录", "打包输出目录，默认为 git 仓库上级目录下的 DLR 文件夹"), buildPath);
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择打包输出目录", buildPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    buildPath = selectedPath;
                }
            }
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(buildPath) || !Directory.Exists(buildPath));
            if (GUILayout.Button("打开", GUILayout.Width(50)))
            {
                EditorUtility.RevealInFinder(buildPath);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("重置为默认路径（git 上级目录/DLR）"))
            {
                buildPath = GetDefaultBuildPath();
            }
        });

        GUILayout.FlexibleSpace();

        // 打包按钮
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(buildPath));
        if (GUILayout.Button("开始打包", GUILayout.Height(40)))
        {
            BuildGame();
        }
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = prev;
        GUILayout.Space(8);
    }

    /// <summary>
    /// 绘制一个打包选项开关：值变化时立即写入 EditorPrefs 持久化
    /// </summary>
    private void DrawOptionToggle(string label, string tooltip, bool value, string prefsKey, System.Action<bool> onChanged)
    {
        bool newValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), value);
        if (newValue != value)
        {
            onChanged?.Invoke(newValue);
            EditorPrefs.SetBool(prefsKey, newValue);
        }
    }

    /// <summary>
    /// 绘制分组框（与 GameResourceEditor 风格一致）
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

    #endregion

    #region 路径

    /// <summary>
    /// 获取默认打包路径：从项目根向上查找 git 仓库根目录，在其上一级目录下新建/使用 DLR 目录
    /// </summary>
    private static string GetDefaultBuildPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        DirectoryInfo gitRoot = FindGitRoot(new DirectoryInfo(projectRoot));
        // 找不到 .git 时退化为项目根的上级目录
        string baseDir = gitRoot != null ? gitRoot.Parent.FullName : Directory.GetParent(projectRoot).FullName;
        return Path.Combine(baseDir, "DLR");
    }

    /// <summary>
    /// 从指定目录向上查找包含 .git 的目录（即 git 仓库根）
    /// </summary>
    private static DirectoryInfo FindGitRoot(DirectoryInfo dir)
    {
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir;
            }
            dir = dir.Parent;
        }
        return null;
    }

    #endregion

    #region 打包逻辑

    /// <summary>
    /// 执行打包：先按勾选项生成 Spine 资源，再调用 BuildPlayer 打包
    /// </summary>
    private void BuildGame()
    {
        // URP 兼容模式缺 URP_COMPATIBILITY_MODE 宏时打包必失败，先补宏；补宏会触发脚本重编译中断后续流程，需重编译完成后重新打包
        if (EnsureURPCompatibilityModeDefine())
        {
            EditorUtility.DisplayDialog("打包游戏", "已自动为当前平台补充 URP_COMPATIBILITY_MODE 编译宏（URP 兼容模式打包必需）。\n脚本重编译完成后，请重新点击「开始打包」。", "确定");
            return;
        }

        if (string.IsNullOrEmpty(buildPath))
        {
            Debug.LogError("打包路径不能为空！");
            return;
        }
        EditorPrefs.SetString(EditorPrefsKeyBuildPath, buildPath);

        // 打包前自动切到 Game 场景：正式包固定从 GameScene 出包，避免当前打开/Build Settings 里配置的是 TestScene 时打出测试包
        if (!File.Exists(GameScenePath))
        {
            Debug.LogError($"找不到游戏场景：{GameScenePath}，无法打包！");
            return;
        }
        string prevScenePath = EditorSceneManager.GetActiveScene().path;
        // 当前场景有未保存修改时询问是否保存，点取消则中止打包
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            LogUtil.Log("已取消打包（场景存在未保存的修改）");
            return;
        }
        if (prevScenePath != GameScenePath)
        {
            LogUtil.Log($"打包前切换场景：{prevScenePath} -> {GameScenePath}");
            EditorSceneManager.OpenScene(GameScenePath);
        }

        // 打包前资源生成（全部成功后再打包）
        if (isGenItemIcons)
        {
            LogUtil.Log("========== 打包前：生成所有 Spine 道具图标 ==========");
            GameResourceEditor.SpineAllItemInit();
        }
        if (isGenSkinIcons)
        {
            LogUtil.Log("========== 打包前：生成所有 Spine 皮肤图标 ==========");
            GameResourceEditor.SpineAllSkinInit();
        }
        if (isRefreshAtlases)
        {
            LogUtil.Log("========== 打包前：刷新所有图集 ==========");
            GameResourceEditor.RefreshAllAtlases();
        }

        // 固定使用 Game 场景打包（不读 Build Settings 的场景列表，避免日常挂的 TestScene 混进正式包）
        string[] scenes = { GameScenePath };

        Directory.CreateDirectory(buildPath);

        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string locationPath = buildPath;
        // Windows 平台需要指定 exe 文件名
        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
        {
            locationPath = Path.Combine(buildPath, PlayerSettings.productName + ".exe");
        }

        // 按勾选项组装 BuildOptions
        BuildOptions buildOptions = BuildOptions.None;
        if (isDevelopment) buildOptions |= BuildOptions.Development;
        if (isAllowDebugging) buildOptions |= BuildOptions.AllowDebugging;
        if (isConnectProfiler) buildOptions |= BuildOptions.ConnectWithProfiler;
        if (isDeepProfiling) buildOptions |= BuildOptions.EnableDeepProfilingSupport;
        if (isAutoRun) buildOptions |= BuildOptions.AutoRunPlayer;
        if (isShowBuiltPlayer) buildOptions |= BuildOptions.ShowBuiltPlayer;

        LogUtil.Log($"========== 开始打包：{target} -> {locationPath}（Options: {buildOptions}，GM模式: {isEnableGMMode}） ==========");

        // 按 GM 选项写入运行时开关文件（打包结束恢复 0，避免残留影响日常开发出包）
        WriteGMModeConfig(isEnableGMMode);
        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(scenes, locationPath, target, buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                LogUtil.Log($"========== 打包完成：{locationPath} ==========");
                // Mod 复制失败不阻断打包收尾（场景恢复/GM配置还原等），只记错误日志
                try
                {
                    CopySelectedModsToBuild();
                }
                catch (System.Exception e)
                {
                    LogUtil.LogError($"[打包] 复制 Mod 资源失败：{e.Message}");
                }
                // 未勾选 ShowBuiltPlayer 时手动打开一次输出目录，保证用户总能找到产物
                if (!isShowBuiltPlayer)
                {
                    EditorUtility.RevealInFinder(locationPath);
                }
            }
            else
            {
                Debug.LogError($"打包失败：{report.summary.result}，错误数 {report.summary.totalErrors}");
            }
        }
        finally
        {
            WriteGMModeConfig(false);
        }

        // 打包结束恢复打包前打开的场景（如从 TestScene 发起的打包，打完切回去继续日常开发）
        if (!string.IsNullOrEmpty(prevScenePath) && prevScenePath != GameScenePath && File.Exists(prevScenePath))
        {
            EditorSceneManager.OpenScene(prevScenePath);
        }
    }

    /// <summary>
    /// 写入 GM 模式开关文件：打包前按选项写入 0/1 并强制同步导入，保证 BuildPlayer 读到最新值
    /// </summary>
    /// <param name="isEnable">是否开启 GM 模式</param>
    private static void WriteGMModeConfig(bool isEnable)
    {
        File.WriteAllText(GMModeAssetPath, isEnable ? "1" : "0");
        AssetDatabase.ImportAsset(GMModeAssetPath, ImportAssetOptions.ForceSynchronousImport);
    }

    /// <summary>
    /// 确保当前平台已添加 URP_COMPATIBILITY_MODE 编译宏（Unity 6.3 起 URP 兼容模式被打包校验拦截，缺失时 URPPreprocessBuild 直接抛 BuildFailedException）
    /// </summary>
    /// <returns>本次是否新补了宏（补宏会触发脚本重编译，调用方应中止本次打包并提示重新执行）</returns>
    private static bool EnsureURPCompatibilityModeDefine()
    {
        const string define = "URP_COMPATIBILITY_MODE";
        NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        // 按分号拆分精确匹配，防止误判同名前缀宏
        if (defines.Split(';').Any(d => d.Trim() == define))
        {
            return false;
        }
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.IsNullOrEmpty(defines) ? define : defines + ";" + define);
        return true;
    }

    #endregion

    #region Mod 资源复制

    /// <summary>
    /// 扫描项目根目录下的 Mods 文件夹，刷新可勾选 Mod 列表；勾选状态从 EditorPrefs 读取，新出现的 Mod 默认不勾选
    /// </summary>
    private void RefreshModList()
    {
        listModNames.Clear();
        dictModSelection.Clear();
        string modsRoot = GetProjectModsRootPath();
        if (Directory.Exists(modsRoot))
        {
            foreach (var dir in Directory.GetDirectories(modsRoot))
            {
                listModNames.Add(Path.GetFileName(dir));
            }
            listModNames.Sort();
        }
        foreach (var modName in listModNames)
        {
            dictModSelection[modName] = EditorPrefs.GetBool(EditorPrefsKeyPrefixCopyMod + modName, false);
        }
    }

    /// <summary>
    /// 全选/全不选所有 Mod，并写入 EditorPrefs 持久化
    /// </summary>
    private void SetAllModSelection(bool isSelected)
    {
        foreach (var modName in listModNames)
        {
            dictModSelection[modName] = isSelected;
            EditorPrefs.SetBool(EditorPrefsKeyPrefixCopyMod + modName, isSelected);
        }
    }

    /// <summary>
    /// 获取项目内 Mods 根目录（与 Assets 同级，与 ModManager.GetModsRootPath 编辑器模式下一致）
    /// </summary>
    private static string GetProjectModsRootPath()
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Mods");
    }

    /// <summary>
    /// 打包成功后，把勾选的 Mod 目录复制到输出目录的 Mods 下（目标已存在时先删后拷，保证与项目内一致）
    /// </summary>
    private void CopySelectedModsToBuild()
    {
        if (!isCopyMods)
            return;
        string sourceRoot = GetProjectModsRootPath();
        string targetRoot = Path.Combine(buildPath, "Mods");
        foreach (var modName in listModNames)
        {
            if (!dictModSelection.TryGetValue(modName, out bool selected) || !selected)
                continue;
            string sourceDir = Path.Combine(sourceRoot, modName);
            if (!Directory.Exists(sourceDir))
            {
                LogUtil.LogWarning($"[打包] 勾选复制的 Mod 目录不存在，已跳过：{sourceDir}");
                continue;
            }
            string targetDir = Path.Combine(targetRoot, modName);
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            CopyDirectory(sourceDir, targetDir);
            LogUtil.Log($"[打包] 已复制 Mod：{modName} -> {targetDir}");
        }
    }

    /// <summary>
    /// 递归复制目录（同名文件覆盖）
    /// </summary>
    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }

    #endregion
}
