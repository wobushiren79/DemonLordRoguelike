using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIBaseCore : BaseUIComponent
{

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue, true);
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
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
        else if (viewButton == ui_ViewBaseCoreItemFunction_Creature)
        {
            OnClickForCreature();
        }
        else if (viewButton == ui_ViewBaseCoreItemFunction_Lineup)
        {
            OnClickForLineup();
        }
        else if (viewButton == ui_ViewBaseCoreItemFunction_Gashapon)
        {
            OnClickForGashapon();
        }
        else if (viewButton == ui_ViewBaseCoreItemFunction_Research)
        {
            OnClickForResearch();
        }
        else if(viewButton == ui_ViewBaseCoreItemFunction_Vat)
        {
            OnClickForVat();
        }
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {

    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    /// <summary>
    /// 点击生物管理
    /// </summary>
    public void OnClickForCreature()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UICreatureManager>();
    }

    /// <summary>
    /// 点击阵容管理
    /// </summary>
    public void OnClickForLineup()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UILineupManager>();
    }

    /// <summary>
    /// 点击打开扭蛋机
    /// </summary>
    public void OnClickForGashapon()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponMachine>();
    }

    /// <summary>
    /// 点击打开商店
    /// </summary>
    public void OnClickForResearch()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIBaseResearch>();
        targetUI.SetData();
    }

    /// <summary>
    /// 点击打开-蜕变
    /// </summary>
    public void OnClickForVat()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UICreatureVat>();
    }
}
