using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIBaseCore : BaseUIComponent
{

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue,true);
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_ViewBaseCoreItemFunction_Gashapon)
        {
            OnClickForGashapon();
        }
    }

    /// <summary>
    /// ˢ��UI����
    /// </summary>
    public void RefreshUIData()
    {

    }

    /// <summary>
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        CameraHandler.Instance.SetBaseCoreCamera(0, false);
        CameraHandler.Instance.ShowCinemachineCamera(CinemachineCameraEnum.Base);

        GameControlHandler.Instance.SetBaseControl();
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    /// <summary>
    /// �����Ť����
    /// </summary>
    public void OnClickForGashapon()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponMachine>();
    }
}
