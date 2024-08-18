using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class UIBaseMain : BaseUIComponent
{
    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.F12)
        {
            if (Application.isEditor)
            {
                UIHandler.Instance.OpenUIAndCloseOther<UITestBase>();
            }
        }
    }


    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    /// <summary>
    /// Ë¢ÐÂUIÊý¾Ý
    /// </summary>
    public void RefreshUIData()
    {

    }
}
