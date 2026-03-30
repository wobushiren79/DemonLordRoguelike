public class BuffEntityConditionalDeadAreaDRChange : BuffEntityConditionalDead
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        var targetFightCreatureEntity = GetFightCreatureEntityForTarget();
        if (targetFightCreatureEntity == null)
            return false;
        BuffEntityBaseDRChangeArea.DRChangeArea(buffEntityData, targetFightCreatureEntity);
        return true;
    }
}