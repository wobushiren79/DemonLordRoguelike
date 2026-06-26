using System.Collections.Generic;

/// <summary>
/// 单体定向属性 BUFF：选取时随机锁定一只防守生物，仅对该生物按 class_entity_data 指定的属性施加加成。
/// <para>用于「大力出奇迹 / 膘肥体壮 / 钢铁憨憨」等深渊馈赠——随机一只防守魔物 攻击力/生命/护甲 翻倍(trigger_value_rate=1 即 +100%)。</para>
/// <para>实现 <see cref="IBuffSingleTarget"/>，由 FightCreatureBean.CollectFromBuffList 把 modifier 限定到锁定的那只生物；
/// 因只改运行时计算的 dicAttribute、不改持久 CreatureBean，故不污染存档。可重复选取(level=0)，每次锁定一只新随机生物叠加。</para>
/// </summary>
public class BuffEntityAttributeSingleTarget : BuffEntityAttribute, IBuffSingleTarget
{
    private string singleTargetCreatureUUId;

    /// <summary>
    /// 选取馈赠时被随机锁定的单只防守生物 UUID
    /// </summary>
    public string SingleTargetCreatureUUId => singleTargetCreatureUUId;

    /// <summary>
    /// 设置数据：先按 class_entity_data 解析属性类型(父类逻辑)，再随机锁定一只防守生物
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        singleTargetCreatureUUId = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()?.fightData?.GetRandomDefenseCreatureUUId();
    }

    /// <summary>
    /// 清理数据：归还对象池前重置随机目标，避免复用串味
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        singleTargetCreatureUUId = null;
    }

    /// <summary>
    /// 收集属性 modifier：仅在已锁定到生物时生效(具体「只对锁定生物」的过滤在 CollectFromBuffList 完成)
    /// </summary>
    public override void CollectModifiers(List<AttributeModifier> sink)
    {
        if (singleTargetCreatureUUId.IsNull()) return;
        base.CollectModifiers(sink);
    }
}
