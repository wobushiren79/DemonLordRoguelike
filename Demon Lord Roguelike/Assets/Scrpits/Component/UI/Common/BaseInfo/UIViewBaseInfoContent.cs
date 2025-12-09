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
        this.RegisterEvent(EventsInfo.Backpack_Crystal_Change, RefreshUIData);
        this.RegisterEvent(EventsInfo.Backpack_Reputation_Change, RefreshUIData);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RefreshUIData(false);
    }

    public void RefreshUIData()
    {
        RefreshUIData(true);
    }

    public void RefreshUIData(bool isAnim)
    {
        if (ui_Crystal.gameObject.activeSelf)
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            if (userData != null)
            {
                SetCrystalData(userData.crystal, isAnim);
            }
        }
        if (ui_Reputation.gameObject.activeSelf)
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            if (userData != null)
            {
                SetReputationData(userData.reputation, isAnim);
            }
        }
    }

    /// <summary>
    /// 设置当前魔晶
    /// </summary>
    public void SetCrystalData(long crystal, bool isAnim = true)
    {
        ClearAnim();
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
    /// 清理动画
    /// </summary>
    public void ClearAnim()
    {
        animForCrystalChange?.Kill();
        animForReputationChange?.Kill();
        ui_CrystalText.transform.localScale = Vector3.one;
        ui_ReputationText.transform.localScale = Vector3.one;
    }
}
