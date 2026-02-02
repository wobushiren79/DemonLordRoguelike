public class BuffEntityConditionalDeadRebirth : BuffEntityConditionalDead
{
    public override void EventForCreatureDeadEnd(FightCreatureBean fightCreatureBean)
    {
        base.EventForCreatureDeadEnd(fightCreatureBean);
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
        if (fightCreatureData == null)
            return false;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var creatureData = gameFightLogic.fightData.GetCreatureDataById(buffEntityData.targetCreatureId);
        //重生
        CreatureHandler.Instance.CreateDefenseCreatureEntity(creatureData, fightCreatureData.positionCreate);
        //重生不继承重生BUFF 所以要删除
        BuffHandler.Instance.RemoveFightCreatureBuffs<BuffEntityConditionalDeadRebirth>(creatureData.creatureUUId);
        return true;
    }
}