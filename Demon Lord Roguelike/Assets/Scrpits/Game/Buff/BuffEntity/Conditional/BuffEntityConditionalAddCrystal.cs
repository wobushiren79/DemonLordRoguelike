public class BuffEntityConditionalAddCrystal : BuffEntityConditional
{
    public string nameRegisterEvent = null;
    public FightDropCrystalBean addFightDropCrystal;

    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        if (!buffInfo.class_entity_data.IsNull())
        {
            nameRegisterEvent = buffInfo.class_entity_data;
            //监听魔晶掉落事件
            if (buffInfo.class_entity_data.Equals(EventsInfo.GameFightLogic_CreatureDeadDropCrystal))
            {
                EventHandler.Instance.RegisterEvent<FightDropCrystalBean>(buffInfo.class_entity_data, EventForCreatureDeadDropCrystal);
            }
        }
    }

    public override void ClearData()
    {
        base.ClearData();
        //清理数据的时候需要清理一下注册的信息
        if (nameRegisterEvent != null)
        {
            var buffInfo = buffEntityData.GetBuffInfo();
            if (buffInfo.class_entity_data.Equals(EventsInfo.GameFightLogic_CreatureDeadDropCrystal))
            {
                EventHandler.Instance.UnRegisterEvent<FightDropCrystalBean>(nameRegisterEvent, EventForCreatureDeadDropCrystal);
            }
        }
        nameRegisterEvent = null;
    }

    /// <summary>
    /// 事件触发
    /// </summary>
    public void EventForCreatureDeadDropCrystal(FightDropCrystalBean fightDropCrystal)
    {
        addFightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(fightDropCrystal);
        //触发BUFF
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
        //掉落水晶
        FightHandler.Instance.CreateDropCrystal(addFightDropCrystal);
        return true;
    }
}