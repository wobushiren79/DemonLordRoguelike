using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public partial class CreatureBean
{
    #region 字段
    /// <summary>生物配置ID（对应 CreatureInfo 配置表）</summary>
    public long creatureId;
    /// <summary>生物运行时唯一ID（每个实例独立，用于战斗/存档检索）</summary>
    public string creatureUUId;
    /// <summary>生物名字</summary>
    public string creatureName;
    /// <summary>等级</summary>
    public int level;
    /// <summary>当前等级经验</summary>
    public long levelExp;
    /// <summary>星级</summary>
    public int starLevel;
    /// <summary>稀有度（RarityEnum）</summary>
    public int rarity;
    /// <summary>生物关系/好感度（仅NPC有意义）</summary>
    public int relationship;

    /// <summary>所有皮肤数据（只保存身体基础皮肤；装备的皮肤保存在装备数据里，初始默认装备也保存在这里）</summary>
    public Dictionary<CreatureSkinTypeEnum, SpineSkinBean> dicSkinData = new Dictionary<CreatureSkinTypeEnum, SpineSkinBean>();
    /// <summary>装备数据（key=装备部位类型）</summary>
    public Dictionary<ItemTypeEnum, ItemBean> dicEquipItemData = new Dictionary<ItemTypeEnum, ItemBean>();
    /// <summary>NPC数据（只有NPC才有，普通生物为 null）</summary>
    public CreatureNpcBean creatureNpcData;
    /// <summary>生物状态</summary>
    public CreatureStateEnum creatureState = CreatureStateEnum.Idle;
    /// <summary>生物属性（手动加点等附加属性来源）</summary>
    public CreatureAttributeBean creatureAttribute = new CreatureAttributeBean();
    /// <summary>生物稀有度BUFF（key=稀有度，扭蛋抽取时按命中的稀有度随机授予对应BUFF）</summary>
    public Dictionary<RarityEnum, BuffBean> dicRarityBuff = new Dictionary<RarityEnum, BuffBean>();
    /// <summary>体型缩放倍率（在模型目标大小 size_spine 基础上再相乘；目前仅NPC配置 body_size，普通生物恒为1）。
    /// 含随机区间的配置在创建时解析一次并缓存到此，保证后续重复渲染体型稳定。</summary>
    public float bodySizeScale = 1f;
    #endregion

    #region 构造与初始化
    /// <summary>
    /// 无参构造（供序列化/对象池复用，需后续调用 SetData 重建数据）
    /// </summary>
    public CreatureBean()
    {

    }

    /// <summary>
    /// 构造：按生物配置ID创建
    /// </summary>
    public CreatureBean(long creatureId)
    {
        SetData(creatureId);
    }

    /// <summary>
    /// 构造：按NPC配置创建
    /// </summary>
    public CreatureBean(NpcInfoBean npcInfo)
    {
        SetData(npcInfo);
    }

    /// <summary>
    /// 设置数据（按生物配置ID，生成唯一UUID）
    /// </summary>
    public void SetData(long creatureId)
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        this.creatureId = creatureId;
        this.creatureName = creatureInfo.name_language;
        this.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
    }

    /// <summary>
    /// 设置数据(NPC：写入NPC数据、等级，并初始化皮肤与装备)
    /// </summary>
    public void SetData(NpcInfoBean npcInfo)
    {
        creatureNpcData = new CreatureNpcBean(npcInfo.id);

        this.creatureId = npcInfo.creature_id;
        this.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        this.creatureName = npcInfo.name_language;
        this.level = npcInfo.level;
        //解析并缓存体型缩放倍率（区间随机只在创建时定一次）
        this.bodySizeScale = npcInfo.GetBodySizeRandomScale();
        //添加随机皮肤
        InitSkin(npcInfo);
        //添加装备
        InitEquip(npcInfo);
    }
    #endregion


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

    /// <summary>
    /// 获取NPC数据（非NPC生物返回 null）
    /// </summary>
    public CreatureNpcBean GetCreatureNpcData()
    {
        return creatureNpcData;
    }

    /// <summary>
    /// 获取体型缩放倍率（带保护：旧存档或异常值≤0时回退为1，避免模型缩放为0而不可见）
    /// </summary>
    /// <returns>体型缩放倍率（恒大于0）</returns>
    public float GetBodySizeScale()
    {
        return bodySizeScale > 0 ? bodySizeScale : 1f;
    }
    #endregion

    #region BUFF相关
    /// <summary>
    /// 获取所有buff
    /// </summary>
    /// <param name="hasSelfBuff">是否有自身BUFF</param>
    /// <param name="getRarityBuff">是否有稀有度BUFF</param>
    /// <param name="getBuffAttributeBase">是否包含属性BUFF(纯加属性)</param>
    /// <returns></returns>
    public List<BuffBean> GetListBuffData(bool getSelfBuff = true, bool getRarityBuff = true, bool getBuffAttributeBase = true)
    {
        List<BuffBean> listAllBuff = new List<BuffBean>();
        //获取自身BUFF
        if (getSelfBuff)
        {
            List<BuffBean> listSelfBuff = creatureInfo.GetCreatureBuffs();
            if (!listSelfBuff.IsNull())
            {
                for (int i = 0; i < listSelfBuff.Count; i++)
                {
                    var buffData = listSelfBuff[i];
                    //如果不获取含属性BUFF 则判断是否是属性BUFF 如果是则跳过
                    if (getBuffAttributeBase == false && buffData.IsBuffEntityAttributeBase() != CreatureAttributeTypeEnum.None)
                    {
                        continue;
                    }
                    listAllBuff.Add(buffData);
                }
            }
        }
        //获取稀有度BUFF
        if (getRarityBuff)
        {
            foreach (var item in dicRarityBuff)
            {
                var buffData = item.Value;
                //如果不获取含属性BUFF 则判断是否是属性BUFF 如果是则跳过
                if (getBuffAttributeBase == false && buffData.IsBuffEntityAttributeBase() != CreatureAttributeTypeEnum.None)
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
            CreatureAttributeTypeEnum buffAttributeType = buffData.IsBuffEntityAttributeBase();
            if (buffAttributeType != CreatureAttributeTypeEnum.None && buffAttributeType == creatureAttributeType)
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

    /// <summary>
    /// 初始化装备（先清空原装备，再按道具ID列表逐件装备）
    /// </summary>
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
    /// 改变装备（无需关心被替换下来的旧装备时的简化重载）
    /// </summary>
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

    /// <summary>
    /// 获取装备属性
    /// </summary>
    public float GetEquipAttribute(CreatureAttributeTypeEnum attributeType)
    {
        float totalAttribute = 0;
        foreach (var item in dicEquipItemData)
        {
            var itemEquip = item.Value;
            totalAttribute += itemEquip.GetAttribute(attributeType);
        }
        return totalAttribute;
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
    public void InitSkin(List<long> listSkin, bool isRandomColor = true)
    {
        //清理所有皮肤
        ClearSkin();
        //添加基础皮肤
        AddSkinForBase();
        //添加配置皮肤
        AddSkin(listSkin, isRandomColor);
    }

    /// <summary>
    /// 添加皮肤
    /// </summary>
    public void AddSkin(List<long> skinIds, bool isRandomColor = true)
    {
        if (skinIds == null)
        {
            return;
        }
        for (int i = 0; i < skinIds.Count; i++)
        {
            var itemSkinId = skinIds[i];
            AddSkin(itemSkinId, isRandomColor);
        }
    }

    /// <summary>
    /// 添加皮肤
    /// </summary>
    public void AddSkin(long skinId, bool isRandomColor = true)
    {
        SpineSkinBean spineSkinData = new SpineSkinBean(skinId);
        if(isRandomColor)
        {
            var itemSkinInfo = CreatureModelInfoCfg.GetItemData(skinId);
            if (itemSkinInfo.color_state == 1)
            {
                spineSkinData.hasColor = true;
                spineSkinData.skinColor = ColorBean.Random();
            }
        }
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

    /// <summary>
    /// 获取最终属性值
    /// <para>叠加顺序：基础值(NPC优先，否则取 creatureInfo) → 角色加点(creatureAttribute) → 装备属性 → 自身/稀有度BUFF修正 → [可选]深渊馈赠全局池BUFF。</para>
    /// <para>CRT/EVA 基础值为0（按需由加点/装备/BUFF提供）；MP/CMP/MPF 等魔力相关仅战斗中有意义。</para>
    /// <para>深渊馈赠全局池(dicAbyssalBlessingBuffsActivie)仅当 includeAbyssalBlessing=true 时按需叠加，供非战斗缓存链路(如复活CD查询)使用；
    /// 战斗链路(FightCreatureBean.RefreshBaseAttribute)走 ModifierPipeline 独立叠加深渊馈赠，调用时须保持默认 false 以免重复计算。</para>
    /// </summary>
    /// <param name="creatureAttributeType">属性类型</param>
    /// <param name="includeAbyssalBlessing">是否叠加深渊馈赠全局池BUFF（非战斗链路按需开启，默认 false 避免与战斗缓存重复计算）</param>
    /// <returns>经全部来源叠加后的属性值</returns>
    public float GetAttribute(CreatureAttributeTypeEnum creatureAttributeType, bool includeAbyssalBlessing = false)
    {
        float targetData = 0;
        //如果有NPC数据 优先使用NPC数据里的属性
        var npcInfo = creatureNpcData?.npcInfo;
        switch (creatureAttributeType)
        {
            case CreatureAttributeTypeEnum.HP://获取生命值
                targetData = npcInfo != null ? npcInfo.HP : creatureInfo.HP;
                break;
            case CreatureAttributeTypeEnum.DR://获取护甲值
                targetData = npcInfo != null ? npcInfo.DR : creatureInfo.DR;
                break;
            case CreatureAttributeTypeEnum.ATK://获取攻击力
                targetData = npcInfo != null ? npcInfo.ATK : creatureInfo.ATK;
                break;
            case CreatureAttributeTypeEnum.ASPD://获取攻击速度
                targetData = npcInfo != null ? npcInfo.ASPD : creatureInfo.ASPD;
                break;
            case CreatureAttributeTypeEnum.MSPD://获取移动速度
                targetData = npcInfo != null ? npcInfo.MSPD : creatureInfo.MSPD;
                break;
            case CreatureAttributeTypeEnum.MP://魔力上限(魔王创建魔物的资源池, 仅战斗中有效)
                targetData = npcInfo != null ? npcInfo.MP : creatureInfo.MP;
                break;
            case CreatureAttributeTypeEnum.MPR://魔力回复%
                targetData = creatureInfo.MPR;
                break;
            case CreatureAttributeTypeEnum.MPF://魔力回复
                targetData = creatureInfo.MPF;
                break;
            case CreatureAttributeTypeEnum.RCD://复活CD基础值(深渊馈赠全局池由 includeAbyssalBlessing 控制在下方按需叠加)
                targetData = creatureInfo.RCD;
                break;
            case CreatureAttributeTypeEnum.CMP://召唤魔力消耗：基础CMP + 基础CMP×(等级增加倍率+稀有度增加倍率)，再经下方BUFF管线修正(如扭蛋稀有度CMP减益)
                {
                    float baseCMP = creatureInfo.CMP;
                    targetData = baseCMP + baseCMP * GetCreateMPAddRate();
                }
                break;
            case CreatureAttributeTypeEnum.EVA://获取闪避概率
                targetData = 0;
                break;
            case CreatureAttributeTypeEnum.CRT://获取暴击率
                targetData = 0;
                break;
            default:
                targetData = 0;
                break;
        }
        //获取角色属性加成
        targetData += creatureAttribute.GetAttribute(creatureAttributeType);
        //获取装备属性
        targetData += GetEquipAttribute(creatureAttributeType);
        //获取BUFF改变后的属性加成（自身/稀有度BUFF）
        targetData = GetBuffChangeAttribute(creatureAttributeType, targetData);
        //深渊馈赠全局池加成：仅非战斗链路按需叠加（不走战斗缓存的 ModifierPipeline，避免重复计算）
        if (includeAbyssalBlessing)
        {
            targetData = GetAbyssalBlessingChangeAttribute(creatureAttributeType, targetData);
        }
        return targetData;
    }

    /// <summary>
    /// 叠加深渊馈赠全局池(dicAbyssalBlessingBuffsActivie)中匹配该属性的BUFF修正
    /// <para>深渊馈赠为全局池，是非战斗缓存链路特有的按需叠加；战斗链路(FightCreatureBean.RefreshBaseAttribute)经 ModifierPipeline 独立处理，不调用此方法以免重复。</para>
    /// <para>注：当前 RCD 类BUFF均为纯百分比(trigger_value=0)叠乘、可交换，深渊馈赠相对自身BUFF的先后不影响结果。</para>
    /// </summary>
    /// <param name="creatureAttributeType">属性类型</param>
    /// <param name="targetData">已叠加自身来源后的属性值</param>
    /// <returns>再叠加深渊馈赠全局池后的属性值</returns>
    public float GetAbyssalBlessingChangeAttribute(CreatureAttributeTypeEnum creatureAttributeType, float targetData)
    {
        var abyssalBlessingBuffs = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
        if (!abyssalBlessingBuffs.List.IsNull())
        {
            for (int i = 0; i < abyssalBlessingBuffs.List.Count; i++)
            {
                var itemAbyssalBlessingBuff = abyssalBlessingBuffs.List[i];
                for (int j = 0; j < itemAbyssalBlessingBuff.Count; j++)
                {
                    BuffBaseEntity buffEntity = itemAbyssalBlessingBuff[j];
                    if (buffEntity is BuffEntityAttribute buffEntityAttribute && buffEntityAttribute.attributeType == creatureAttributeType)
                    {
                        targetData = buffEntityAttribute.ChangeData(creatureAttributeType, targetData);
                    }
                }
            }
        }
        return targetData;
    }

    /// <summary>
    /// 获取属性值（int 版本，四舍五入），供消耗/数量等需要整数的场景调用（如召唤魔力消耗 CMP）
    /// </summary>
    /// <param name="creatureAttributeType">属性类型</param>
    /// <returns>GetAttribute 结果四舍五入后的整数</returns>
    public int GetAttributeInt(CreatureAttributeTypeEnum creatureAttributeType)
    {
        return UnityEngine.Mathf.RoundToInt(GetAttribute(creatureAttributeType));
    }

    /// <summary>
    /// 获取召唤魔力增加倍率（等级增加倍率 + 稀有度增加倍率）
    /// <para>用于召唤魔力消耗计算：最终CMP = 基础CMP + 基础CMP ×（本倍率）。</para>
    /// <para>等级增加倍率取自 LevelInfo.CMP_rate（按当前 level 查表；level 0 或越界无配置时记 0，即不增加）。</para>
    /// <para>稀有度增加倍率取自 RarityInfo.CMP_rate（按 rarity 查表；rarity≤0 视为 N=1，N 的增加倍率为 0）。</para>
    /// <para>两项相加；均为 0 时表示不增加召唤消耗（保持基础CMP）。</para>
    /// </summary>
    /// <returns>等级增加倍率 + 稀有度增加倍率</returns>
    public float GetCreateMPAddRate()
    {
        //等级增加倍率：按当前等级查 LevelInfo（level 0 或越界无配置时记 0）
        float levelRate = 0;
        var levelInfo = LevelInfoCfg.GetItemData(level);
        if (levelInfo != null)
            levelRate = levelInfo.CMP_rate;
        //稀有度增加倍率：rarity 为 0 时视为 N(1)，与卡片显示口径一致
        int rarityForLookup = rarity <= 0 ? 1 : rarity;
        float rarityRate = 0;
        var rarityInfo = RarityInfoCfg.GetItemData(rarityForLookup);
        if (rarityInfo != null)
            rarityRate = rarityInfo.CMP_rate;
        return levelRate + rarityRate;
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
