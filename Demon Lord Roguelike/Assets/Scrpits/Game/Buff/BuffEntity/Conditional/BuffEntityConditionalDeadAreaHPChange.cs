public class BuffEntityConditionalDeadAreaHPChange : BuffEntityConditionalDead
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
        BuffEntityBaseHPChangeArea.HPChangeArea(buffEntityData, targetFightCreatureEntity);
        return true;
    }
}