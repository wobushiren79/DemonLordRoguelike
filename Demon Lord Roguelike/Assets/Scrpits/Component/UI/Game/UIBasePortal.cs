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

        //��������
        GameControlHandler.Instance.SetBaseControl(false);
        //��������ͷ
        CameraHandler.Instance.SetBasePortalCamera(int.MaxValue,true);
        //�ر�Զ��
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        //�ر�Զ��
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
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

}