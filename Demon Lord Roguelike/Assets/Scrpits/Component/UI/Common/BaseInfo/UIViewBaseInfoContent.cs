using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UIViewBaseInfoContent : BaseUIView
{
    Sequence animForCrystalChange;//魔晶变化动画

    public override void Awake()
    { 
        base.Awake();
        this.RegisterEvent(EventsInfo.Backpack_Item_Change, RefreshUIData);
        this.RegisterEvent(EventsInfo.Magic_Change, RefreshUIData);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RefreshUIData(false);
    }

    public override void OnDisable()
    {
        base.OnDisable();
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
        if (ui_Magic.gameObject.activeSelf)
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            if (gameFightLogic != null)
            {
                SetMagicData(0);
            }
        }
    }

    /// <summary>
    /// 设置当前魔力
    /// </summary>
    public void SetMagicData(int magic)
    {
        ui_MagicText.text = $"{magic}";
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
    /// 清理动画
    /// </summary>
    public void ClearAnim()
    {
        animForCrystalChange?.Kill();
        ui_CrystalText.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 魔力不够动画
    /// </summary>
    public void PlayAnimForMagicNoEnough()
    {
        ui_MagicText.DOKill();
        ui_MagicText.DOColor(Color.red, 0.05f).SetLoops(6, LoopType.Yoyo).OnComplete(() =>
        {
            ui_MagicText.color = Color.white;
        });
    }
}
