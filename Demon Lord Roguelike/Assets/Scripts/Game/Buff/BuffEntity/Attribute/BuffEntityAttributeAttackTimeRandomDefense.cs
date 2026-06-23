/// <summary>
/// 单体定向攻速深渊馈赠 BUFF：选取时随机锁定一只防守生物，仅对该生物按 trigger_value_rate 缩放攻击时间。
/// <para>用于「急性子」馈赠——随机一只防守魔物 攻击间隔减半(trigger_value_rate=0.5) 即攻击翻倍快。</para>
/// <para>攻速不走属性管线，由 BuffHandler.ChangeAttackTimeDataForBuff 扫描深渊馈赠池，并据 <see cref="ISingleTargetAbyssalBuff"/>
/// 限定到锁定的那只生物；因只在取攻击时间时临时缩放、不改持久 CreatureBean，故不污染存档。可重复选取(level=0)叠加。</para>
/// </summary>
public class BuffEntityAttributeAttackTimeRandomDefense : BuffEntityAttributeAttackTime, ISingleTargetAbyssalBuff
{
    private string singleTargetCreatureUUId;

    /// <summary>
    /// 选取馈赠时被随机锁定的单只防守生物 UUID
    /// </summary>
    public string SingleTargetCreatureUUId => singleTargetCreatureUUId;

    /// <summary>
    /// 设置数据：随机锁定一只防守生物
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        singleTargetCreatureUUId = AbyssalBlessingSingleTargetUtil.PickRandomDefenseCreatureUUId();
    }

    /// <summary>
    /// 清理数据：归还对象池前重置随机目标，避免复用串味
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        singleTargetCreatureUUId = null;
    }
}
