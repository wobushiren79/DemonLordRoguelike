using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UIViewBaseInfoContent : BaseUIView
{
    Sequence animForCrystalChange;//魔晶变化动画
    Sequence animForReputationChange;//声望变化动画

    public override void Awake()
    {
        base.Awake();
        this.RegisterEvent(EventsInfo.Backpack_Crystal_Change, RefreshCrystalData);
        this.RegisterEvent(EventsInfo.Backpack_Reputation_Change, RefreshReputationData);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RefreshUIData(false);
        ui_Crystal_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000002), PopupEnum.Text);
        ui_Reputation_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000003), PopupEnum.Text);
    }

    /// <summary>
    /// 全量刷新魔晶与声望（OnEnable 初始化用，isAnim=false 不播动画）
    /// </summary>
    public void RefreshUIData(bool isAnim)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlockData = userData.GetUserUnlockData();
        //按解锁状态刷新声望显隐
        ui_Reputation_Image.gameObject.SetActive(userUnlockData.CheckIsUnlock(UnlockEnum.DoomCouncil));

        if (ui_Crystal_Image.gameObject.activeSelf && userData != null)
        {
            SetCrystalData(userData.crystal, isAnim);
        }
        if (ui_Reputation_Image.gameObject.activeSelf && userData != null)
        {
            SetReputationData(userData.reputation, isAnim);
        }
    }

    /// <summary>
    /// 刷新魔晶（魔晶变化事件回调，只播魔晶动画）
    /// </summary>
    public void RefreshCrystalData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData != null && ui_Crystal_Image.gameObject.activeSelf)
        {
            SetCrystalData(userData.crystal, true);
        }
    }

    /// <summary>
    /// 刷新声望（声望变化事件回调，只播声望动画）
    /// </summary>
    public void RefreshReputationData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return;
        //按解锁状态刷新声望显隐
        UserUnlockBean userUnlockData = userData.GetUserUnlockData();
        ui_Reputation_Image.gameObject.SetActive(userUnlockData.CheckIsUnlock(UnlockEnum.DoomCouncil));
        if (ui_Reputation_Image.gameObject.activeSelf)
        {
            SetReputationData(userData.reputation, true);
        }
    }

    /// <summary>
    /// 设置当前魔晶
    /// </summary>
    public void SetCrystalData(long crystal, bool isAnim = true)
    {
        ClearCrystalAnim();
        if (isAnim)
        {
            animForCrystalChange = AnimUtil.AnimForUINumberChange(animForCrystalChange, ui_CrystalText, long.Parse(ui_CrystalText.text), crystal, 1f);
        }
        else
        {
            ui_CrystalText.text = $"{crystal}";
        }
    }

    /// <summary>
    /// 设置当前声望
    /// </summary>
    public void SetReputationData(long reputation, bool isAnim = true)
    {
        ClearReputationAnim();
        if (isAnim)
        {
            animForReputationChange = AnimUtil.AnimForUINumberChange(animForReputationChange, ui_ReputationText, long.Parse(ui_ReputationText.text), reputation, 1f);
        }
        else
        {
            ui_ReputationText.text = $"{reputation}";
        }
    }

    /// <summary>
    /// 清理魔晶动画
    /// </summary>
    void ClearCrystalAnim()
    {
        animForCrystalChange?.Kill();
        ui_CrystalText.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 清理声望动画
    /// </summary>
    void ClearReputationAnim()
    {
        animForReputationChange?.Kill();
        ui_ReputationText.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 清理动画（魔晶+声望）
    /// </summary>
    public void ClearAnim()
    {
        ClearCrystalAnim();
        ClearReputationAnim();
    }
}
