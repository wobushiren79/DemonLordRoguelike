using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗模式编辑工具（主窗口）
/// 以页签方式承载各战斗模式的配置编辑：
/// - 征服模式：编辑 excel_fight_type_conquer_info[战斗-征服模式]，见 FightModeEditorTabConquer
/// - 挑战100勇士：编辑 excel_fight_type_challenge_hundred_info[战斗-挑战100勇士]，见 FightModeEditorTabChallengeHundred
/// </summary>
public class FightModeEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/战斗模式编辑
    /// </summary>
    [MenuItem("游戏/战斗模式编辑")]
    private static void CreateWindow()
    {
        var window = EditorWindow.GetWindow<FightModeEditorWindow>();
        window.titleContent = new GUIContent("战斗模式编辑工具");
        window.minSize = new Vector2(980, 620);
        window.Show();
    }

    #endregion

    #region 成员变量

    /// <summary>页签名称（索引与 tab 实例一一对应）</summary>
    private static readonly string[] TabNames = { "征服模式", "挑战100勇士" };

    /// <summary>当前选中的页签索引</summary>
    private int selectedTab = 0;

    /// <summary>征服模式页签</summary>
    private FightModeEditorTabConquer conquerTab;

    /// <summary>挑战100勇士页签</summary>
    private FightModeEditorTabChallengeHundred challengeHundredTab;

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时创建并初始化全部页签（切页签不丢各自编辑状态）
    /// </summary>
    private void OnEnable()
    {
        conquerTab = new FightModeEditorTabConquer();
        conquerTab.Init();
        challengeHundredTab = new FightModeEditorTabChallengeHundred();
        challengeHundredTab.Init();
    }

    /// <summary>
    /// GUI 渲染入口：顶部页签栏固定，下面委托给当前页签绘制
    /// </summary>
    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, TabNames, GUILayout.Height(28));
        switch (selectedTab)
        {
            case 0:
                conquerTab.OnGUI();
                break;
            case 1:
                challengeHundredTab.OnGUI();
                break;
        }
    }

    #endregion
}

/// <summary>
/// 战斗模式编辑工具的页签基类：每个战斗模式一个页签，各自持有独立状态
/// </summary>
public abstract class FightModeEditorTabBase
{
    /// <summary>
    /// 宿主窗口 OnEnable 时调用：初始化路径与加载数据
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// 宿主窗口 OnGUI 时调用：绘制本页签全部内容
    /// </summary>
    public abstract void OnGUI();
}
