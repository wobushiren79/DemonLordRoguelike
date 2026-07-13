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
    //护盾受击特效位置偏移
    protected bool isInitShieldEffectPosition = false;
    protected Vector3 shieldEffectPosition;
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
    /// 获取护盾受击特效位置偏移(相对生物坐标)。配置 shield_effect_position 为空时回退默认偏移 (0, 0.5, 0)。
    /// </summary>
    public Vector3 GetShieldEffectPosition()
    {
        if (!isInitShieldEffectPosition)
        {
            //空配置回退默认偏移，否则按 "x,y,z" 解析
            shieldEffectPosition = shield_effect_position.IsNull() ? new Vector3(0, 0.5f, 0) : shield_effect_position.SplitForVector3(',');
            isInitShieldEffectPosition = true;
        }
        return shieldEffectPosition;
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
    /// 是否可搜索并转身攻击身后敌人（正面无目标时才向身后补搜，范围与正面一致）
    /// </summary>
    public bool IsAttackSearchBack()
    {
        //TODO(临时打桩)：excel_creature_info 已加 attack_search_back 列，待 Unity 重新生成 CreatureInfoBean 后改回 return attack_search_back == 1;
        return false;
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

    /// <summary>
    /// 判断是否可以装备某道具类型
    /// </summary>
    /// <param name="itemType">道具类型</param>
    /// <returns>是否可以装备</returns>
    public bool CanEquipItemType(ItemTypeEnum itemType)
    {
        List<ItemTypeEnum> equipTypes = GetEquipItemsType();
        return equipTypes.Contains(itemType);
    }

    /// <summary>
    /// 判断是否可以装备某武器类型
    /// </summary>
    /// <param name="weaponType">武器类型</param>
    /// <returns>是否可以装备</returns>
    public bool CanEquipWeaponType(ItemTypeWeaponEnum weaponType)
    {
        // equip_items_weapon_type 为 0 表示可以装备所有武器类型
        if (equip_items_weapon_type == 0)
        {
            return true;
        }
        return equip_items_weapon_type == (int)weaponType;
    }

    /// <summary>
    /// 判断是否可以装备某道具
    /// </summary>
    /// <param name="itemInfo">道具配置信息</param>
    /// <returns>是否可以装备</returns>
    public bool CanEquipItem(ItemsInfoBean itemInfo)
    {
        if (itemInfo == null)
        {
            return false;
        }

        ItemTypeEnum itemType = itemInfo.GetItemType();
        
        // 检查是否可以装备该类型的道具
        if (!CanEquipItemType(itemType))
        {
            return false;
        }

        // 检查种族模组是否匹配（装备 creature_model_id 为 0 表示通用装备，任何种族可装；否则须与生物 model_id 一致）
        if (itemInfo.creature_model_id != 0 && itemInfo.creature_model_id != model_id)
        {
            return false;
        }

        // 如果是武器类型，需要检查武器类型是否匹配
        if (itemType == ItemTypeEnum.Weapon)
        {
            ItemTypeWeaponEnum weaponType = itemInfo.GetWeaponType();
            if (!CanEquipWeaponType(weaponType))
            {
                return false;
            }
        }

        return true;
    }

    #region 体型
    /// <summary>
    /// 获取体型缩放倍率（在目标大小 size_spine 的基础上再相乘）
    /// <para>配置 body_size 规则：空 / "0" / 解析失败 => 1（默认大小）；</para>
    /// <para>含逗号 "min,max"（如 "0.9,1.1"） => 在 [min,max] 区间内随机一个倍率；</para>
    /// <para>单个数值（如 "1.1"） => 固定该倍率。</para>
    /// <para>注意：含随机区间时本方法每次调用都会重新随机，应在生物创建时调用一次并缓存（见 CreatureBean.bodySizeScale）。</para>
    /// </summary>
    /// <returns>体型缩放倍率（恒大于0，异常时回退为1）</returns>
    public float GetBodySizeRandomScale()
    {
        //空配置 => 默认1倍
        if (body_size.IsNull())
            return 1f;
        string sizeStr = body_size.Trim();
        if (sizeStr.Length == 0)
            return 1f;
        //区间随机 "min,max"
        if (sizeStr.Contains(","))
        {
            string[] rangeStr = sizeStr.Split(',');
            if (rangeStr.Length >= 2
                && float.TryParse(rangeStr[0].Trim(), out float min)
                && float.TryParse(rangeStr[1].Trim(), out float max))
            {
                //容错：min>max 时交换
                if (min > max)
                {
                    float temp = min;
                    min = max;
                    max = temp;
                }
                float randomScale = UnityEngine.Random.Range(min, max);
                return randomScale > 0 ? randomScale : 1f;
            }
            return 1f;
        }
        //固定倍率
        if (float.TryParse(sizeStr, out float fixedScale))
        {
            //0 或负数 => 默认1倍
            return fixedScale > 0 ? fixedScale : 1f;
        }
        return 1f;
    }
    #endregion
}
public partial class CreatureInfoCfg
{
}
