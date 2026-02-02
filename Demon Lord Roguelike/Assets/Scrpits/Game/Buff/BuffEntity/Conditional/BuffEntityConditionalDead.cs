public class BuffEntityConditionalDead : BuffEntityConditional
{
    //战斗生物数据（注：该数据会在下一帧回收 请在当前帧数使用该数据）
    public FightCreatureBean fightCreatureData;
    public string nameRegisterEvent;
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        if (!buffInfo.class_entity_data.IsNull())
        {
            nameRegisterEvent = buffInfo.class_entity_data;
            //监听生物死亡
            if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadStart))
            {
                EventHandler.Instance.RegisterEvent<FightCreatureBean>(nameRegisterEvent, EventForCreatureDeadStart);
            }
            else if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadEnd))
            {
                EventHandler.Instance.RegisterEvent<FightCreatureBean>(nameRegisterEvent, EventForCreatureDeadEnd);
            }
        }
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
                EventHandler.Instance.UnRegisterEvent<FightCreatureBean>(nameRegisterEvent, EventForCreatureDeadStart);
            }
            else if (nameRegisterEvent.Equals(EventsInfo.GameFightLogic_CreatureDeadEnd))
            {
                EventHandler.Instance.UnRegisterEvent<FightCreatureBean>(nameRegisterEvent, EventForCreatureDeadEnd);
            }
        }
        nameRegisterEvent = null;
    }

    /// <summary>
    /// 时间-生物死亡开始
    /// </summary>
    public virtual void EventForCreatureDeadStart(FightCreatureBean fightCreatureBean)
    {
        if(buffEntityData.isValid == false) return;
        this.fightCreatureData = fightCreatureBean;
    }

    /// <summary>
    /// 时间-生物死亡结束
    /// </summary>
    public virtual void EventForCreatureDeadEnd(FightCreatureBean fightCreatureBean)
    { 
        if(buffEntityData.isValid == false) return;
        this.fightCreatureData = fightCreatureBean;
        buffEntityData.isValid = false;
    }
}