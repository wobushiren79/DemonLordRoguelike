using System.Globalization;

/// <summary>
/// 终焉议会议案-敌人强度调整(「挑战更强/更弱的敌人」)
/// class_entity_data 配置强度倍率(如"2"翻倍强 / "0.5"减半弱)，作用于下一整场征服模式所有关卡(含BOSS)敌人的 HP/护甲/攻击力；
/// TriggerFirst 返回 false 使其常驻议案列表，直到一场征服战斗结束时消耗移除
/// </summary>
public class DoomCouncilEntityEnemyIntensity : DoomCouncilBaseEntity
{
    /// <summary>
    /// 敌人强度倍率(解析 class_entity_data，用不变区域性以兼容小数点；解析失败按1不改变)
    /// </summary>
    /// <returns>敌人强度倍率</returns>
    public override float GetEnemyIntensityRate()
    {
        if (float.TryParse(doomCouncilInfo.class_entity_data, NumberStyles.Float, CultureInfo.InvariantCulture, out float rate))
        {
            return rate;
        }
        return 1f;
    }

    /// <summary>
    /// 征服模式战斗结束时消耗移除(仅征服模式生效，其它模式结束不消耗，保证效果作用于下一场征服)
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
