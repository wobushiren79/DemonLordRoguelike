public class BuffEntityPeriodicPickupCrystal : BuffEntityConditional
{
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        //获取指定战斗生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureId, CreatureFightTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return false;
        }
        //拾取水晶
        gameFightLogic.PickupCrystalForCreature(targetCreature, buffEntityData.buffData.trigger_value);
        return true;
    }
}