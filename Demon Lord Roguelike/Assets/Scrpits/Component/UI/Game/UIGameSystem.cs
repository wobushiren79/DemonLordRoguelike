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
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
        GameControlHandler.Instance.SetBaseControl(true);
    }

    /// <summary>
    /// ���������Ϸ����
    /// </summary>
    public void OnClickForGameSetting()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameSetting>();
    }

    /// <summary>
    /// �뿪��Ϸ
    /// </summary>
    public void OnClickForExitGame()
    {
        GameUtil.ExitGame();
    }
}
