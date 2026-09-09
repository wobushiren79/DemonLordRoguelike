using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

/// <summary>
/// 游戏资源处理窗口：按板块页签组织各类资源处理功能（Spine 图标提取 / 图集刷新 / 通用一键生成）。
/// 各板块功能拆分为 partial：GameResourceEditor.Spine.cs / GameResourceEditor.Atlas.cs / GameResourceEditor.Common.cs
/// </summary>
public partial class GameResourceEditor : EditorWindow
{
    #region 字段

    /// <summary>当前选中的板块页签</summary>
    private int selectedTab = 0;

    /// <summary>板块页签名</summary>
    private static readonly string[] TabNames = { "Spine", "图集", "通用" };

    /// <summary>刷新灰</summary>
    private readonly Color _colorRefresh = new Color(0.55f, 0.55f, 0.55f);
    /// <summary>一键生成绿</summary>
    private readonly Color _colorAll     = new Color(0.20f, 0.75f, 0.35f);
    /// <summary>道具橙</summary>
    private readonly Color _colorItem    = new Color(0.90f, 0.55f, 0.25f);
    /// <summary>皮肤紫</summary>
    private readonly Color _colorSkin    = new Color(0.75f, 0.40f, 0.80f);

    #endregion

    #region 窗口入口

    [MenuItem("游戏/游戏资源处理")]
    public static void ShowWindow()
    {
        GetWindow<GameResourceEditor>("游戏资源处理", typeof(SceneView)).minSize = new Vector2(520, 560);
    }

#if UNITY_6000_3_OR_NEWER
    /// <summary>
    /// 主工具栏快捷按钮：直接打开游戏资源处理窗口（元素 ID 不可改——主工具栏按 ID 持久化显示状态，改 ID 会被当新元素默认隐藏）
    /// </summary>
    [MainToolbarElement("自定义标题/游戏资源处理", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement CreateToolbarButton()
    {
        var content = new MainToolbarContent("游戏资源处理", null, "打开游戏资源处理窗口（Spine 图标提取 / Spine 资源导入 / 图集刷新 / 一键生成）");
        return new MainToolbarButton(content, ShowWindow);
    }
#endif

    private void OnGUI()
    {
        // 标题
        GUILayout.Space(12);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize  = 16
        };
        GUILayout.Label("游戏资源处理工具", titleStyle);
        GUILayout.Space(4);

        GUIStyle subStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 11
        };
        GUILayout.Label("按板块分类处理游戏资源", subStyle);
        GUILayout.Space(10);

        DrawTabToolbar();
        GUILayout.Space(10);

        switch (selectedTab)
        {
            case 0: DrawSpineTab(); break;
            case 1: DrawAtlasTab(); break;
            case 2: DrawCommonTab(); break;
        }

        GUILayout.Space(16);
    }

    #endregion

    #region UI Helpers

    /// <summary>
    /// 绘制板块页签栏
    /// </summary>
    private void DrawTabToolbar()
    {
        int newTab = GUILayout.Toolbar(selectedTab, TabNames, GUILayout.Height(24));
        if (newTab != selectedTab)
        {
            selectedTab = newTab;
            // Spine 板块含导入列表内容较多，切页签时同步调整最小尺寸
            minSize = selectedTab == 0 ? new Vector2(520, 560) : new Vector2(340, 420);
        }
    }

    /// <summary>
    /// 绘制分组框（带标题与分隔线）
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
    /// 绘制带背景色的功能按钮
    /// </summary>
    private void DrawButton(string label, Color color, float height, System.Action onClick)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        if (GUILayout.Button(label, GUILayout.Height(height)))
        {
            onClick?.Invoke();
        }
        GUI.backgroundColor = prev;
    }

    #endregion
}
