using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIBasePortal : BaseUIComponent
{

    public override void OpenUI()
    {
        base.OpenUI();

        //开启控制
        GameControlHandler.Instance.SetBaseControl(false);
        //开启摄像头
        CameraHandler.Instance.SetBasePortalCamera(int.MaxValue,true);
        //关闭远景
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        //关闭远景
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
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

}
