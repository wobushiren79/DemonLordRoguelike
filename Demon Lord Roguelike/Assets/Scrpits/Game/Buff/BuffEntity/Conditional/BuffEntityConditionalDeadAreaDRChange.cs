public class BuffEntityConditionalDeadAreaDRChange : BuffEntityConditionalDead
{
    public override void CreatureDeadEnd()
    {
        base.CreatureDeadEnd();
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
        if (targetFightCreatureEntity == null)
            return false;
        BuffEntityBaseDRChangeArea.DRChangeArea(buffEntityData, targetFightCreatureEntity);
        return true;
    }
}