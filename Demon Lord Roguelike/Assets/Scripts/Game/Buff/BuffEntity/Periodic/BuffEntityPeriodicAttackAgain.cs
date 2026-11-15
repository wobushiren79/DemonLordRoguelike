public class BuffEntityPeriodicAttackAgain : BuffEntityPeriodic
{
    #region 追加攻击
    /// <summary>等待已登记的周期追加攻击实际发射，再开始下一个周期。</summary>
    public override void UpdateBuffTime(float buffTime)
    {
        var target = GetFightCreatureEntityForTarget();
        if (buffTime > 0 && target != null && !target.IsDead() && !target.HasPendingPeriodicBuffAttack)
            base.UpdateBuffTime(buffTime);
    }

    /// <summary>达成一个周期后登记一发追加攻击，待索敌成功后发射。</summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        return base.TriggerBuffPeriodic(buffEntityData)
            && GetFightCreatureEntityForTarget()?.QueueBuffAttack(isPeriodic: true) == true;
    }
    #endregion
}
