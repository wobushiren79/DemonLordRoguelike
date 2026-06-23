/// <summary>
/// 单体定向深渊馈赠 BUFF 标记接口
/// <para>普通深渊馈赠 BUFF 通过馈赠池(dicAbyssalBlessingBuffsActivie)作用于所有匹配生物；实现本接口的 BUFF 只作用于
/// <see cref="SingleTargetCreatureUUId"/> 指定的那一只随机防守生物。</para>
/// <para>属性管线(FightCreatureBean.CollectFromBuffList)与攻速管线(BuffHandler.ChangeAttackTimeDataForBuff)会据此把加成
/// 限定到单只生物，从而实现「随机一只防守生物属性翻倍 / 攻速翻倍」类馈赠；因只改运行时计算结果、不改持久 CreatureBean，故不污染存档。</para>
/// </summary>
public interface ISingleTargetAbyssalBuff
{
    /// <summary>
    /// 选取馈赠时被随机锁定的单只防守生物 UUID（为空表示未锁定任何生物，加成不生效）
    /// </summary>
    string SingleTargetCreatureUUId { get; }
}
