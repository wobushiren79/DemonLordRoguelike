public class BuffEntityInstantCloneDefenseCreature : BuffEntityInstant
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffInstant(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffInstant(buffEntityData);
        if (isTriggerSuccess == false) return false;
        
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var allDefenseCreature = gameFightLogic.fightData.dlDefenseCreatureData;
        CreatureBean randomCreatureData = allDefenseCreature.List.GetRandomData();

        CreatureBean copyCreatureData = ClassUtil.DeepCopy(randomCreatureData);
        copyCreatureData.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);

        gameFightLogic.fightData.dlDefenseCreatureData.Add(copyCreatureData.creatureUUId, copyCreatureData);

        //通知UI新增一张防御生物卡片(增量添加，不重建整个卡片列表以保留其它卡片的Rest/Fighting状态)
        //克隆体只继承「作用于全体防守生物」的馈赠(靠 trigger_creature_type 自动生效)，单体定向馈赠按 UUID 锁定故不继承
        EventHandler.Instance.TriggerEvent(EventsInfo.Buff_DefenseCreatureAdd, copyCreatureData);
        return true;
    }
}