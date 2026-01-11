using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewCreatureCardDetails : BaseUIView
{
    public CreatureBean creatureData;
    //是否展示装备道具
    public bool isShowEquipItem = true;
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
        SetLevelData(creatureData.level, creatureData.levelExp);

        SetRelationship(creatureData.relationship);
        SetClass(creatureData.creatureInfo.class_icon_res, creatureData.creatureInfo.name_language);
        SetTitle();

        SetDoomCouncilData();
        SetEquipData();

        RefreshUILayout();
    }

    /// <summary>
    /// 设置装备数据
    /// </summary>
    public void SetEquipData()
    {
        //如果不展示装备数据
        if (!isShowEquipItem)
        {
            ui_Equip.gameObject.SetActive(false);
            return;
        }
        ui_Equip.gameObject.SetActive(true);
        List<ItemTypeEnum> listEquipType = creatureData.creatureInfo.GetEquipItemsType();
        
        for (int i = 0; i < ui_Equip.transform.childCount; i++)
        {
            var itemChildTF = ui_Equip.transform.GetChild(i);
            if (i < listEquipType.Count)
            {       
                var itemType = listEquipType[i];
                var viewItemEquip =  itemChildTF.GetComponent<UIViewItemEquip>();
                viewItemEquip.SetData(itemType);
                itemChildTF.gameObject.SetActive(true);
                var itemData = creatureData.GetEquip(itemType);
                viewItemEquip.SetData(itemData);
            }
            else
            {
                itemChildTF.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置终焉议会数据
    /// </summary>
    public void SetDoomCouncilData()
    {
        var npcData = creatureData.GetCreatureNpcData();
        //如果是NPC数据
        if (npcData != null && npcData.npcId != 0)
        {
            var npcInfo = NpcInfoCfg.GetItemData(npcData.npcId);
            if (npcInfo.GetNpcType() == NpcTypeEnum.Councilor)
            {
                ui_NameDoomCouncil.gameObject.SetActive(true);
                int rating = npcInfo.GetCouncilorRatings();
                var rarityInfo = DoomCouncilRatingsInfoCfg.GetItemData(rating);
                ui_NameDoomCouncilText.text = $"{TextHandler.Instance.GetTextById(53000)}{rarityInfo.name_language}({rarityInfo.vote})";
                return;   
            }
        }
        ui_NameDoomCouncil.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置关系
    /// </summary>
    public void SetRelationship(int relationship)
    {
        var npcData = creatureData.GetCreatureNpcData();
        //如果是NPC数据
        if (npcData != null && npcData.npcId != 0)
        {
            var npcRelationshipInfo = NpcRelationshipInfoCfg.GetNpcRelationship(relationship);
            ui_Relationship.gameObject.SetActive(true);
            IconHandler.Instance.SetUIIcon(npcRelationshipInfo.icon_res, ui_RelationshipIcon);
            ui_RelationshipText.text = $"{npcRelationshipInfo.name_language}";
        }
        else
        {
            ui_Relationship.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 设置职业
    /// </summary>
    /// <param name="iconRes"></param>
    /// <param name="className"></param>
    public void SetClass(string iconRes, string className)
    {
        ui_ClassName.text = className;
        IconHandler.Instance.SetUIIcon(iconRes, ui_ClassIcon);
    }

    /// <summary>
    /// 设置称号
    /// </summary>
    public void SetTitle()
    {
        var npcData = creatureData.GetCreatureNpcData();
        List<long> titleList = null;
        if (npcData != null && npcData.npcId != 0)
        {
            var npcInfo = NpcInfoCfg.GetItemData(npcData.npcId);
            titleList = npcInfo.GetTitles();
        }
        if (titleList.IsNull())
        {
            ui_NameTitle.gameObject.SetActive(false);
        }
        else
        {
            ui_NameTitle.gameObject.SetActive(true);
            string titleText = "";
            for (int i = 0; i < titleList.Count; i++)
            {
                var titleInfo = TitleInfoCfg.GetItemData(titleList[i]);
                titleText += $"{titleInfo.name_language} ";
            }
            ui_NameTitleText.text = titleText;
        }
    }

    /// <summary>
    /// 设置等级数据
    /// </summary>
    public void SetLevelData(int level, long levelExp)
    {
        ui_LevelText.text = string.Format(TextHandler.Instance.GetTextById(1001001), level);
        var levelInfo = LevelInfoCfg.GetItemData(level + 1);
        //如果没有下一级的数据了
        if (levelInfo == null || levelInfo.id == 0)
        {
            ui_LevelProgressData.fillAmount = 1;
        }
        else
        {
            float percentage = (float)levelExp / long.Parse(levelInfo.level_exp);
            ui_LevelProgressData.fillAmount = percentage;
            ui_LevelProgressText.text = $"{MathUtil.GetPercentage(percentage, 2)}%";
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

    /// <summary>
    /// 设置详情位置
    /// </summary>
    /// <param name="direction"></param>
    public void SetDetailsDirection(Direction2DEnum direction)
    {
        if (direction == Direction2DEnum.Left)
        {
            ui_Details.anchorMin = new Vector2(0, 0.5f);
            ui_Details.anchorMax = new Vector2(0, 0.5f);
            ui_Details.pivot = new Vector2(1, 0.5f);
            ui_Details.anchoredPosition = new Vector2(-20, 0);

        }
        else
        {
            ui_Details.anchorMin = new Vector2(1, 0.5f);
            ui_Details.anchorMax = new Vector2(1, 0.5f);
            ui_Details.pivot = new Vector2(0, 0.5f);
            ui_Details.anchoredPosition = new Vector2(20, 0);
        }
    }

    /// <summary>
    /// 刷新一下UI布局
    /// </summary>
    public void RefreshUILayout()
    {
        //如果2个UI都没了
        if (!ui_NameTitle.gameObject.activeSelf && !ui_Relationship.gameObject.activeSelf)
        {
            ui_Details_Child_1.gameObject.SetActive(false);
        }
        else
        {
            ui_Details_Child_1.gameObject.SetActive(true);
        }
        //刷新UI
        UGUIUtil.RefreshUISize(ui_Details);
    }
}
