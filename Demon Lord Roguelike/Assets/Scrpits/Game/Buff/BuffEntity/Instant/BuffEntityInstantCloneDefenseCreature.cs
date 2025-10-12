public class BuffEntityInstantCloneDefenseCreature : BuffEntityInstant
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var allDefenseCreature = gameFightLogic.fightData.dlDefenseCreatureData;
        CreatureBean randomCreatureData = allDefenseCreature.List.GetRandomData();

        CreatureBean copyCreatureData = ClassUtil.DeepCopy(randomCreatureData);
        copyCreatureData.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);

        gameFightLogic.fightData.dlDefenseCreatureData.Add(copyCreatureData.creatureUUId, copyCreatureData);
    }
}