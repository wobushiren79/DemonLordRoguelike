public class DoomCouncilEntityMoreExp : DoomCouncilBaseEntity
{
    public override bool TriggerGameFightLogicAddExp(int addExp)
    {
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