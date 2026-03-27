using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffInfoBean
{
    protected Color colorBody = Color.white;

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
