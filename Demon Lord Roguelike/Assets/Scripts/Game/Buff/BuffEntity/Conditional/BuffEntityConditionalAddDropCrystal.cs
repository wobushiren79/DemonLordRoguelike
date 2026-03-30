/// <summary>
/// 增加掉落的魔晶
/// </summary>
public class BuffEntityConditionalAddDropCrystal : BuffEntityConditional
{
    public FightDropCrystalBean addFightDropCrystal;

    public override void HandleForEvent()
    {
        base.HandleForEvent();
        TriggerBuffConditional(buffEntityData);
    }

    /// <summary>
    /// 事件触发
    /// </summary>
    public override void EventForCreatureDeadDropCrystal(FightDropCrystalBean fightDropCrystal)
    {
        addFightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(fightDropCrystal);
        base.EventForCreatureDeadDropCrystal(fightDropCrystal);
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