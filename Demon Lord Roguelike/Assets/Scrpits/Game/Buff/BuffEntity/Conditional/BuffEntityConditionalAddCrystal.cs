public class BuffEntityConditionalAddCrystal : BuffEntityConditional
{
    public string nameRegisterEvent = null;
    public FightDropCrystalBean addFightDropCrystal;

    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        if (!buffEntityData.buffInfo.class_entity_data.IsNull())
        {
            nameRegisterEvent = buffEntityData.buffInfo.class_entity_data;
            if (buffEntityData.buffInfo.class_entity_data.Equals(EventsInfo.GameFightLogic_CreatureDeadDropCrystal))
            {
                EventHandler.Instance.RegisterEvent<FightDropCrystalBean>(buffEntityData.buffInfo.class_entity_data, EventForCreatureDeadDropCrystal);
            }
        }
    }

    public override void ClearData()
    {
        base.ClearData();
        if (nameRegisterEvent != null)
        {
            if (buffEntityData.buffInfo.class_entity_data.Equals(EventsInfo.GameFightLogic_CreatureDeadDropCrystal))
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
        //触发BUG
        TriggerBuff(buffEntityData);
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);
        //掉落水晶
        FightHandler.Instance.CreateDropCrystal(addFightDropCrystal);
    }
}