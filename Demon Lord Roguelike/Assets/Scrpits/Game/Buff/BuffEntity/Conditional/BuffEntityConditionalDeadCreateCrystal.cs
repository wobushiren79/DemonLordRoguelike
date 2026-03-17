public class BuffEntityConditionalDeadCreateCrystal : BuffEntityConditionalDead
{
    public override void CreatureDeadEnd()
    {
        base.CreatureDeadEnd();
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
        var fightCreatureEntity = GetFightCreatureEntityForTarget();
        if (fightCreatureEntity == null)
            return false;
        FightDropCrystalBean fightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(1, fightCreatureEntity.creatureObj.transform.position);
        //掉落水晶
        FightHandler.Instance.CreateDropCrystal(fightDropCrystal);

        return true;
    }
}