using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 打包游戏工具窗口：打包前按勾选项执行 Spine 资源生成（道具图标/皮肤图标/刷新图集，逻辑复用 GameDataEditor），随后执行 BuildPlayer 打包
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

    /// <summary>打包用的游戏场景（正式包固定从该场景出包，与 Build Settings 里的日常测试场景解耦）</summary>
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

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

    #endregion

    #region 窗口入口

    [MenuItem("游戏/打包游戏")]
    public static void ShowWindow()
    {
        GetWindow<GameBuildEditorWindow>("打包游戏", typeof(SceneView)).minSize = new Vector2(360, 320);
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

        DrawSectionBox("打包前执行（复用 Spine 资源生成工具）", () =>
        {
            isGenItemIcons = EditorGUILayout.Toggle(new GUIContent("生成所有 Spine 道具图标", "调用 GameDataEditor.SpineAllItemInit"), isGenItemIcons);
            GUILayout.Space(4);
            isGenSkinIcons = EditorGUILayout.Toggle(new GUIContent("生成所有 Spine 皮肤图标", "调用 GameDataEditor.SpineAllSkinInit"), isGenSkinIcons);
            GUILayout.Space(4);
            isRefreshAtlases = EditorGUILayout.Toggle(new GUIContent("刷新所有图集", "调用 GameDataEditor.RefreshAllAtlases"), isRefreshAtlases);
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
            DrawOptionToggle("打包完成后自动运行", "BuildOptions.AutoRunPlayer", isAutoRun, EditorPrefsKeyAutoRun, v => isAutoRun = v);
            DrawOptionToggle("打包完成后打开输出目录", "BuildOptions.ShowBuiltPlayer", isShowBuiltPlayer, EditorPrefsKeyShowBuiltPlayer, v => isShowBuiltPlayer = v);
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
    /// 绘制分组框（与 GameDataEditor 风格一致）
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
            GameDataEditor.SpineAllItemInit();
        }
        if (isGenSkinIcons)
        {
            LogUtil.Log("========== 打包前：生成所有 Spine 皮肤图标 ==========");
            GameDataEditor.SpineAllSkinInit();
        }
        if (isRefreshAtlases)
        {
            LogUtil.Log("========== 打包前：刷新所有图集 ==========");
            GameDataEditor.RefreshAllAtlases();
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

        LogUtil.Log($"========== 开始打包：{target} -> {locationPath}（Options: {buildOptions}） ==========");
        BuildReport report = BuildPipeline.BuildPlayer(scenes, locationPath, target, buildOptions);

        if (report.summary.result == BuildResult.Succeeded)
        {
            LogUtil.Log($"========== 打包完成：{locationPath} ==========");
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

        // 打包结束恢复打包前打开的场景（如从 TestScene 发起的打包，打完切回去继续日常开发）
        if (!string.IsNullOrEmpty(prevScenePath) && prevScenePath != GameScenePath && File.Exists(prevScenePath))
        {
            EditorSceneManager.OpenScene(prevScenePath);
        }
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
}
