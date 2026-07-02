using System.Collections.Generic;

/// <summary>
/// 多属性BUFF：同时改变多个属性。
/// <para>class_entity_data 格式 "属性:倍率|属性:倍率"（倍率相对 trigger_value_rate，如 "ATK:1|HP:-1" = ATK 按 +率、HP 按等量负率）。</para>
/// <para>所有属性共享同一次随机ロール(trigger_value_rate)，实现"一个属性加成、对应属性等比减益"（如"ATK+30% 则 HP-30%"）。</para>
/// <para>与单属性 BuffEntityAttribute 同属"纯属性BUFF"：既可走属性烘焙(CreatureBean.GetBuffChangeAttribute)预览路径，也可走运行时 ModifierPipeline。</para>
/// </summary>
public class BuffEntityAttributeMulti : BuffEntityAttribute
{
    #region 数据解析
    /// <summary>
    /// 多属性BUFF不使用单一 attributeType：解析由 buffInfo.GetAttributeMultiPairs() 按需缓存承担，
    /// 此处重写为空以跳过基类"把 class_entity_data 当单枚举 Parse"的逻辑（否则 "ATK:1|HP:-1" 会抛异常）
    /// </summary>
    protected override void ParseAttributeData(BuffInfoBean buffInfo)
    {
    }
    #endregion

    #region 运行时路径（ModifierPipeline）
    /// <summary>
    /// 把多属性加成追加到 sink：逐个属性按各自倍率缩放 trigger_value / trigger_value_rate 后 emit
    /// </summary>
    public override void CollectModifiers(List<AttributeModifier> sink)
    {
        if (buffEntityData == null || !buffEntityData.isValid) return;
        var buffInfo = buffEntityData.GetBuffInfo();
        var listPair = buffInfo.GetAttributeMultiPairs();
        var buffData = buffEntityData.buffData;
        int stackCount = buffEntityData.stackCount < 1 ? 1 : buffEntityData.stackCount;
        for (int i = 0; i < listPair.Count; i++)
        {
            var pair = listPair[i];
            float val = buffData.trigger_value * pair.rateMultiplier * stackCount;
            float rate = buffData.trigger_value_rate * pair.rateMultiplier * stackCount;
            EmitModifiers(sink, pair.attributeType, val, rate, this);
        }
    }
    #endregion

    #region 预览路径（属性烘焙 / 卡片详情）
    /// <summary>
    /// 兼容层：单BUFF应用到 targetData（运行时实例，供 GetAbyssalBlessingChangeAttribute 等预览路径）
    /// </summary>
    public override float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (buffEntityData == null) return targetData;
        var buffInfo = buffEntityData.GetBuffInfo();
        int stackCount = buffEntityData.stackCount < 1 ? 1 : buffEntityData.stackCount;
        return ApplyChangeData(buffInfo.GetAttributeMultiPairs(), buffEntityData.buffData, targetAttributeType, targetData, stackCount);
    }

    /// <summary>
    /// 配置层预览：无运行时实例时直接按配置(BuffBean + BuffInfo)应用多属性变化
    /// （供 CreatureBean.GetBuffChangeAttribute 烘焙自身/稀有度多属性BUFF）
    /// </summary>
    public static float ChangeDataForConfig(BuffBean buffData, BuffInfoBean buffInfo, CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        return ApplyChangeData(buffInfo.GetAttributeMultiPairs(), buffData, targetAttributeType, targetData, 1);
    }

    /// <summary>
    /// 把列表中匹配 targetAttributeType 的每一对按其倍率缩放后应用到 targetData
    /// </summary>
    private static float ApplyChangeData(List<BuffAttributeMultiModifierStruct> listPair, BuffBean buffData, CreatureAttributeTypeEnum targetAttributeType, float targetData, int stackCount)
    {
        if (stackCount < 1) stackCount = 1;
        for (int i = 0; i < listPair.Count; i++)
        {
            var pair = listPair[i];
            if (pair.attributeType != targetAttributeType) continue;
            float val = buffData.trigger_value * pair.rateMultiplier * stackCount;
            float rate = buffData.trigger_value_rate * pair.rateMultiplier * stackCount;
            targetData = ChangeData(targetAttributeType, targetData, val, rate);
        }
        return targetData;
    }
    #endregion

    #region 解析
    /// <summary>
    /// 解析 "属性:倍率|属性:倍率" 为 (属性,倍率) 列表；无 ":" 时倍率默认 1，非法属性跳过
    /// </summary>
    public static void ParsePairs(string classEntityData, List<BuffAttributeMultiModifierStruct> result)
    {
        result.Clear();
        if (classEntityData.IsNull()) return;
        string[] arrayPart = classEntityData.Split('|');
        for (int i = 0; i < arrayPart.Length; i++)
        {
            string part = arrayPart[i];
            if (part.IsNull()) continue;
            string[] arrayKV = part.Split(':');
            CreatureAttributeTypeEnum attributeType = arrayKV[0].Trim().GetEnum<CreatureAttributeTypeEnum>();
            if (attributeType == CreatureAttributeTypeEnum.None) continue;
            float rateMultiplier = 1f;
            if (arrayKV.Length > 1 && float.TryParse(arrayKV[1].Trim(), out float parseMult))
                rateMultiplier = parseMult;
            result.Add(new BuffAttributeMultiModifierStruct { attributeType = attributeType, rateMultiplier = rateMultiplier });
        }
    }
    #endregion
}

/// <summary>
/// 多属性BUFF的单个 (属性, 倍率) 条目
/// </summary>
public struct BuffAttributeMultiModifierStruct
{
    /// <summary>目标属性</summary>
    public CreatureAttributeTypeEnum attributeType;
    /// <summary>相对 trigger_value_rate 的倍率（+1 加成 / -1 等量减益）</summary>
    public float rateMultiplier;
}
