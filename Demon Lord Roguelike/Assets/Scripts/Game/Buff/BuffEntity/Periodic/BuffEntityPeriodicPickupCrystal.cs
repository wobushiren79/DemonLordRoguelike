public class BuffEntityPeriodicPickupCrystal : BuffEntityPeriodic
{
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        //获取指定战斗生物
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null || targetCreature.IsDead())
        {
            return false;
        }
        //拾取水晶
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        gameFightLogic.PickupCrystalForCreature(targetCreature, buffEntityData.buffData.trigger_value);
        return true;
    }
}