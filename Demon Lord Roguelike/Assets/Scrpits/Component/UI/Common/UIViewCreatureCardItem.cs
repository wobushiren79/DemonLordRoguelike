using DG.Tweening;
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
    public void SetData(CreatureBean creatureData, CardUseState cardUseState)
    {
        this.cardData.cardUseState = cardUseState;
        this.cardData.creatureData = creatureData;

        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetRarity(creatureData.rarity);

        RefreshCardState(this.cardData.cardState);
        //设置弹窗气泡数据
        ui_BtnSelect_PopupButtonCommonView.SetData((creatureData),PopupEnum.CreatureCardDetails);
    }

    /// <summary>
    /// 设置稀有度
    /// </summary>
    public void SetRarity(int rarity)
    {
        if (rarity == 0)
            rarity = 1;
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        ColorUtility.TryParseHtmlString(rarityInfo.ui_board_color, out Color boardColor);
        ui_CardBgBorad.color = boardColor;
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
    /// 设置倒计时
    /// </summary>
    public void SetCDTime(string cdStr,float progress)
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
    public void OnClickSelect()
    {
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnClickSelect, this);
    }

}
