public class BuffEntityConditionalDead : BuffEntityConditional
{

    /// <summary>
    /// 时间-生物死亡开始
    /// </summary>
    public override void EventForCreatureDeadStart(FightCreatureEntity eventFightCreatureEntity)
    {
        base.EventForCreatureDeadStart(eventFightCreatureEntity);
        if(!CheckEvent(eventFightCreatureEntity))
        {
            return;
        }
    }

    /// <summary>
    /// 时间-生物死亡结束
    /// </summary>
    public override void EventForCreatureDeadEnd(FightCreatureEntity eventFightCreatureEntity)
    {         
        base.EventForCreatureDeadEnd(eventFightCreatureEntity);
        if(!CheckEvent(eventFightCreatureEntity))
        {
            return;
        }
        buffEntityData.isValid = false;
        TriggerBuffConditional(buffEntityData);
    }

    /// <summary>
    /// 检测事件是否通过-通过再执行BUFF
    /// </summary>
    public virtual bool CheckEvent(FightCreatureEntity eventFightCreatureEntity)
    {
        if(buffEntityData.isValid == false)
        {
            return false;
        }
        var targetFightCreatureEntity = GetFightCreatureEntityForTarget();
        if (targetFightCreatureEntity == null)
        {
            buffEntityData.isValid = false;
            return false;
        }       
        if (eventFightCreatureEntity == targetFightCreatureEntity)
        {
            return true;
        }
        return false;
    }
}