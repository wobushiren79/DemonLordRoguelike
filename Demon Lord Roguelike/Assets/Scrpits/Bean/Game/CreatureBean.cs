using System;
using System.Collections.Generic;
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

    public CreatureBean()
    {
        
    }

    public CreatureBean(long creatureId)
    {
        this.creatureId = creatureId;
        this.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
    }

    public CreatureBean(NpcInfoBean npcInfo)
    {
        creatureNpcData = new CreatureNpcBean(npcInfo.id);

        this.creatureId = npcInfo.creature_id;
        this.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        this.creatureName = npcInfo.name_language;
        //添加随机皮肤
        InitSkin(npcInfo);
        //添加装备
        InitEquip(npcInfo);
    }
    #region 杂项
    public NpcRelationshipEnum GetRelationshipForNpc()
    {
        return NpcRelationshipInfoCfg.GetNpcRelationshipEnum(relationship);
    }

    public CreatureNpcBean GetCreatureNpcData()
    {
        return creatureNpcData;
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
    /// <summary>
    /// 获取生命值
    /// </summary>
    public int GetHP()
    {
        return creatureInfo.GetHP();
    }

    public int GetHPOrigin()
    {
        return creatureInfo.GetHPOrigin();
    }

    /// <summary>
    /// 获取护甲值
    /// </summary>
    public int GetDR()
    {
        return creatureInfo.GetDR();
    }
    
    public int GetDROrigin()
    {
        return creatureInfo.GetDROrigin();
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
    /// 获取攻击
    /// </summary>
    public int GetATK()
    {
        return creatureInfo.ATK;
    }

    /// <summary>
    /// 获取攻击速度
    /// </summary>
    /// <returns></returns>
    public float GetASPD()
    {
        return creatureInfo.ASPD;
    }

    /// <summary>
    /// 获取移动速度
    /// </summary>
    public float GetMSPD()
    {
        return creatureInfo.MSPD;
    }

    /// <summary>
    /// 获取闪避率
    /// </summary>
    /// <returns></returns>
    public float GetEVA()
    {
        return 0;
    }

    /// <summary>
    /// 获取暴击率
    /// </summary>
    /// <returns></returns>
    public float GetCRT()
    {
        return 0;
    }

    /// <summary>
    /// 获取间隔搜索敌人时间
    /// </summary>
    public float GetAttackSearchTime()
    {
        //保底容错
        if (creatureInfo.attack_search_time <= 0)
            creatureInfo.attack_search_time = 0.2f;
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
    #endregion
}
