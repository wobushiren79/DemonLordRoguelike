using DG.Tweening;
using UnityEngine;

public partial class UIViewCreatureCardDetails : BaseUIView
{
    public CreatureBean creatureData;

    /// <summary>
    /// 刷新显示
    /// </summary>
    public void RefreshCard()
    {
        SetData(creatureData);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="creatureData"></param>
    public void SetData(CreatureBean creatureData)
    {
        if (creatureData == null)
            return;
        this.creatureData = creatureData;

        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);

        int hp = creatureData.GetHPOrigin();
        int dr = creatureData.GetDROrigin();
        int atk = creatureData.GetATK();
        float aspd = creatureData.GetASPD();
        
        SetAttribute(hp, dr,atk, aspd);
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
        ui_CardRate.color = boardColor;
        ColorUtility.TryParseHtmlString(rarityInfo.ui_board_other_color, out Color boardOtherColor);
        ui_AttributeList_Image.color = boardOtherColor;
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    public void SetAttribute(int HP, int DR, int atk, float aspk)
    {
        ui_AttributeItemText_Life.text = $"{HP}";
        ui_AttributeItemText_Def.text = $"{DR}";
        ui_AttributeItemText_Att.text = $"{atk}";
        ui_AttributeItemText_Speed.text = $"{aspk}s";
    }

    /// <summary>
    /// 设置图片
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForDetails(ui_Icon, ui_CardScene, creatureData);
    }
}
