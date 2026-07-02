using System.Collections.Generic;

/// <summary>
/// 动态百分比属性 BUFF 基类：加成率不再取配置写死的 trigger_value_rate，而是每次重算属性时按运行时状态实时计算。
/// <para>与普通 <see cref="BuffEntityAttribute"/> 的区别：普通类 rate 恒定；本类每次 <see cref="CollectModifiers"/> / <see cref="ChangeData(CreatureAttributeTypeEnum,float)"/>
/// 都调用 <see cref="GetDynamicRate"/> 返回当前应加成的百分比(如"随场上魔物数量/累计击杀数增长")。</para>
/// <para>只走 PercentAdd 通道(仅用于 ATK/DR/HP 这类百分比加成)；trigger_value(Flat)不参与。作用范围由配置 trigger_creature_type 决定。</para>
/// <para>抽象基类，不在配置 class_entity 中直接引用；具体效果由子类(都是兄弟/杀红了眼)重写 <see cref="GetDynamicRate"/> 实现。</para>
/// </summary>
public abstract class BuffEntityAttributeDynamicRate : BuffEntityAttribute
{
    #region 动态加成率
    /// <summary>
    /// 计算当前应施加的百分比加成率(如 0.1 表示 +10%)；由子类按运行时状态实现。返回 &lt;=0 表示本次不加成。
    /// </summary>
    protected abstract float GetDynamicRate();

    /// <summary>
    /// 获取当前战斗数据(取不到返回 null，子类算率时据此读取场上魔物/击杀数等)
    /// </summary>
    protected FightBean GetFightData()
    {
        return GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()?.fightData;
    }
    #endregion

    #region 属性收集(战斗热点)
    /// <summary>
    /// 收集属性 modifier：用动态率替代配置固定率，按 PercentAdd 通道加到 sink
    /// </summary>
    public override void CollectModifiers(List<AttributeModifier> sink)
    {
        if (buffEntityData == null || !buffEntityData.isValid) return;
        if (attributeType == CreatureAttributeTypeEnum.None) return;
        float rate = GetDynamicRate();
        if (rate <= 0f) return;
        sink.Add(new AttributeModifier { attributeType = attributeType, channel = ModifierChannel.PercentAdd, value = rate, source = this });
    }
    #endregion

    #region 兼容层(卡片详情预览路径)
    /// <summary>
    /// 兼容层：单BUFF应用到 targetData，用动态率做百分比加成(供 CreatureBean.GetAbyssalBlessingChangeAttribute 详情预览调用)
    /// </summary>
    public override float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (targetAttributeType != attributeType) return targetData;
        float rate = GetDynamicRate();
        if (rate <= 0f) return targetData;
        targetData *= 1f + rate;
        if (targetData < 0) targetData = 0;
        return targetData;
    }
    #endregion
}
