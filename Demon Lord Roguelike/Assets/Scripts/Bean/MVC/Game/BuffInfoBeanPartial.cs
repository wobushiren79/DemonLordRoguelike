using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BUFF堆叠策略
/// 当向已经存在同ID BUFF的生物再次添加同ID BUFF时如何处理
/// </summary>
public enum BuffStackMode
{
    /// <summary>
    /// 刷新：刷新剩余次数/时间，不叠层（默认，兼容旧行为）
    /// </summary>
    Refresh = 0,
    /// <summary>
    /// 叠层：层数+1（受 stack_max 限制），每层独立加成
    /// </summary>
    Stack = 1,
    /// <summary>
    /// 独立：完全独立实例，分别计时（多源 DOT）
    /// </summary>
    Independent = 2,
    /// <summary>
    /// 忽略：已存在则忽略新添加（一次性免疫）
    /// </summary>
    Ignore = 3,
    /// <summary>
    /// 替换最强：仅当新BUFF的trigger_value更大时替换旧实例（同类减速取最强）
    /// </summary>
    ReplaceStrongest = 4,
}

public partial class BuffInfoBean
{
    protected Color colorBody = Color.white;

    /// <summary>
    /// 获取堆叠策略
    /// </summary>
    public BuffStackMode GetStackMode()
    {
        return (BuffStackMode)stack_mode;
    }

    /// <summary>
    /// 获取最大堆叠层数（0=无上限，仅 Stack 模式生效）
    /// </summary>
    public int GetStackMax()
    {
        return stack_max;
    }

    /// <summary>
    /// 缓存：class_entity 对应的 Type 是否继承自 BuffEntityInstant
    /// 通过 Type 继承检查代替类名前缀匹配，避免改名后静默失效
    /// </summary>
    private bool? cachedIsInstant;

    /// <summary>
    /// 是否为Instant类型BUFF（SetData中立即触发并失效）
    /// 通过 class_entity 解析出 Type 后做继承检查并缓存结果
    /// </summary>
    public bool IsInstantBuffEntity()
    {
        if (cachedIsInstant.HasValue)
            return cachedIsInstant.Value;

        bool result = false;
        if (!class_entity.IsNull())
        {
            Type type = Type.GetType(class_entity);
            if (type != null)
            {
                result = typeof(BuffEntityInstant).IsAssignableFrom(type);
            }
        }
        cachedIsInstant = result;
        return result;
    }

    /// <summary>
    /// 获取BUFF稀有度
    /// </summary>
    public RarityEnum GetRarity()
    {
        return (RarityEnum)rarity;
    }

    /// <summary>
    /// 获取BUFF类型
    /// </summary>
    public BuffTypeEnum GetBuffType()
    {
        return (BuffTypeEnum)buff_type;
    }

    /// <summary>
    /// 获取触发BUFF生物类型
    /// </summary>
    /// <returns></returns>
    public CreatureFightTypeEnum GetTriggerCreatureType()
    {
        return (CreatureFightTypeEnum)trigger_creature_type;
    }

    /// <summary>
    /// 获取身体颜色
    /// </summary>
    /// <returns></returns>
    public Color GetBodyColor()
    {
        if (color_body.IsNull())
        {
            return Color.white;
        }
        else
        {
            if (colorBody == Color.white)
            {
                ColorUtility.TryParseHtmlString($"{color_body}", out Color targetColor);
                colorBody = targetColor;
            }
            return colorBody;
        }
    }

    //缓存：多属性BUFF(BuffEntityAttributeMulti) class_entity_data 解析出的 (属性,倍率) 列表
    private List<BuffAttributeMultiModifierStruct> cachedAttributeMultiPairs;

    /// <summary>
    /// 获取多属性BUFF的 (属性,倍率) 列表（解析 class_entity_data，如 "ATK:1|HP:-1"，结果缓存）
    /// </summary>
    public List<BuffAttributeMultiModifierStruct> GetAttributeMultiPairs()
    {
        if (cachedAttributeMultiPairs == null)
        {
            cachedAttributeMultiPairs = new List<BuffAttributeMultiModifierStruct>();
            BuffEntityAttributeMulti.ParsePairs(class_entity_data, cachedAttributeMultiPairs);
        }
        return cachedAttributeMultiPairs;
    }

    protected Dictionary<long, float> dicBuffPre;

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public Dictionary<long, float> GetPreInfo()
    {
        if (pre_info.IsNull())
        {
            return null;
        }
        if (dicBuffPre == null)
        {
            dicBuffPre = pre_info.SplitForDictionaryLongFloat();
        }
        return dicBuffPre;
    }
}

public partial class BuffInfoCfg
{
    public static Dictionary<BuffTypeEnum,List<BuffInfoBean>> dicBuffTypeData;
    // 缓存：以 (parentId, level) 为键快速查找 BUFF
    private static Dictionary<(long parentId, int level), BuffInfoBean> dicBuffByParentAndLevel;

    /// <summary>
    /// 获取指定父级BUFFID和等级的BUFF
    /// </summary>
    public static BuffInfoBean GetBuffByParentAndLevel(long parentId, int level)
    {
        // 初始化缓存
        if (dicBuffByParentAndLevel == null)
        {
            dicBuffByParentAndLevel = new Dictionary<(long parentId, int level), BuffInfoBean>();
            var allData = GetAllArrayData();
            for (int i = 0; i < allData.Length; i++)
            {
                var item = allData[i];
                if (item.buff_parent_id > 0 && item.buff_level > 0)
                {
                    var key = (item.buff_parent_id, item.buff_level);
                    if (!dicBuffByParentAndLevel.ContainsKey(key))
                    {
                        dicBuffByParentAndLevel.Add(key, item);
                    }
                }
            }
        }
        
        if (dicBuffByParentAndLevel.TryGetValue((parentId, level), out BuffInfoBean result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// 通过BUFF类型获取数据
    /// </summary>
    /// <param name="buffTypeEnum"></param>
    /// <returns></returns>
    public static List<BuffInfoBean> GetItemDataByBuffType(BuffTypeEnum buffTypeEnum)
    {
        if (dicBuffTypeData==null)
        {
            dicBuffTypeData = new Dictionary<BuffTypeEnum, List<BuffInfoBean>>();
            var allData = GetAllArrayData();
            for (int i = 0; i < allData.Length; i++)
            {
                var itemData = allData[i];
                var buffType = itemData.GetBuffType();
                if (dicBuffTypeData.ContainsKey(buffType))
                {
                    dicBuffTypeData[buffType].Add(itemData);
                }
                else
                {
                    dicBuffTypeData.Add(buffType, new List<BuffInfoBean>() { itemData });
                }
            }
        }
        if (dicBuffTypeData.TryGetValue(buffTypeEnum, out List<BuffInfoBean> valueData))
        {
            return valueData;
        }
        return null;
    }
}
