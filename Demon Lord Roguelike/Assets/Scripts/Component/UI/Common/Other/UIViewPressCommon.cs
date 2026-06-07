using UnityEngine;

/// <summary>
/// 通用按键提示UI：统一展示某个操作对应的快捷按键名（如 C / 1 / 2...），
/// 并受游戏设置「按键提示显示」开关统一控制显隐。
/// </summary>
public partial class UIViewPressCommon : BaseUIView
{
    /// <summary>
    /// 是否已设置有效按键。无有效按键时(如阵容序号超过9的卡片)，无论全局开关如何都保持隐藏，
    /// 避免「按键提示显示」开关被打开时误显示空提示。
    /// </summary>
    private bool isKeyValid;

    #region 生命周期

    /// <summary>
    /// 初始化-注册「按键提示显示」开关变更事件，开关切换时实时刷新自身显隐
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        RegisterEvent(EventsInfo.GameSetting_PressKeyTipShowChange, RefreshShow);
    }

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置按键提示数据(KeyCode 重载)：自动补全为可读按键名后展示
    /// </summary>
    /// <param name="keyCode">按键</param>
    public void SetData(KeyCode keyCode)
    {
        SetData(GetKeyDisplayName(keyCode));
    }

    /// <summary>
    /// 设置按键提示数据：设置按键名文本，并按全局「按键提示显示」设置控制显隐
    /// </summary>
    /// <param name="keyName">按键名(已经补全好的展示文本)</param>
    public void SetData(string keyName)
    {
        isKeyValid = true;
        if (ui_Text != null)
            ui_Text.text = keyName;
        RefreshShow();
    }

    /// <summary>
    /// 隐藏按键提示并标记为无有效按键(如阵容序号超过9的卡片)，使其不受全局开关影响、始终保持隐藏
    /// </summary>
    public void HideForNoKey()
    {
        isKeyValid = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 按全局「按键提示显示」设置刷新自身显隐：无有效按键或开关关闭时整体隐藏，否则显示
    /// </summary>
    public void RefreshShow()
    {
        if (!isKeyValid)
        {
            gameObject.SetActive(false);
            return;
        }
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        //配置未就绪时默认显示，避免误隐藏
        bool isShow = gameConfig == null || gameConfig.pressKeyTipShow;
        gameObject.SetActive(isShow);
    }

    #endregion

    #region 按键名补全

    /// <summary>
    /// 按键名补全逻辑：将 KeyCode 转换为简洁可读的展示名。
    /// 数字键去掉 Alpha/Keypad 前缀，常用功能键给出简短别名，其余统一大写。
    /// </summary>
    /// <param name="keyCode">按键</param>
    /// <returns>用于UI展示的按键名</returns>
    public static string GetKeyDisplayName(KeyCode keyCode)
    {
        switch (keyCode)
        {
            //主键盘数字
            case KeyCode.Alpha0: return "0";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            //小键盘数字
            case KeyCode.Keypad0: return "0";
            case KeyCode.Keypad1: return "1";
            case KeyCode.Keypad2: return "2";
            case KeyCode.Keypad3: return "3";
            case KeyCode.Keypad4: return "4";
            case KeyCode.Keypad5: return "5";
            case KeyCode.Keypad6: return "6";
            case KeyCode.Keypad7: return "7";
            case KeyCode.Keypad8: return "8";
            case KeyCode.Keypad9: return "9";
            //常用功能键简短别名
            case KeyCode.Escape: return "ESC";
            case KeyCode.Space: return "SPACE";
            case KeyCode.Return:
            case KeyCode.KeypadEnter: return "ENTER";
            case KeyCode.Tab: return "TAB";
            case KeyCode.LeftShift:
            case KeyCode.RightShift: return "SHIFT";
            case KeyCode.LeftControl:
            case KeyCode.RightControl: return "CTRL";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt: return "ALT";
            default:
                //字母及其余按键统一大写展示
                return keyCode.ToString().ToUpper();
        }
    }

    #endregion
}
