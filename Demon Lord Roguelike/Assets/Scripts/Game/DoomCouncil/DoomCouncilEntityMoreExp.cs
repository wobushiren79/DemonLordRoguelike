public class DoomCouncilEntityMoreExp : DoomCouncilBaseEntity
{
    /// <summary>
    /// 战斗结算发放经验时触发：给本场出战阵容再追加一份同等经验，实现下次征服经验翻倍
    /// </summary>
    public override bool TriggerGameFightLogicAddExp(int addExp)
    {
        var conquerLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogicConquer>();
        if (conquerLogic == null)
            return false;
        var fightDataForConquer = conquerLogic.fightData as FightBeanForConquer;
        if (fightDataForConquer == null)
            return false;
        //走关卡相同的加经验逻辑，追加一份 addExp 达成翻倍
        conquerLogic.AddLevelExpForLineupCreature(fightDataForConquer, addExp);
        return false;
    }

    /// <summary>
    /// 比赛结束时删除
    /// </summary>
    public override bool TriggerGameFightLogicEndGame()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if(gameFightLogic == null)
            return false;
        var doomCouncilInfo = DoomCouncilInfoCfg.GetItemData(doomCouncilBillId);
        //如果是一个类型
        if (doomCouncilInfo.class_entity_data.Equals(gameFightLogic.fightData.gameFightType.GetEnumName()))
        {
            return true;
        }
        return false;
    }
}