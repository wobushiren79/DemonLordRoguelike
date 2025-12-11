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
        ui_Crystal_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000002), PopupEnum.Text);
        ui_Reputation_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000003), PopupEnum.Text);
    }

    public void RefreshUIData()
    {
        RefreshUIData(true);
    }

    public void RefreshUIData(bool isAnim)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlockData = userData.GetUserUnlockData();
        //判断是否解锁终焉议会
        bool isUnlockDoomCouncil = userUnlockData.CheckIsUnlock(UnlockEnum.DoomCouncil);
        if (isUnlockDoomCouncil)
        {
            ui_Reputation_Image.gameObject.SetActive(true);
        }
        else
        {
            ui_Reputation_Image.gameObject.SetActive(false);
        }

        if (ui_Crystal_Image.gameObject.activeSelf)
        {
            if (userData != null)
            {
                SetCrystalData(userData.crystal, isAnim);
            }
        }
        if (ui_Reputation_Image.gameObject.activeSelf)
        {
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
