using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIGameSystem : BaseUIComponent
{

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.SetBaseControl(false, false);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnSetting)
        {
            OnClickForGameSetting();
        }
        else if (viewButton == ui_BtnBack)
        {
            OnClickForBackMain();
        }
        else if (viewButton == ui_BtnExit)
        {
            OnClickForExitGame();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    /// <summary>
    /// 点击返回主界面
    /// </summary>
    public void OnClickForBackMain()
    {
        WorldHandler.Instance.EnterMainForBaseScene();
    }

    /// <summary>
    /// 点击进入游戏设置
    /// </summary>
    public void OnClickForGameSetting()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameSetting>();
        targetUI.enterType = 1;
    }

    /// <summary>
    /// 离开游戏
    /// </summary>
    public void OnClickForExitGame()
    {
        GameUtil.ExitGame();
    }
}
