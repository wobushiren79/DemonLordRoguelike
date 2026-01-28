using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffInfoBean
{
    protected Color colorBody = Color.white;

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
