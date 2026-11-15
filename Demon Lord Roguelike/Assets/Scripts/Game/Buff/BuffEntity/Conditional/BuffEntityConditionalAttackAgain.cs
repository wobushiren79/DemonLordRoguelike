public class BuffEntityConditionalAttackAgain : BuffEntityConditional
{
    #region 追加攻击
    /// <summary>
    /// 登记本次追加攻击，普通索敌取得合法目标后发射。
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        return base.TriggerBuffConditional(buffEntityData)
            && GetFightCreatureEntityForTarget()?.QueueBuffAttack() == true;
    }

    /// <summary>
    /// 剩余累计保留，达标时为该生物追加一发。
    /// </summary>
    public override void HandleForEvent()
    {
        if (TryConsumeTriggerCondition())
            TriggerBuffConditional(buffEntityData);
    }

    /// <summary>只有普通来源伤害计入追加攻击的触发门槛。</summary>
    public override void EventForUnderAttack(FightUnderAttackBean fightUnderAttack)
    {
        if (!fightUnderAttack.isBuffAttackAgain)
            base.EventForUnderAttack(fightUnderAttack);
    }

    /// <summary>追加攻击导致的击杀不会递归触发追加攻击。</summary>
    public override void EventForUnderAttackDead(FightUnderAttackBean fightUnderAttack)
    {
        if (!fightUnderAttack.isBuffAttackAgain)
            base.EventForUnderAttackDead(fightUnderAttack);
    }
    #endregion
}
