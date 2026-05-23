using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性BUFF：把 trigger_value / trigger_value_rate 转化为 ModifierPipeline 可消费的 modifier
/// </summary>
public class BuffEntityAttribute : BuffBaseEntity, IAttributeModifierSource
{
    public CreatureAttributeTypeEnum attributeType = CreatureAttributeTypeEnum.None;

    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        string classEntityData = buffInfo.class_entity_data;
        if (classEntityData.IsNull())
        {
            LogUtil.LogError($"初始化BuffEntityAttribute失败，class_entity_data为空 buffID:{buffEntityData.buffId}");
        }
        else
        {
            attributeType = classEntityData.GetEnum<CreatureAttributeTypeEnum>();
        }
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        attributeType = CreatureAttributeTypeEnum.None;
    }

    /// <summary>
    /// 把本BUFF的属性加成追加到 sink
    /// 默认条件：runtime有效；子类可重写以加入条件门控
    /// </summary>
    public virtual void CollectModifiers(List<AttributeModifier> sink)
    {
        if (buffEntityData == null || !buffEntityData.isValid) return;
        EmitModifiers(sink, buffEntityData.buffData, attributeType, buffEntityData.stackCount, this);
    }

    /// <summary>
    /// 把 BuffBean 转换为 modifier 列表的通用辅助
    /// 用于运行时实例（带 stackCount）以及预览场景（CreatureBean.GetBuffChangeAttribute 等，stackCount=1）
    /// CRT/EVA：rate 直接 Flat 累加（其值本身就是百分比，不再走百分比乘）
    /// 其他属性：value 走 Flat，rate 走 PercentAdd
    /// </summary>
    public static void EmitModifiers(List<AttributeModifier> sink, BuffBean buffData, CreatureAttributeTypeEnum attributeType, int stackCount, object source)
    {
        if (stackCount < 1) stackCount = 1;
        float val = buffData.trigger_value * stackCount;
        float rate = buffData.trigger_value_rate * stackCount;

        switch (attributeType)
        {
            case CreatureAttributeTypeEnum.CRT:
            case CreatureAttributeTypeEnum.EVA:
                //CRT/EVA：rate 本身是百分比绝对值（如 +15%），直接累加
                if (rate != 0f)
                    sink.Add(new AttributeModifier { attributeType = attributeType, channel = ModifierChannel.Flat, value = rate, source = source });
                break;
            default:
                if (val != 0f)
                    sink.Add(new AttributeModifier { attributeType = attributeType, channel = ModifierChannel.Flat, value = val, source = source });
                if (rate != 0f)
                    sink.Add(new AttributeModifier { attributeType = attributeType, channel = ModifierChannel.PercentAdd, value = rate, source = source });
                break;
        }
    }

    #region 兼容层：单BUFF版 ChangeData（仅供 CreatureBean.cs 的预览路径调用）
    //CreatureBean.cs 是 hook 保护的 partial 文件，不能直接编辑；
    //保留这两个方法以维持其调用方可编译。
    //注意：这些路径仍是叠序敏感的（每次只应用一个BUFF），
    //战斗热点 FightCreatureBean.RefreshBaseAttribute 已经切到 ModifierPipeline，无此问题。
    /// <summary>
    /// 兼容层：单BUFF应用到 targetData
    /// </summary>
    public virtual float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (targetAttributeType != attributeType) return targetData;
        int stack = buffEntityData != null ? buffEntityData.stackCount : 1;
        return ChangeData(buffEntityData.buffData, targetAttributeType, targetData, stack);
    }

    /// <summary>
    /// 兼容层：单BUFF应用到 targetData（静态，用于无运行时实例的预览路径）
    /// </summary>
    public static float ChangeData(BuffBean buffData, CreatureAttributeTypeEnum targetAttributeType, float targetData, int stackCount = 1)
    {
        if (stackCount < 1) stackCount = 1;
        switch (targetAttributeType)
        {
            case CreatureAttributeTypeEnum.CRT:
            case CreatureAttributeTypeEnum.EVA:
                targetData += buffData.trigger_value_rate * stackCount;
                break;
            default:
                targetData += buffData.trigger_value * stackCount;
                if (targetData < 0) targetData = 0;
                targetData *= 1f + buffData.trigger_value_rate * stackCount;
                break;
        }
        if (targetData < 0) targetData = 0;
        return targetData;
    }
    #endregion
}
