using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 生物卡片详情UI视图 - 用于展示生物的详细信息，包括属性、装备、BUFF等
/// </summary>
public partial class UIViewCreatureCardDetails : BaseUIView
{
    /// <summary>生物数据，包含当前展示的生物完整信息</summary>
    public CreatureBean creatureData;
    /// <summary>是否展示装备道具，默认为true</summary>
    public bool isShowEquipItem = true;
    /// <summary>初始详情面板方向（可在编辑器中配置，默认右侧）；与 SetDetailsDirection 使用相同的参数逻辑</summary>
    [Header("详情面板初始方向")]
    public Direction2DEnum directionInit = Direction2DEnum.Right;

    /// <summary>
    /// 初始化，应用编辑器配置的初始详情面板方向
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        SetDetailsDirection(directionInit);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下修改 directionInit 时实时应用方向，便于直接预览左右布局
    /// </summary>
    public void OnValidate()
    {
        if (ui_Details != null)
        {
            SetDetailsDirection(directionInit);
        }
    }
#endif

    /// <summary>
    /// 刷新卡片显示
    /// </summary>
    public void RefreshCard()
    {
        SetData(creatureData);
    }

    /// <summary>
    /// 设置生物数据并刷新所有UI显示
    /// </summary>
    /// <param name="creatureData">生物数据</param>
    public void SetData(CreatureBean creatureData)
    {
        if (creatureData == null)
            return;
        this.creatureData = creatureData;

        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);

        //魔王:使用魔王专属稀有度(DemonLord)配置显示(深黑+暗紫红配色)、隐藏等级容器(ui_Level)与详情基础容器(ui_DetailsBase)
        bool isDemonLord = creatureData.IsDemonLord();
        ui_Level.gameObject.SetActive(!isDemonLord);
        ui_DetailsBase.gameObject.SetActive(!isDemonLord);

        SetAttribute();
        SetRarity(isDemonLord ? (int)RarityEnum.DemonLord : creatureData.rarity);
        SetLevelData(creatureData.level, creatureData.levelExp);

        SetRelationship(creatureData.relationship);
        SetClass(creatureData.creatureInfo.class_icon_res, creatureData.creatureInfo.name_language);
        SetTitle();

        SetDoomCouncilData();
        SetEquipData();
        SetBuff();
        SetMP();
        SetRenmark();

        RefreshUILayout();
    }

    /// <summary>
    /// 设置BUFF显示
    /// </summary>
    public void SetBuff()
    {
        List<BuffBean> listBuffData = creatureData.GetListBuffData();
        if (listBuffData.IsNull())
        {
            ui_Buff.gameObject.SetActive(false);
            return;
        }
        ui_Buff.gameObject.SetActive(true);
        for (int i = 0; i < ui_Buff.childCount; i++)
        {
            var itemChildTF = ui_Buff.GetChild(i);
            if (i < listBuffData.Count)
            {       
                itemChildTF.gameObject.SetActive(true);
                var itemBuffData = listBuffData[i];
                var viewItem =  itemChildTF.GetComponent<UIViewBuffShowItem>();
                viewItem.SetData(itemBuffData);
            }
            else
            {
                itemChildTF.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置魔力消耗显示（召唤该生物所需消耗的魔王魔力 CMP，基础CMP×(1+等级/稀有度增加倍率)）
    /// <para>若该生物本身就是魔王(与玩家存档 selfCreature 同一 UUId)，则无召唤消耗概念，隐藏父节点 ui_MP。</para>
    /// </summary>
    public void SetMP()
    {
        //如果是魔王本体则隐藏魔力消耗节点
        if (IsDemonLord())
        {
            ui_MP.gameObject.SetActive(false);
            return;
        }
        ui_MP.gameObject.SetActive(true);
        //召唤该生物消耗的魔力（GetAttribute(CMP)=经自身/稀有度BUFF修正后的召唤耗魔）
        ui_MPContent.text = $"{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.CMP)}";
    }

    /// <summary>
    /// 设置生物详情说明（攻击方式描述）。文本取自生物表 details[language_1] 列（textId=生物自身id，取语言表 content_1 语种列）；
    /// details=0（未配置）或当前语种文本为空时隐藏 ui_RenmarkText 容器。魔王等未配置 details 的生物自然走隐藏分支，无需特判。
    /// </summary>
    public void SetRenmark()
    {
        string detailsText = creatureData.creatureInfo.details_language;
        if (detailsText.IsNull())
        {
            ui_RenmarkText.gameObject.SetActive(false);
            return;
        }
        ui_RenmarkText.gameObject.SetActive(true);
        ui_RenmarkTextContent.text = detailsText;
    }

    /// <summary>
    /// 判断当前展示的生物是否为魔王本体（与玩家存档中的 selfCreature 同一 UUId）。收口到 CreatureBean.IsDemonLord 单一真实源。
    /// </summary>
    /// <returns>true=魔王本体</returns>
    public bool IsDemonLord()
    {
        return creatureData != null && creatureData.IsDemonLord();
    }

    /// <summary>
    /// 设置装备数据显示
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
    /// 设置终焉议会数据（如果是议会成员）
    /// </summary>
    public void SetDoomCouncilData()
    {
        var npcData = creatureData.GetCreatureNpcData();
        //如果是NPC数据
        if (npcData != null && npcData.npcId != 0)
        {
            var npcInfo = NpcInfoCfg.GetItemData(npcData.npcId);
            //议会固定NPC与议会随机NPC都展示评级名称
            if (npcInfo.GetNpcType() == NpcTypeEnum.Councilor || npcInfo.GetNpcType() == NpcTypeEnum.CouncilorRandom)
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
    /// 设置关系显示（如果是NPC）
    /// </summary>
    /// <param name="relationship">关系值</param>
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
    /// 设置职业显示
    /// </summary>
    /// <param name="iconRes">职业图标资源路径</param>
    /// <param name="className">职业名称</param>
    public void SetClass(string iconRes, string className)
    {
        ui_ClassName.text = className;
        IconHandler.Instance.SetUIIcon(iconRes, ui_ClassIcon);
    }

    /// <summary>
    /// 设置称号显示
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
    /// <param name="level">等级</param>
    /// <param name="levelExp">当前等级经验</param>
    public void SetLevelData(int level, long levelExp)
    {
        //魔王隐藏等级:等级容器 ui_Level 已在 SetData 关闭,此处直接跳过赋值
        if (IsDemonLord())
            return;
        ui_LevelText.text = string.Format(TextHandler.Instance.GetTextById(1001001), level);
        //按等级配置表的等级颜色给等级字体着色(0 级白色, 1-10 级渐进色)
        ui_LevelText.color = LevelInfoCfg.GetLevelColor(level);
        var levelInfo = LevelInfoCfg.GetItemData(level + 1);
        //如果没有下一级的数据了
        if (levelInfo == null || levelInfo.id == 0)
        {
            ui_LevelProgressData.fillAmount = 1;
        }
        else
        {
            //经验由战斗持续累积、仅在献祭成功时才消耗并清0,未献祭前 levelExp 可能超过本级所需经验,此处限制进度最大为100%避免显示110%
            float percentage = (float)levelExp / long.Parse(levelInfo.level_exp);
            percentage = Mathf.Clamp01(percentage);
            ui_LevelProgressData.fillAmount = percentage;
            ui_LevelProgressText.text = $"{MathUtil.GetPercentage(percentage, 2)}%";
        }
    }

    /// <summary>
    /// 设置稀有度显示
    /// </summary>
    /// <param name="rarity">稀有度等级</param>
    public void SetRarity(int rarity)
    {
        if (rarity == 0)
            rarity = 1;
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        //卡片底板与场景背景使用稀有度主板颜色
        GameUIUtil.SetGradientColor(ui_CardBgBoard, rarityInfo.ui_board_color);
        GameUIUtil.SetGradientColor(ui_CardSceneBg, rarityInfo.ui_board_color);
        //稀有度条使用稀有度副板颜色
        GameUIUtil.SetGradientColor(ui_CardRate, rarityInfo.ui_board_other_color);
    }

    /// <summary>
    /// 设置生物名称
    /// </summary>
    /// <param name="name">生物名称</param>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置属性显示
    /// <para>按 creatureInfo.show_attribute 配置开关槽位：HP→生命/DR→防御/ATK→攻击/ASPD→Speed槽(攻速)/MSPD→Speed槽(移速)/MP→魔力/MPF→MPR槽(魔力回复)；未配置的槽位隐藏。</para>
    /// <para>Speed 槽为 ASPD/MSPD 共用(同一生物不应同时配置两者,同时配置时优先显示攻速ASPD)；MPR(魔力回复%)无对应槽位,配置后忽略。</para>
    /// <para>回复型生物(攻击方式为恢复类)：配置含 ATK 时攻击槽改显示治疗量条目(AddLife，RegainHP系)或回甲量条目(AddDef，RegainDR系)，值=当前ATK×攻击模式伤害加成倍率。</para>
    /// <para>魔王同样走配置(其物种行 id 1-7 配 4,5,2,11 = ATK/MSPD/MP/MPF),无特判分支。</para>
    /// <para>详情面板按「含深渊馈赠全局池」口径取属性(includeAbyssalBlessing=true)，与场上实际数值一致(如随机一只攻击力翻倍)。</para>
    /// </summary>
    public void SetAttribute()
    {
        //按 creatureInfo.show_attribute 配置开关槽位(未配置的属性隐藏),8 槽全部显式设置激活态(防详情面板池化复用残留)
        var listShowAttribute = creatureData.creatureInfo.GetShowAttributeList();
        //回复型:配置含 ATK 时攻击槽映射为治疗量(AddLife,RegainHP系)/回甲量(AddDef,RegainDR系);非回复型正常显示攻击槽
        bool isRegainHP = creatureData.creatureInfo.IsRegainHPAttackMode();
        bool isRegainDR = creatureData.creatureInfo.IsRegainDRAttackMode();
        bool isShowHP = listShowAttribute.Contains(CreatureAttributeTypeEnum.HP);
        bool isShowDR = listShowAttribute.Contains(CreatureAttributeTypeEnum.DR);
        bool isShowATK = listShowAttribute.Contains(CreatureAttributeTypeEnum.ATK);
        bool isShowASPD = listShowAttribute.Contains(CreatureAttributeTypeEnum.ASPD);
        bool isShowMSPD = listShowAttribute.Contains(CreatureAttributeTypeEnum.MSPD);
        bool isShowMP = listShowAttribute.Contains(CreatureAttributeTypeEnum.MP);
        bool isShowMPF = listShowAttribute.Contains(CreatureAttributeTypeEnum.MPF);
        ui_ViewCreatureCardItemAttribute_Life.gameObject.SetActive(isShowHP);
        ui_ViewCreatureCardItemAttribute_Def.gameObject.SetActive(isShowDR);
        ui_ViewCreatureCardItemAttribute_MP.gameObject.SetActive(isShowMP);
        ui_ViewCreatureCardItemAttribute_MPR.gameObject.SetActive(isShowMPF);
        ui_ViewCreatureCardItemAttribute_Atk.gameObject.SetActive(isShowATK && !isRegainHP && !isRegainDR);
        ui_ViewCreatureCardItemAttribute_AddLife.gameObject.SetActive(isShowATK && isRegainHP);
        ui_ViewCreatureCardItemAttribute_AddDef.gameObject.SetActive(isShowATK && isRegainDR);
        //Speed槽为 ASPD/MSPD 共用(同一生物不应同时配置两者,同时配置时优先显示攻速)
        ui_ViewCreatureCardItemAttribute_Speed.gameObject.SetActive(isShowASPD || isShowMSPD);
        //文本只填可见槽位
        if (isShowHP)
            ui_AttributeItemText_Life.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.HP, true)}";
        if (isShowDR)
            ui_AttributeItemText_Def.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.DR, true)}";
        if (isShowMP)
            ui_AttributeItemText_MP.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.MP, true)}";
        if (isShowMPF)
            ui_AttributeItemText_MPR.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.MPF, true)}";
        if (isShowASPD)
            ui_AttributeItemText_Speed.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.ASPD, true)}";
        else if (isShowMSPD)
            ui_AttributeItemText_Speed.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.MSPD, true)}";
        if (isShowATK)
        {
            if (isRegainHP || isRegainDR)
            {
                //回复量=当前ATK×攻击模式伤害加成倍率(配置0=按1倍)，与战斗实际恢复量口径一致
                var attackModeInfo = AttackModeInfoCfg.GetItemData(creatureData.creatureInfo.attack_mode);
                float damageAddRate = attackModeInfo != null ? attackModeInfo.GetDamageAddRate() : 1f;
                int regainValue = (int)(creatureData.GetAttribute(CreatureAttributeTypeEnum.ATK, true) * damageAddRate);
                if (isRegainDR)
                {
                    ui_AttributeItemText_AddDef.text = $"{regainValue}";
                }
                else
                {
                    ui_AttributeItemText_AddLife.text = $"{regainValue}";
                }
            }
            else
            {
                ui_AttributeItemText_Atk.text = $"{(int)creatureData.GetAttribute(CreatureAttributeTypeEnum.ATK, true)}";
            }
        }
    }

    /// <summary>
    /// 设置卡片图标
    /// </summary>
    /// <param name="creatureData">生物数据</param>
    public void SetCardIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForDetails(ui_Icon, ui_CardScene, creatureData);
    }

    /// <summary>
    /// 设置详情面板位置方向
    /// </summary>
    /// <param name="direction">方向（左/右）</param>
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
    /// 刷新UI布局
    /// <para>Details 下的内容容器(DetailsBase/NameDoomCouncil/RenmarkText/Details_Child_1/Level/Equip/Buff)全部隐藏时,Details 面板自身也一起隐藏(如魔王:等级/详情基础/称号关系等均不显示,装备与BUFF也为空时整个面板无内容,防残留空面板)。</para>
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
        //Details 下直接子容器全部隐藏时,Details 面板一起隐藏;有任一内容显示则保持/恢复显示
        bool isAllDetailsContentHide = true;
        for (int i = 0; i < ui_Details.childCount; i++)
        {
            if (ui_Details.GetChild(i).gameObject.activeSelf)
            {
                isAllDetailsContentHide = false;
                break;
            }
        }
        ui_Details.gameObject.SetActive(!isAllDetailsContentHide);
        //刷新UI
        UGUIUtil.RefreshUISize(ui_Details);
    }

    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    /// <param name="viewButton">点击的按钮</param>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_IconBtn)
        {
            OnClickForIconShow();
        }
    }

    /// <summary>
    /// 点击生物图标打开生物展示弹窗
    /// </summary>
    public void OnClickForIconShow()
    {
        DialogCreatureShowBean dialogCreatureShow=new DialogCreatureShowBean();
        dialogCreatureShow.creatureData = creatureData;
        UIHandler.Instance.ShowDialogCreatureShow(dialogCreatureShow);
    }
}
