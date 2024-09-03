using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIMainLoad : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        GameDataHandler.Instance.manager.LoadUserData(1, ActionForLoadUserData);
        GameDataHandler.Instance.manager.LoadUserData(2, ActionForLoadUserData);
        GameDataHandler.Instance.manager.LoadUserData(3, ActionForLoadUserData);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
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
        UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
    }

    /// <summary>
    /// 获取用户数据回调
    /// </summary>
    public void ActionForLoadUserData(int index, UserDataBean userData)
    {
        switch (index)
        {
            case 1:
                ui_UIViewMainLoadItem_1.SetData(index, userData);
                break;
            case 2:
                ui_UIViewMainLoadItem_2.SetData(index, userData);
                break;
            case 3:
                ui_UIViewMainLoadItem_3.SetData(index, userData);
                break;
        }
    }
}
