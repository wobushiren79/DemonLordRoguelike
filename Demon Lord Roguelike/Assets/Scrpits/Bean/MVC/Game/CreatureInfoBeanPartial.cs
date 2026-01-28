using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
public partial class CreatureInfoBean
{
    //spine身体基础部位IDs
    protected List<long> listSpineBaseIds;
    //spine身体可替换部位类型
    protected List<ItemTypeEnum> listEquipItemsType;
    //攻击起始位置
    protected bool isInitAttackStartPosition = false;
    protected Vector3 attackStartPosition;
    //生物buff
    protected List<BuffBean> listCreatureBuff;

    public CreatureTypeEnum GetCreatureType()
    {
        return (CreatureTypeEnum)creature_type;
    }

    public CreatureFightTypeEnum GetAttackSearchCreatureFightType()
    {
        return (CreatureFightTypeEnum)attack_search_creature_fight_type;
    }

    public Vector3 GetAttackStartPosition()
    {
        if (!isInitAttackStartPosition)
        {
            attackStartPosition = attack_start_position.SplitForVector3(',');
            isInitAttackStartPosition = true;
        }
        return attackStartPosition;
    }

    /// <summary>
    /// 获取所有基础部位IDs
    /// </summary>
    public List<long> GetSpineBaseIds()
    {
        if (listSpineBaseIds == null)
        {
            listSpineBaseIds = new List<long>();
            if (!spine_base.IsNull())
            {
                listSpineBaseIds = spine_base.SplitForListLong(',');
            }
        }
        return listSpineBaseIds;
    }

    /// <summary>
    /// 获取基础武器部件ID
    /// </summary>
    public long GetEquipBaseWeaponId()
    {
        return equip_item_base_weapon;
    }

    /// <summary>
    /// 获取所有可装备道具类型
    /// </summary>
    public List<ItemTypeEnum> GetEquipItemsType()
    {
        if (listEquipItemsType == null)
        {
            listEquipItemsType = new List<ItemTypeEnum>();
            if (!equip_items_type.IsNull())
            {
                listEquipItemsType = equip_items_type.SplitForListEnum<ItemTypeEnum>(',');
            }
        }
        return listEquipItemsType;
    }

    /// <summary>
    /// 获取生物攻击搜索模式
    /// </summary>
    /// <returns></returns>
    public CreatureSearchType GetCreatureSearchType()
    {
        return (CreatureSearchType)attack_search_type;
    }

    /// <summary>
    /// 获取生物自带buff
    /// </summary>
    public List<BuffBean> GetCreatureBuffs()
    {
        if (creature_buff.IsNull())
        {
            return null;
        }
        if (listCreatureBuff == null)
        {
            listCreatureBuff = new List<BuffBean>();
            var buffIds = creature_buff.SplitForListLong(',');
            for (int i = 0; i < buffIds.Count; i++)
            {
                BuffBean buffData = new BuffBean(buffIds[i]);
                listCreatureBuff.Add(buffData);
            }
        }
        return listCreatureBuff;
    }
}
public partial class CreatureInfoCfg
{
}
