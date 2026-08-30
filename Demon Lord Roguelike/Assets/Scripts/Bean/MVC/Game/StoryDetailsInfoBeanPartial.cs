using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 故事演出步骤配置扩展
/// <para>param_1~4 语义表（与 excel_story_details_info 表头、story-system skill 文档三方同步维护）：</para>
/// <para>Talk(1):      param_1=对话ID(&amp;分隔=同一步内顺序连播多句) param_2=对话框对齐(bottom/bottom_left/bottom_right/middle/middle_left/middle_right/top/top_left/top_right,空=bottom下对齐;可接 |高亮目标|形状rect/circle默认rect|尺寸倍率默认1,如 top|crystal|circle|1.2) param_3=偏移X(默认0) param_4=偏移Y(默认0)</para>
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

    /// <summary>对话步骤可选的对话框对齐方式（param_2 对齐段合法值，空串=bottom 下对齐；编辑器下拉与保存校验共用此表）</summary>
    public static readonly string[] TalkContentAligns = { "bottom", "bottom_left", "bottom_right", "middle", "middle_left", "middle_right", "top", "top_left", "top_right" };

    /// <summary>对话步骤可选的高亮目标标记（param_2 高亮段合法值，空=不高亮；编辑器下拉与保存校验共用此表。demon=战斗魔王核心/crystal=掉落魔晶，其余为 UIFightMain 控件）</summary>
    public static readonly string[] TalkHighlightMarkers = { "demon", "crystal", "ui_fight_card", "ui_fight_remove", "ui_fight_att_progress" };

    /// <summary>对话步骤可选的高亮形状（param_2 形状段合法值，空=rect 方形；编辑器下拉与保存校验共用此表，值对应 Shader_UI_GuideHighlight 的 _ShapeType）</summary>
    public static readonly string[] TalkHighlightShapes = { "rect", "circle" };

    /// <summary>
    /// 取对话步骤 param_2 的指定段（| 分隔：0=对齐 1=高亮目标 2=形状 3=尺寸倍率；段不存在或为空返回 null）
    /// </summary>
    private string GetTalkParam2Segment(int segmentIndex)
    {
        string value = GetParam(2);
        if (value.IsNull())
            return null;
        var parts = value.Split('|');
        if (segmentIndex >= parts.Length)
            return null;
        string segment = parts[segmentIndex];
        return segment.IsNull() ? null : segment;
    }

    /// <summary>
    /// 解析对话步骤的对话框对齐锚点（param_2 第 0 段；空=默认下对齐 (0.5,0)，非法值兜底下对齐）
    /// </summary>
    public Vector2 GetTalkContentAnchor()
    {
        //默认下对齐(底部居中)
        float anchorX = 0.5f, anchorY = 0f;
        string align = GetTalkParam2Segment(0);
        if (!align.IsNull())
        {
            align = align.ToLowerInvariant();
            if (align.StartsWith("top"))
                anchorY = 1f;
            else if (align.StartsWith("middle"))
                anchorY = 0.5f;
            if (align.EndsWith("_left"))
                anchorX = 0f;
            else if (align.EndsWith("_right"))
                anchorX = 1f;
        }
        return new Vector2(anchorX, anchorY);
    }

    /// <summary>
    /// 解析对话步骤的高亮目标标记（param_2 第 1 段；未配置返回 null=不高亮）
    /// </summary>
    public string GetTalkHighlightMarker()
    {
        return GetTalkParam2Segment(1);
    }

    /// <summary>
    /// 解析对话步骤的高亮形状（param_2 第 2 段；0=rect 方形(默认) 1=circle 圆形，非法值兜底方形）
    /// </summary>
    public int GetTalkHighlightShape()
    {
        string shape = GetTalkParam2Segment(2);
        if (!shape.IsNull() && string.Equals(shape, "circle", StringComparison.OrdinalIgnoreCase))
            return 1;
        return 0;
    }

    /// <summary>
    /// 解析对话步骤的高亮尺寸倍率（param_2 第 3 段；空或非法=默认 1，下限 0.01 防退化不可见）
    /// </summary>
    public float GetTalkHighlightScale()
    {
        string scale = GetTalkParam2Segment(3);
        if (scale.IsNull() || !float.TryParse(scale, out float value))
            return 1f;
        return Mathf.Max(value, 0.01f);
    }

    /// <summary>
    /// 解析对话步骤的对话框偏移坐标（param_3=X/param_4=Y，空或非法=默认 0）
    /// </summary>
    public Vector2 GetTalkContentOffset()
    {
        return new Vector2(GetParamFloat(3, 0f), GetParamFloat(4, 0f));
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
