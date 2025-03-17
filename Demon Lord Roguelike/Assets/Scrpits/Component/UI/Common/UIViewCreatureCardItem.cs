using DG.Tweening;
using Spine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
using UnityEngine.UI;

public partial class UIViewCreatureCardItem : BaseUIView
{
    public CreatureCardItemBean cardData = new CreatureCardItemBean();
    public MaskUIView maskUI;//遮罩处理

    public override void Awake()
    {
        base.Awake();
        maskUI = transform.GetComponent<MaskUIView>();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CardUseState cardUseState)
    {
        this.cardData.cardUseState = cardUseState;
        this.cardData.creatureData = creatureData;
        int attDamage = creatureData.GetAttackDamage();
        int HPMax = creatureData.GetHP();

        SetCardIcon(creatureData);
        SetAttribute(attDamage, HPMax);
        SetName(creatureData.creatureName);
        SetLevel(creatureData.level);
        SetRarity(creatureData.rarity);
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
        maskUI.ChangeDefColor(ui_CardBgBorad, boardColor);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置等级
    /// </summary>
    public void SetLevel(int level)
    {
        ui_Level.text = $"{level}";
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    public void SetAttribute(int attDamage, int HPMax)
    {
        ui_AttributeItemText_Att.text = $"{attDamage}";
        ui_AttributeItemText_Life.text = $"{HPMax}";
    }

    /// <summary>
    /// 设置卡片图像
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
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
        ui_CardBg.color = Color.white;
        maskUI.HideMask();
    }

    /// <summary>
    /// 按钮点击
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnSelect)
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
