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
        FightDropCrystalBean fightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(1, fightCreatureEntity.creatureObj.transform.position);
        //掉落水晶
        FightHandler.Instance.CreateDropCrystal(fightDropCrystal);
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