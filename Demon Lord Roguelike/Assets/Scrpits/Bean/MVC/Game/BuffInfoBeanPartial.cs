using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffInfoBean
{
    protected Color colorBody = Color.white;

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
}
