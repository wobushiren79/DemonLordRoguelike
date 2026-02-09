public class BuffEntityConditionalDead : BuffEntityConditional
{
    protected FightCreatureEntity targetFightCreatureEntity = null;
    public string nameRegisterEvent;
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        if (!buffInfo.class_entity_events.IsNull())
        {
            nameRegisterEvent = buffInfo.class_entity_events;
            //监听生物死亡
            if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadStart))
            {
                EventHandler.Instance.RegisterEvent<FightCreatureEntity>(nameRegisterEvent, EventForCreatureDeadStart);
            }
            else if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadEnd))
            {
                EventHandler.Instance.RegisterEvent<FightCreatureEntity>(nameRegisterEvent, EventForCreatureDeadEnd);
            }
        }
        targetFightCreatureEntity = GetFightCreatureEntityForTarget();
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        //清理数据的时候需要清理一下注册的信息
        if (nameRegisterEvent != null)
        {
            if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadStart))
            {
                EventHandler.Instance.UnRegisterEvent<FightCreatureEntity>(nameRegisterEvent, EventForCreatureDeadStart);
            }
            else if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadEnd))
            {
                EventHandler.Instance.UnRegisterEvent<FightCreatureEntity>(nameRegisterEvent, EventForCreatureDeadEnd);
            }
        }
        nameRegisterEvent = null;
    }

    /// <summary>
    /// 时间-生物死亡开始
    /// </summary>
    public virtual void EventForCreatureDeadStart(FightCreatureEntity eventFightCreatureEntity)
    {
        if(!CheckEvent(eventFightCreatureEntity))
        {
            return;
        }
        CreatureDeadStart();
    }

    public virtual void CreatureDeadStart()
    {
        
    }

    /// <summary>
    /// 时间-生物死亡结束
    /// </summary>
    public virtual void EventForCreatureDeadEnd(FightCreatureEntity eventFightCreatureEntity)
    { 
        if(!CheckEvent(eventFightCreatureEntity))
        {
            return;
        }
        CreatureDeadEnd();
    }

    public virtual void CreatureDeadEnd()
    {
        buffEntityData.isValid = false;
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