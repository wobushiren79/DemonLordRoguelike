using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Mod 构建工具窗口：只打开主项目即可一键完成 MOD 项目的构建与部署——
/// ⓪「仅导出配置」跑主项目生成脚本(export)重建 JsonText 并只同步 JsonText 目录(不构建/不动资源包，秒级)；
/// ①「构建 Mod 资源」用批量模式(-batchmode -executeMethod)调起 MOD 项目自己的 Unity 执行构建方法(产物 bundle+catalog)；
/// ②「导出配置+整体移动」跑主项目生成脚本(export --deploy-main)重新导出 JsonText 并整体覆盖到本项目 Mods/(含资源包)；
/// ③「仅移动 JsonText」不导出，纯把 JsonText 目录覆盖拷贝到本项目(已导出过、只想快速同步时用)。
/// ④「一键构建+移动」= ①成功后再②。全程无需人工打开 MOD 项目编辑器。
/// </summary>
public class ModBuildEditorWindow : EditorWindow
{
    #region 字段

    /// <summary>Mod 项目路径的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyModProjectPath = "ModBuildEditorWindow.ModProjectPath";
    /// <summary>Mod 名称的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyModName = "ModBuildEditorWindow.ModName";
    /// <summary>构建方法的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyBuildMethod = "ModBuildEditorWindow.BuildMethod";
    /// <summary>Unity.exe 覆盖路径的 EditorPrefs 键</summary>
    private const string EditorPrefsKeyUnityExeOverride = "ModBuildEditorWindow.UnityExeOverride";

    /// <summary>MOD 项目路径（Unity 工程根目录）</summary>
    private string modProjectPath = "";
    /// <summary>Mod 名（= Mods 下的目录名）</summary>
    private string modName = "AeonsEchoSpine";
    /// <summary>MOD 项目侧执行的静态构建方法（-executeMethod 参数）</summary>
    private string buildMethod = "AeonsEchoSpineModBuilder.BuildMod";
    /// <summary>Unity.exe 覆盖路径（空=按 MOD 项目版本自动定位）</summary>
    private string unityExeOverride = "";

    /// <summary>批量构建进程（null=空闲）</summary>
    private Process buildProcess;
    /// <summary>构建开始时间（耗时显示用）</summary>
    private DateTime buildStartTime;
    /// <summary>批量构建日志文件路径</summary>
    private string buildLogPath;
    /// <summary>构建成功后是否继续执行导出+移动（一键模式）</summary>
    private bool isOneClickPending;

    /// <summary>状态/日志文本（追加式，超长截头）</summary>
    private string logText = "";
    /// <summary>日志区滚动位置</summary>
    private Vector2 scrollLog;

    #endregion

    #region 窗口入口

    [MenuItem("游戏/Mod构建工具")]
    public static void ShowWindow()
    {
        GetWindow<ModBuildEditorWindow>("Mod构建工具", typeof(SceneView)).minSize = new Vector2(420, 420);
    }

    private void OnEnable()
    {
        modProjectPath = EditorPrefs.GetString(EditorPrefsKeyModProjectPath, "");
        modName = EditorPrefs.GetString(EditorPrefsKeyModName, "AeonsEchoSpine");
        buildMethod = EditorPrefs.GetString(EditorPrefsKeyBuildMethod, "AeonsEchoSpineModBuilder.BuildMod");
        unityExeOverride = EditorPrefs.GetString(EditorPrefsKeyUnityExeOverride, "");
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
                }
            }
            EditorGUILayout.EndHorizontal();
            DrawTextFieldWithSave("Mod 名称", "Mods 下的目录名，主游戏按此名加载", ref modName, EditorPrefsKeyModName);
            DrawTextFieldWithSave("构建方法", "批量模式 -executeMethod 调用的静态方法（类名.方法名）", ref buildMethod, EditorPrefsKeyBuildMethod);

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
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.65f);
            if (GUILayout.Button("⓪ 仅导出配置（Excel→JsonText，秒级，不动资源包）", GUILayout.Height(30)))
            {
                RunExportOnly();
            }
            GUI.backgroundColor = new Color(0.40f, 0.70f, 0.90f);
            if (GUILayout.Button("① 构建 Mod 资源（批量模式，产物 bundle+catalog）", GUILayout.Height(30)))
            {
                StartBuild(false);
            }
            GUI.backgroundColor = new Color(0.85f, 0.60f, 0.90f);
            if (GUILayout.Button("② 导出配置 + 整体移动 Mod（含资源包）", GUILayout.Height(30)))
            {
                RunExportAndMove();
            }
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.65f);
            if (GUILayout.Button("③ 仅移动 JsonText（不导出，纯拷贝，秒级）", GUILayout.Height(24)))
            {
                MoveJsonTextOnly();
            }
            GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
            if (GUILayout.Button("⚡ 一键构建 + 移动", GUILayout.Height(36)))
            {
                StartBuild(true);
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // 构建中状态与取消
            if (isBuilding)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"构建中… 已耗时 {(DateTime.Now - buildStartTime):mm\\:ss}（MOD 项目体量大时可达数分钟）", EditorStyles.miniLabel);
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
    /// 轮询批量构建进程（EditorApplication.update 驱动）：完成后读取日志、按结果继续一键流程
    /// </summary>
    private void PollBuildProcess()
    {
        if (buildProcess == null) return;
        Repaint();// 刷新耗时显示
        if (!buildProcess.HasExited) return;

        int exitCode = buildProcess.ExitCode;
        buildProcess.Dispose();
        buildProcess = null;
        EditorApplication.update -= PollBuildProcess;

        string logTail = ReadLogTail(buildLogPath, 30);
        if (exitCode == 0)
        {
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
        string exportPy = Path.Combine(mainRoot, ".claude/scripts/gen_aeonsecho_spine_mod.py");
        if (File.Exists(runPythonPs1) && File.Exists(exportPy))
        {
            RunPythonExportAndDeploy(mainRoot, runPythonPs1, exportPy);
        }
        else
        {
            AppendLog("（未找到主项目导出脚本，退化为纯目录拷贝）");
            CopyModToMainProject(modOutputDir, Path.Combine(mainRoot, "Mods", modName));
        }
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
        string exportPy = Path.Combine(mainRoot, ".claude/scripts/gen_aeonsecho_spine_mod.py");
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
