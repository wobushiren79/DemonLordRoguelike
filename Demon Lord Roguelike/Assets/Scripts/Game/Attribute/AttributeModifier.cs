using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性修改器通道
/// 按 Flat → PercentAdd → PercentMul → Override 顺序应用，叠序无关
/// </summary>
public enum ModifierChannel
{
    /// <summary>
    /// 平A：直接相加（atk += 100）
    /// </summary>
    Flat = 0,
    /// <summary>
    /// 百分比加：同通道 rate 累加后乘一次（+15% 与 +20% 互叠 = +35%）
    /// </summary>
    PercentAdd = 1,
    /// <summary>
    /// 百分比独立乘：每个 (1+rate) 连乘（多个 ×1.5 互不干扰）
    /// </summary>
    PercentMul = 2,
    /// <summary>
    /// 强制覆盖：取最高 priority 的值（HP 锁血、攻速锁定等）
    /// </summary>
    Override = 3,
}

/// <summary>
/// 单条属性修改器
/// </summary>
public struct AttributeModifier
{
    public CreatureAttributeTypeEnum attributeType;
    public ModifierChannel channel;
    public float value;
    /// <summary>
    /// Override 通道用 priority 决定取哪个值（同 priority 时取最后一个）
    /// </summary>
    public int priority;
    /// <summary>
    /// 修改器来源（BUFF/装备/天赋实例等），未来用于"移除某来源全部加成"
    /// </summary>
    public object source;
}

/// <summary>
/// 属性修改器来源
/// BUFF（或将来的装备/天赋）实现此接口即可参与属性管线
/// </summary>
public interface IAttributeModifierSource
{
    /// <summary>
    /// 把当前生效的 modifier 追加到 sink
    /// 条件未满足应直接返回（不追加），避免引入"中性 modifier"
    /// </summary>
    void CollectModifiers(List<AttributeModifier> sink);
}

/// <summary>
/// 属性修改器管线
/// 给定 baseValue + 一组 modifier，按通道顺序计算最终值，叠序无关
/// </summary>
public static class ModifierPipeline
{
    /// <summary>
    /// 把 modifiers 中属于 attributeType 的条目应用到 baseValue
    /// </summary>
    public static float Apply(float baseValue, CreatureAttributeTypeEnum attributeType, List<AttributeModifier> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return baseValue < 0 ? 0 : baseValue;
        }

        float flatSum = 0;
        float pctAddSum = 0;
        float pctMulProduct = 1f;
        bool hasOverride = false;
        float overrideValue = 0;
        int overridePri = int.MinValue;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var m = modifiers[i];
            if (m.attributeType != attributeType) continue;
            switch (m.channel)
            {
                case ModifierChannel.Flat: flatSum += m.value; break;
                case ModifierChannel.PercentAdd: pctAddSum += m.value; break;
                case ModifierChannel.PercentMul: pctMulProduct *= 1f + m.value; break;
                case ModifierChannel.Override:
                    if (m.priority >= overridePri)
                    {
                        hasOverride = true;
                        overrideValue = m.value;
                        overridePri = m.priority;
                    }
                    break;
            }
        }

        float v = (baseValue + flatSum) * (1f + pctAddSum) * pctMulProduct;
        if (hasOverride) v = overrideValue;
        if (v < 0) v = 0;
        return v;
    }
}
