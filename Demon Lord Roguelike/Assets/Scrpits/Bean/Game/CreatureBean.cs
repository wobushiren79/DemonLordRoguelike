using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

[Serializable]
public partial class CreatureBean
{
    //生物ID
    public long creatureId;
    //生物唯一ID
    public string creatureUUId;
    //生物名字
    public string creatureName;
    //等级
    public int level;
    //等级经验
    public long levelExp;
    //星级
    public int starLevel;
    //稀有度
    public int rarity;
    //生物关系
    public int relationship;

    //所有的皮肤数据（只保存身体基本皮肤 装备的皮肤保存再装备数据里 如果是初始默认装备也保存在这里）
    public Dictionary<CreatureSkinTypeEnum, SpineSkinBean> dicSkinData = new Dictionary<CreatureSkinTypeEnum, SpineSkinBean>();
    //装备数据
    public Dictionary<ItemTypeEnum, ItemBean> dicEquipItemData = new Dictionary<ItemTypeEnum, ItemBean>();
    //NPC数据(只有NPC才有)
    public CreatureNpcBean creatureNpcData;
    //生物状态
    public CreatureStateEnum creatureState = CreatureStateEnum.Idle;
    //生物属性
    public CreatureAttributeBean creatureAttribute = new CreatureAttributeBean();
    //生物稀有度BUFF
    public Dictionary<RarityEnum, BuffBean> dicRarityBuff = new Dictionary<RarityEnum, BuffBean>();

    public CreatureBean()
    {

    }

    public CreatureBean(long creatureId)
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        this.creatureId = creatureId;
        this.creatureName = creatureInfo.name_language;
        this.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
    }

    public CreatureBean(NpcInfoBean npcInfo)
    {
        creatureNpcData = new CreatureNpcBean(npcInfo.id);

        this.creatureId = npcInfo.creature_id;
        this.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        this.creatureName = npcInfo.name_language;
        this.level = npcInfo.level;

        //添加随机皮肤
        InitSkin(npcInfo);
        //添加装备
        InitEquip(npcInfo);
    }
    #region 杂项
    /// <summary>
    /// 添加好感
    /// </summary>
    public int AddRelationship(int addData)
    {
        relationship += addData;
        if (relationship < 0)
        {
            relationship = 0;
        }
        return relationship;
    }

    /// <summary>
    /// 获取NPC关系枚举
    /// </summary>
    /// <returns></returns>
    public NpcRelationshipEnum GetRelationshipForNpc()
    {
        return NpcRelationshipInfoCfg.GetNpcRelationshipEnum(relationship);
    }

    public CreatureNpcBean GetCreatureNpcData()
    {
        return creatureNpcData;
    }
    #endregion

    #region BUFF相关
    /// <summary>
    /// 获取所有buff
    /// </summary>
    /// <param name="hasSelfBuff">是否有自身BUFF</param>
    /// <param name="getRarityBuff">是否有稀有度BUFF</param>
    /// <param name="getBuffAttribute">是否包含属性BUFF</param>
    /// <returns></returns>
    public List<BuffBean> GetListBuffData(bool getSelfBuff = true, bool getRarityBuff = true, bool getBuffAttribute = true)
    {
        List<BuffBean> listAllBuff = new List<BuffBean>();
        //获取自身BUFF
        if (getSelfBuff)
        {
            List<BuffBean> listSelfBuff = creatureInfo.GetCreatureBuffs();
            if (!listSelfBuff.IsNull())
            {
                listAllBuff.AddRange(listSelfBuff);
            }
        }
        //获取稀有度BUFF
        if (getRarityBuff)
        {
            foreach (var item in dicRarityBuff)
            {
                var buffData = item.Value;
                //如果不获取含属性BUFF 则判断是否是属性BUFF 如果是则跳过
                if (getBuffAttribute == false && buffData.IsBuffEntityAttribute() != CreatureAttributeTypeEnum.None)
                {
                    continue;
                }
                listAllBuff.Add(buffData);
            }
        }
        return listAllBuff;
    }

    /// <summary>
    /// 获取BUFF里改变的属性
    /// </summary>
    public float GetBuffChangeAttribute(CreatureAttributeTypeEnum creatureAttributeType, float targetData)
    {
        var listBuff = GetListBuffData();
        for (int i = 0; i < listBuff.Count; i++)
        {
            var buffData = listBuff[i];
            CreatureAttributeTypeEnum buufAttributeType = buffData.IsBuffEntityAttribute();
            if (buufAttributeType!= CreatureAttributeTypeEnum.None && buufAttributeType == creatureAttributeType)
            {
                targetData = BuffEntityAttribute.ChangeData(buffData, creatureAttributeType, targetData);
            }
        }
        return targetData;
    }
    #endregion

    #region 装备相关

    /// <summary>
    /// 清空装备
    /// </summary>
    public void ClearEquip()
    {
        dicEquipItemData.Clear();
    }

    /// <summary>
    /// 初始化装备
    /// </summary>
    public void InitEquip(NpcInfoBean npcInfo)
    {
        List<long> listEquipItems = npcInfo.GetEquipItems();
        InitEquip(listEquipItems);
    }

    public void InitEquip(List<long> listEquipItems)
    {
        ClearEquip();
        for (int i = 0; i < listEquipItems.Count; i++)
        {
            var itemId = listEquipItems[i];
            ItemsInfoBean itemsInfo = ItemsInfoCfg.GetItemData(itemId);
            ItemBean itemData = new ItemBean(itemsInfo.id);
            ChangeEquip(itemsInfo.GetItemType(), itemData, out ItemBean beforeItem);
        }
    }

    /// <summary>
    /// 改变装备
    /// </summary>
    public void ChangeEquip(ItemTypeEnum itemType, ItemBean changeItem, out ItemBean beforeItem)
    {
        beforeItem = null;
        //首先卸下原来的装备
        if (dicEquipItemData.TryGetValue(itemType, out var equipItem))
        {
            beforeItem = equipItem;
            dicEquipItemData.Remove(itemType);
        }
        //如果需要 装备新得装备
        if (changeItem != null && changeItem.itemId > 0)
        {
            dicEquipItemData.Add(itemType, changeItem);
        }
    }

    public void ChangeEquip(ItemTypeEnum itemType, ItemBean changeItem)
    {
        ChangeEquip(itemType, changeItem, out ItemBean beforeItem);
    }

    /// <summary>
    /// 卸下所有装备到背包里
    /// </summary>
    public void RemoveAllEquipToBackpack()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        foreach (var item in dicEquipItemData)
        {
            var itemEquipData = item.Value;
            userData.AddBackpackItem(itemEquipData);
        }
        dicEquipItemData.Clear();
    }

    /// <summary>
    /// 获取装备
    /// </summary>
    public ItemBean GetEquip(ItemTypeEnum itemType)
    {
        //首先卸下原来的装备
        if (dicEquipItemData.TryGetValue(itemType, out var equipItem))
        {
            return equipItem;
        }
        return null;
    }
    #endregion

    #region  皮肤相关
    /// <summary>
    /// 清空皮肤
    /// </summary>
    public void ClearSkin()
    {
        dicSkinData.Clear();
    }

    /// <summary>
    /// 添加基础皮肤
    /// </summary>
    public void AddSkinForBase(bool hasWeapon = true)
    {
        //添加基础皮肤
        List<long> listSpineBaseIds = creatureInfo.GetSpineBaseIds();
        if (!listSpineBaseIds.IsNull())
        {
            listSpineBaseIds.ForEach((index, itemData) =>
            {
                AddSkin(itemData);
            });
        }
        //容错处理 如果没有设置数据 默认添加第一个基础皮肤
        else
        {
            var listBaseSkin = CreatureModelInfoCfg.GetData(creatureInfo.model_id, CreatureSkinTypeEnum.Base);
            if (!listBaseSkin.IsNull())
            {
                AddSkin(listBaseSkin[0].id);
            }
        }
        //添加武器
        if (hasWeapon)
        {
            long weaponId = creatureInfo.GetEquipBaseWeaponId();
            if (weaponId != 0)
            {
                var weponInfo = ItemsInfoCfg.GetItemData(weaponId);
                if (weponInfo != null)
                {
                    AddSkin(weponInfo.creature_model_info_id);
                }
                else
                {
                    LogUtil.LogError($"添加基础皮肤武器失败 weaponId_{weaponId}");
                }
            }
        }
    }

    /// <summary>
    /// 初始化皮肤-NPC
    /// </summary>
    public void InitSkin(NpcInfoBean npcInfo)
    {
        //获取皮肤
        List<long> listSkin = npcInfo.GetSkins();
        InitSkin(listSkin);
    }

    /// <summary>
    /// 初始化皮肤-自定义
    /// </summary>
    public void InitSkin(List<long> listSkin)
    {
        //清理所有皮肤
        ClearSkin();
        //添加基础皮肤
        AddSkinForBase();
        //添加配置皮肤
        AddSkin(listSkin);
    }

    /// <summary>
    /// 添加皮肤
    /// </summary>
    public void AddSkin(List<long> skinIds)
    {
        if (skinIds == null)
        {
            return;
        }
        for (int i = 0; i < skinIds.Count; i++)
        {
            var itemSkinId = skinIds[i];
            AddSkin(itemSkinId);
        }
    }

    /// <summary>
    /// 添加皮肤
    /// </summary>
    public void AddSkin(long skinId)
    {
        SpineSkinBean spineSkinData = new SpineSkinBean(skinId);
        AddSkin(spineSkinData);
    }

    /// <summary>
    /// 添加皮肤
    /// </summary>
    public void AddSkin(SpineSkinBean spineSkinData)
    {
        var modelDetailsInfo = CreatureModelInfoCfg.GetItemData(spineSkinData.skinId);
        if (modelDetailsInfo == null)
        {
            LogUtil.LogError($"添加皮肤失败 没有找到skinId_{spineSkinData.skinId}的皮肤");
            return;
        }
        CreatureSkinTypeEnum targetSkinType = (CreatureSkinTypeEnum)modelDetailsInfo.part_type;
        //如果已经有这个类型的皮肤类型，则覆盖原来的
        if (dicSkinData.ContainsKey(targetSkinType))
        {
            dicSkinData[targetSkinType] = spineSkinData;
        }
        //如果没有这个类型的皮肤类型 添加
        else
        {
            dicSkinData.Add(targetSkinType, spineSkinData);
        }
    }

    /// <summary>
    /// 改变皮肤颜色
    /// </summary>
    public void ChangeSkinColor(CreatureSkinTypeEnum creatureSkinType, Color skinColor)
    {
        //如果已经有这个类型的皮肤类型，则覆盖原来的
        if (dicSkinData.TryGetValue(creatureSkinType, out var spineSkinData))
        {
            if (spineSkinData != null)
            {
                var creatureModelInfo = CreatureModelInfoCfg.GetItemData(spineSkinData.skinId);
                if (creatureModelInfo != null && creatureModelInfo.color_state != 0)
                {
                    spineSkinData.hasColor = true;
                    spineSkinData.skinColor.SetColor(skinColor);
                }
                else
                {
                    LogUtil.Log($"creatureName:{creatureName} 不支持改变 {creatureSkinType.ToString()} 皮肤颜色");
                }
            }
        }
        //如果没有这个类型的皮肤类型
        else
        {
            LogUtil.LogError($"creatureName:{creatureName} 没有找到类型{creatureSkinType.ToString()} 皮肤类型");
        }
    }

    /// <summary>
    /// 获取皮肤列表
    /// </summary>
    /// <param name="showType">0普通皮肤 1立绘皮肤（表情）</param>
    /// <returns></returns>
    public Dictionary<string, SpineSkinBean> GetSkinData(int showType = 0,
        bool isNeedWeapon = true, bool isNeedEquip = true)
    {
        Dictionary<string, SpineSkinBean> dicSkin = new Dictionary<string, SpineSkinBean>();
        //处理所有身体皮肤
        foreach (var itemSkin in dicSkinData)
        {
            var itemPartType = itemSkin.Key;
            var itemSkinData = itemSkin.Value;
            CreatureModelInfoBean itemSkinInfo = CreatureModelInfoCfg.GetItemData(itemSkinData.skinId);
            if (itemSkinInfo == null)
            {
                LogUtil.LogError($"获取CreatureModelInfoCfg数据失败 没有找到ID_{itemSkinData.skinId} 的数据");
            }
            else
            {
                if (itemSkinInfo.show_type == showType)
                {
                    //特殊处理
                    if (showType == 0)
                    {
                        //是否需要展示武器
                        if (!isNeedWeapon)
                        {
                            switch (itemPartType)
                            {
                                case CreatureSkinTypeEnum.Weapon_L:
                                case CreatureSkinTypeEnum.Weapon_R:
                                    continue;
                            }
                        }

                        //如果有帽子 不需要展示头发
                        if (isNeedEquip && dicEquipItemData.ContainsKey(ItemTypeEnum.Hat) && itemPartType == CreatureSkinTypeEnum.Hair)
                        {
                            continue;
                        }
                        //如果有装备武器 则不需要再添加基础武器
                        if (dicEquipItemData.ContainsKey(ItemTypeEnum.Weapon) && (itemPartType == CreatureSkinTypeEnum.Weapon_L || itemPartType == CreatureSkinTypeEnum.Weapon_R))
                        {
                            continue;
                        }
                    }
                    dicSkin.Add(itemSkinInfo.res_name, itemSkinData);
                }
            }
        }
        //是否需要展示装备
        if (isNeedEquip)
        {
            //处理装备
            foreach (var itemEquip in dicEquipItemData)
            {
                var itemType = itemEquip.Key;
                var itemData = itemEquip.Value;
                //是否需要展示武器
                if (!isNeedWeapon)
                {
                    if (itemType == ItemTypeEnum.Weapon)
                    {
                        continue;
                    }
                }
                ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(itemData.itemId);
                CreatureModelInfoBean itemSkinInfo = CreatureModelInfoCfg.GetItemData(itemInfo.creature_model_info_id);
                if (itemSkinInfo == null)
                {
                    LogUtil.LogError($"没有找到CreatureModelInfoCfg creature_model_info_id_{itemInfo.creature_model_info_id}");
                    continue;
                }
                SpineSkinBean spineSkinData = new SpineSkinBean(0);
                dicSkin.Add(itemSkinInfo.res_name, spineSkinData);
            }
        }
        return dicSkin;
    }

    #endregion

    #region 属性相关

    public float GetAttribute(CreatureAttributeTypeEnum creatureAttributeType)
    {
        float targetData = 0;
        switch (creatureAttributeType)
        {
            case CreatureAttributeTypeEnum.HP://获取生命值
                targetData= creatureInfo.HP + creatureAttribute.GetAttribute(CreatureAttributeTypeEnum.HP);
                break;
            case CreatureAttributeTypeEnum.DR://获取护甲值
                targetData= creatureInfo.DR + creatureAttribute.GetAttribute(CreatureAttributeTypeEnum.DR);
                break;
            case CreatureAttributeTypeEnum.ATK://获取攻击力
                targetData= creatureInfo.ATK + creatureAttribute.GetAttribute(CreatureAttributeTypeEnum.ATK);
                break;
            case CreatureAttributeTypeEnum.ASPD://获取攻击速度
                targetData= creatureInfo.ASPD + creatureAttribute.GetAttribute(CreatureAttributeTypeEnum.ASPD);
                break;
            case CreatureAttributeTypeEnum.MSPD://获取移动速度
                targetData= creatureInfo.MSPD + creatureAttribute.GetAttribute(CreatureAttributeTypeEnum.MSPD);
                break;
            case CreatureAttributeTypeEnum.EVA://获取闪避概率
                targetData= 0;
                break;
            case CreatureAttributeTypeEnum.CRT://获取暴击率
                targetData= 0;
                break;
        }
        //获取BUFF改变后的属性加成
        targetData = GetBuffChangeAttribute(creatureAttributeType, targetData);
        return targetData;
    }

    /// <summary>
    /// 获取复活CD 不建议每帧获取
    /// </summary>
    public float GetRCD()
    {
        float RCDTime = creatureInfo.RCD;
        //深渊馈赠buff加成
        var abyssalBlessingBuffs = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
        if (!abyssalBlessingBuffs.List.IsNull())
        {
            for (int i = 0; i < abyssalBlessingBuffs.List.Count; i++)
            {
                var itemAbyssalBlessingBuff = abyssalBlessingBuffs.List[i];
                for (int j = 0; j < itemAbyssalBlessingBuff.Count; j++)
                {
                    BuffBaseEntity buffEntity = itemAbyssalBlessingBuff[j];
                    if (buffEntity is BuffEntityAttribute buffEntityAttribute && buffEntityAttribute.attributeType == CreatureAttributeTypeEnum.RCD)
                    {
                        RCDTime = buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.RCD, RCDTime);
                    }
                }
            }
        }
        return RCDTime;
    }

    /// <summary>
    /// 获取间隔搜索敌人时间
    /// </summary>
    public float GetAttackSearchTime()
    {
        //保底容错
        if (creatureInfo.attack_search_time <= 0)
            creatureInfo.attack_search_time = 1f;
        return creatureInfo.attack_search_time;
    }

    /// <summary>
    /// 获取攻击动画出手时间
    /// </summary>
    /// <returns></returns>
    public float GetAttackAnimTime()
    {
        return creatureInfo.anim_attack_time;
    }

    /// <summary>
    /// 获取攻击准备时间
    /// </summary>
    /// <returns></returns>
    public float GetAttackPreTime()
    {
        //保底容错
        if (creatureInfo.attack_pre_time <= 0)
            creatureInfo.attack_pre_time = 1f;
        return creatureInfo.attack_pre_time;
    }
    #endregion
}
