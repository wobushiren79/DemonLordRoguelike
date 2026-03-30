public class BuffEntityConditionalCreateCrystal : BuffBaseEntity
{
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
    /// 处理检测
    /// </summary>
    public override void HandleForEvent()
    {
        base.HandleForEvent();
        if (CheckIsPre(buffEntityData))
        {
            buffEntityData.conditionalValue = 0;
            //触发BUFF
            TriggerBuffConditional(buffEntityData);
        }
    }
}