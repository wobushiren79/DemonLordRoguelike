/// <summary>
/// 单体定向 BUFF 标记接口：实现本接口的 BUFF 只作用于 <see cref="SingleTargetCreatureUUId"/> 指定的那一只生物，而非所有匹配生物。
/// <para>属性管线(FightCreatureBean.CollectFromBuffList)与攻速管线(BuffHandler.ChangeAttackTimeDataForBuff)会据此把加成
/// 限定到单只生物，从而实现「随机一只生物属性翻倍 / 攻速翻倍」类效果；因只改运行时计算结果、不改持久 CreatureBean，故不污染存档。</para>
/// <para>当前用于深渊馈赠的单体定向馈赠(急性子/大力出奇迹等)，但不限于此——任何需要「只命中某一只生物」的 BUFF 均可实现本接口。</para>
/// <para>注意：单体定向加成**不随复制魔物(增殖)继承**——克隆体是新 UUID，锁定 UUID 不匹配即不生效；克隆体只继承「作用于全体生物」的加成(靠 trigger_creature_type 过滤，与 UUID 无关)。</para>
/// </summary>
public interface IBuffSingleTarget
{
    /// <summary>
    /// 被锁定的单只目标生物 UUID（为空表示未锁定任何生物，加成不生效）
    /// </summary>
    string SingleTargetCreatureUUId { get; }
}
