using Spine;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewCreatureCardItem : BaseUIView
{
    public CreatureCardItemBean cardData = new CreatureCardItemBean();
    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        this.cardData.cardUseState = cardUseState;
        this.cardData.creatureData = creatureData;

        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetClass(creatureData.creatureInfo.class_icon_res);
        SetRarity(creatureData.rarity);
        SetLevel(creatureData.level);
        SetCreateMP(creatureData.GetAttributeInt(CreatureAttributeTypeEnum.CMP));
        SetPopupShow(creatureData, cardUseState);
        SetSacrificeEffect(creatureData, cardUseState);
        RefreshCardState(this.cardData.cardState);
    }

    /// <summary>
    /// 设置献祭升级特效显隐: 仅在魔物管理界面(CreatureManager) 且 已解锁祭坛 且 当前生物满足献祭升级条件(CanUpLevel)时显示。
    /// </summary>
    public void SetSacrificeEffect(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        bool canShowSacrificeEffect = false;
        if (cardUseState == CardUseStateEnum.CreatureManager && creatureData != null)
        {
            var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
            canShowSacrificeEffect = userUnlock.CheckIsUnlock(UnlockEnum.Altar) && creatureData.CanUpLevel();
        }
        ui_SacrificeEffect.gameObject.SetActive(canShowSacrificeEffect);
    }

    /// <summary>
    /// 设置气泡弹窗展示
    /// </summary>
    public void SetPopupShow(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        //设置弹窗气泡数据
        switch (cardUseState)
        {
            case CardUseStateEnum.ShowNoPopup:
            case CardUseStateEnum.CreatureManager:
                ui_BtnSelect_PopupButtonCommonView.enabled = false;
                break;
            default:
                ui_BtnSelect_PopupButtonCommonView.enabled = true;
                ui_BtnSelect_PopupButtonCommonView.SetData(creatureData, PopupEnum.CreatureCardDetails);
                break;
        }
    }

    /// <summary>
    /// 设置稀有度
    /// </summary>
    public void SetRarity(int rarity)
    {
        if (rarity == 0)
            rarity = 1;
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        GameUIUtil.SetGradientColor(ui_CardBgBorad, rarityInfo.ui_board_color);
        GameUIUtil.SetGradientColor(ui_IconContent, rarityInfo.ui_board_other_color);
    }

    /// <summary>
    /// 设置等级（按等级配置表的等级颜色给等级字体着色：0 级白色，1-10 级渐进色）
    /// </summary>
    public void SetLevel(int level)
    {
        ui_LevelText.text = $"{level}";
        ui_LevelText.color = LevelInfoCfg.GetLevelColor(level);
    }

    /// <summary>
    /// 设置召唤消耗魔力（CMP=召唤该生物需要消耗的魔力，基础CMP×(1+等级/稀有度增加倍率)，战斗中从魔王魔力中扣除）
    /// </summary>
    public void SetCreateMP(int createMP)
    {
        ui_MPText.text = $"{createMP}";
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置卡片图像
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
    }

    /// <summary>
    /// 设置职业图标
    /// </summary>
    public void SetClass(string iconRes)
    {
        if (string.IsNullOrEmpty(iconRes))
        {
            ui_Class.gameObject.SetActive(false);
            return;
        }
        ui_Class.gameObject.SetActive(true);
        IconHandler.Instance.SetUIIcon(iconRes, ui_Class);
    }

    /// <summary>
    /// 设置倒计时
    /// </summary>
    public void SetCDTime(string cdStr, float progress)
    {
        ui_CDTime.text = $"{cdStr}";
        ui_Mask.fillAmount = progress;
    }

    /// <summary>
    /// 设置卡的状态
    /// </summary>
    public virtual void SetCardState(CardStateEnum cardState)
    {
        this.cardData.cardState = cardState;
        RefreshCardState(cardState);
    }

    /// <summary>
    /// 刷新卡的状态
    /// </summary>
    public virtual void RefreshCardState(CardStateEnum cardState)
    {
        ui_SelectBg.gameObject.SetActive(false);
        ui_CDTime.gameObject.SetActive(false);
        ui_Mask.gameObject.SetActive(false);
        ui_Mask.fillAmount = 1;
    }

    /// <summary>
    /// 按钮点击
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnSelect_Button)
        {
            OnClickSelect();
        }
    }

    /// <summary>
    /// 点击选择
    /// </summary>
    public virtual void OnClickSelect()
    {
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnClickSelect, this);
    }
}
