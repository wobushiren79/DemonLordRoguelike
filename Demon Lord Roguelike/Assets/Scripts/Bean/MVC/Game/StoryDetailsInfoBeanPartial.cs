using System;
using System.Collections.Generic;
/// <summary>
/// 故事演出步骤配置扩展
/// <para>param_1~4 语义表（与 excel_story_details_info 表头、story-system skill 文档三方同步维护）：</para>
/// <para>Talk(1):      param_1=对话ID(&amp;分隔=同一步内顺序连播多句)</para>
/// <para>CameraMove(2):param_1=目标标记(base:self/core/portal/gashapon/juicer/altar/vat/achievement/council;fight:core;通用:back=回演出起始位) param_2=时长秒(默认1) param_3=缓动DOTween序号(默认0)</para>
/// <para>Wait(3):      param_1=秒(实时,不受timeScale影响)</para>
/// <para>Effect(4):    param_1=特效ID param_2=目标标记(空=战斗防守核心/基地魔王位) param_3=尺寸倍率(默认1)</para>
/// <para>Audio(5):     param_1=音效ID</para>
/// <para>Fade(6):      param_1=out淡出变黑/in淡入 param_2=时长秒(默认0.5)</para>
/// </summary>
public partial class StoryDetailsInfoBean
{
    /// <summary>
    /// 获取步骤类型
    /// </summary>
    public StoryStepTypeEnum GetStepType()
    {
        return (StoryStepTypeEnum)step_type;
    }

    /// <summary>
    /// 是否并发执行（true=发起后立即进行下一步，不等待本步完成）
    /// </summary>
    public bool IsAsync()
    {
        return is_async == 1;
    }

    /// <summary>
    /// 按序号取原始参数（1~4 对应 param_1~param_4；空串统一返回 null 便于 IsNull 判断）
    /// </summary>
    public string GetParam(int index)
    {
        string value;
        switch (index)
        {
            case 1: value = param_1; break;
            case 2: value = param_2; break;
            case 3: value = param_3; break;
            case 4: value = param_4; break;
            default: return null;
        }
        return value.IsNull() ? null : value;
    }

    /// <summary>
    /// 取 float 参数（空或解析失败返回默认值）
    /// </summary>
    public float GetParamFloat(int index, float defaultValue)
    {
        string value = GetParam(index);
        if (value.IsNull())
            return defaultValue;
        if (float.TryParse(value, out float result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 取 int 参数（空或解析失败返回默认值）
    /// </summary>
    public int GetParamInt(int index, int defaultValue)
    {
        string value = GetParam(index);
        if (value.IsNull())
            return defaultValue;
        if (int.TryParse(value, out int result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 取 long 参数（空或解析失败返回默认值）
    /// </summary>
    public long GetParamLong(int index, long defaultValue)
    {
        string value = GetParam(index);
        if (value.IsNull())
            return defaultValue;
        if (long.TryParse(value, out long result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 解析对话步骤的对话ID列表（param_1 按 &amp; 分隔，连播多句）
    /// </summary>
    public long[] GetTalkIds()
    {
        if (param_1.IsNull())
            return new long[0];
        return param_1.SplitForArrayLong('&');
    }
}
public partial class StoryDetailsInfoCfg
{
    /// <summary>
    /// 获取指定故事的所有演出步骤（按 step_order 升序）
    /// </summary>
    public static List<StoryDetailsInfoBean> GetDataByStoryId(long storyId)
    {
        List<StoryDetailsInfoBean> list = new List<StoryDetailsInfoBean>();
        var arrayData = GetAllArrayData();
        for (int i = 0; i < arrayData.Length; i++)
        {
            StoryDetailsInfoBean itemData = arrayData[i];
            if (itemData.story_id == storyId)
            {
                list.Add(itemData);
            }
        }
        list.Sort((a, b) => a.step_order.CompareTo(b.step_order));
        return list;
    }
}
