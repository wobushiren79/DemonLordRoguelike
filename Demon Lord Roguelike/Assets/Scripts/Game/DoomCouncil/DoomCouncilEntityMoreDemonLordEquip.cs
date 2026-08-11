/// <summary>
/// 终焉议会议案-想要更多魔王装备(「想要更多魔王装备！」)
/// 下一次征服模式通关领奖时，宝箱基础奖励重生成变为「3件全魔王专属装备」(DemonLord)；
/// 与「想要更多装备」同时在列时本议案优先(严格上位)；TriggerFirst 返回 false 常驻议案列表，征服战斗结束(输赢皆然)时消耗移除
/// </summary>
public class DoomCouncilEntityMoreDemonLordEquip : DoomCouncilBaseEntity
{
    /// <summary>
    /// 征服模式战斗结束时消耗移除(仅征服模式消耗，其它模式结束不消耗，保证效果作用于下一场征服)
    /// </summary>
    /// <returns>是否结束(移除)</returns>
    public override bool TriggerGameFightLogicEndGame()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
        {
            return false;
        }
        return gameFightLogic.fightData.gameFightType == GameFightTypeEnum.Conquer;
    }
}
