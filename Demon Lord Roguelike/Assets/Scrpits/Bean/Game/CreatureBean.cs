using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public partial class CreatureBean
{
    //生物ID
    public string creatureId;
    //ID
    public long id;
    //生物名字
    public string creatureName;
    //等级
    public int level;
    //星级
    public int starLevel;
    //稀有度
    public int rarity;

    //所有的皮肤数据（只保存身体基本皮肤 装备的皮肤保存再装备数据里 如果是初始默认装备也保存在这里）
    public Dictionary<CreatureSkinTypeEnum, CreatureSkinBean> dicSkinData = new Dictionary<CreatureSkinTypeEnum, CreatureSkinBean>();
    //装备数据
    public Dictionary<ItemTypeEnum, ItemBean> dicEquipItemData = new Dictionary<ItemTypeEnum, ItemBean>();

    public CreatureBean(long id)
    {
        this.id = id;
        this.creatureId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
    }

    #region 装备相关
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
    public void ClearSkin(bool isAddBase = true)
    {
        dicSkinData.Clear();
        if (isAddBase)
            AddSkinForBase();
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
    /// 添加皮肤
    /// </summary>
    public void AddSkin(long skinId)
    {
        var modelDetailsInfo = CreatureModelInfoCfg.GetItemData(skinId);
        if (modelDetailsInfo == null)
        {
            LogUtil.LogError($"添加皮肤失败 没有找到skinId_{skinId}的皮肤");
            return;
        }
        CreatureSkinTypeEnum targetSkinType = (CreatureSkinTypeEnum)modelDetailsInfo.part_type;
        if (dicSkinData.TryGetValue(targetSkinType, out CreatureSkinBean creatureSkin))
        {
            creatureSkin.skinId = skinId;
        }
        else
        {
            CreatureSkinBean creatureSkinBean = new CreatureSkinBean(skinId);
            dicSkinData.Add(targetSkinType, creatureSkinBean);
        }
    }


    /// <summary>
    /// 获取皮肤列表
    /// </summary>
    /// <param name="showType">0普通皮肤 1立绘皮肤（表情）</param>
    /// <returns></returns>
    public string[] GetSkinArray(int showType = 0, bool isNeedWeapon = true)
    {
        List<string> listSkin = new List<string>();

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
                            if (itemPartType == CreatureSkinTypeEnum.Weapon_L || itemPartType == CreatureSkinTypeEnum.Weapon_R)
                            {
                                continue;
                            }
                        }
                        //如果有帽子 不需要展示头发
                        if (dicEquipItemData.ContainsKey(ItemTypeEnum.Hat) && itemPartType == CreatureSkinTypeEnum.Hair)
                        {
                            continue;
                        }
                        //如果有装备武器 则不需要再添加基础武器
                        if (dicEquipItemData.ContainsKey(ItemTypeEnum.Weapon) && (itemPartType == CreatureSkinTypeEnum.Weapon_L || itemPartType == CreatureSkinTypeEnum.Weapon_R))
                        {
                            continue;
                        }
                    }
                    listSkin.Add(itemSkinInfo.res_name);
                }
            }
        }

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
            listSkin.Add(itemSkinInfo.res_name);
        }
        return listSkin.ToArray();
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
    public int GetASPD()
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
    /// 获取攻击CD
    /// </summary>
    /// <returns></returns>
    public float GetAttackCDTime()
    {
        return creatureInfo.attack_cd_time;
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


    /// <summary>
    /// 获取创建的魔力
    /// </summary>
    public int GetCreateMagic()
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(id);
        return creatureInfo.create_magic;
    }
    #endregion
}
