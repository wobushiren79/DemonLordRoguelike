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
        else if (viewButton == ui_ViewBaseCoreItemFunction_Achievement)
        {
            OnClickForAchievement();
        }
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //是否解锁Vat
        bool isUnlockVat = userUnlock.CheckIsUnlock(UnlockEnum.CreatureVat);
        ui_ViewBaseCoreItemFunction_Vat.gameObject.SetActive(isUnlockVat);
        //是否解锁孕育
        bool isUnlockGashaponMachine = userUnlock.CheckIsUnlock(UnlockEnum.GashaponMachine);
        ui_ViewBaseCoreItemFunction_Gashapon.gameObject.SetActive(isUnlockGashaponMachine);
        //是否解锁成就
        if (ui_ViewBaseCoreItemFunction_Achievement != null)
        {
            bool isUnlockAchievement = userUnlock.CheckIsUnlock(UnlockEnum.Achievement);
            ui_ViewBaseCoreItemFunction_Achievement.gameObject.SetActive(isUnlockAchievement);
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
    }

    /// <summary>
    /// 点击打开-蜕变
    /// </summary>
    public void OnClickForVat()
    {
        //由基地核心打开: 退出时返回 UIBaseCore
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UICreatureVat>((ui) =>
        {
            ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
        });
    }

    /// <summary>
    /// 点击打开-成就
    /// </summary>
    public void OnClickForAchievement()
    {
        //由基地核心打开: 退出时返回 UIBaseCore
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIAchievement>((ui) =>
        {
            ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
        });
    }
}
