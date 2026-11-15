public class BuffEntityConditionalCreateCrystal : BuffEntityConditional
{
    #region 条件触发
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        var fightCreatureEntity = GetFightCreatureEntityForTarget();
        if (fightCreatureEntity == null)
            return false;
        //掉落魔晶
        fightCreatureEntity.DropCrystal(0);
        return true;
    }

    /// <summary>
    /// 达标时掉落魔晶，并保留超过门槛的进度供下一次事件使用。
    /// </summary>
    public override void HandleForEvent()
    {
        if (TryConsumeTriggerCondition())
            TriggerBuffConditional(buffEntityData);
    }
    #endregion
}
