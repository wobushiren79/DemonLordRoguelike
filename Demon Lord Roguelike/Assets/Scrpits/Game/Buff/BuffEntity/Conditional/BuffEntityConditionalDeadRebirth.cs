public class BuffEntityConditionalDeadRebirth : BuffEntityConditionalDead
{
    public override void EventForCreatureDeadEnd(FightCreatureBean fightCreatureBean)
    {
        base.EventForCreatureDeadEnd(fightCreatureBean);
        TriggerBuffConditional(buffEntityData);
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        return true;
    }
}