using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class UIBaseMain : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        //开启控制
        GameControlHandler.Instance.SetBaseControl();
        //开启摄像头
        CameraHandler.Instance.SetCameraForControl(CinemachineCameraEnum.Base);
    }

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
        else if (inputType == InputActionUIEnum.ESC)
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIGameSystem>();
        }
    }


    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {

    }
}
