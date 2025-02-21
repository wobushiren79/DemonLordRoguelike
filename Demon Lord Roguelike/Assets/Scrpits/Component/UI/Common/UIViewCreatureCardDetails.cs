using DG.Tweening;
using UnityEngine;

public partial class UIViewCreatureCardDetails : BaseUIView
{
    public CreatureBean creatureData;

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="creatureData"></param>
    public void SetData(CreatureBean creatureData)
    {
        this.creatureData = creatureData;

        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetLevel(creatureData.level);

        int attDamage = creatureData.GetAttDamage();
        int lifeMax = creatureData.GetLife();
        SetAttribute(attDamage, lifeMax);
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
        ui_CardBgBoard.color = boardColor;
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
    public void SetAttribute(int attDamage, int lifeMax)
    {
        ui_AttributeItemText_Att.text = $"{attDamage}";
        ui_AttributeItemText_Life.text = $"{lifeMax}";
    }

    /// <summary>
    /// 设置图片
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForDetails(ui_Icon, ui_CardScene, creatureData);
    }
}
