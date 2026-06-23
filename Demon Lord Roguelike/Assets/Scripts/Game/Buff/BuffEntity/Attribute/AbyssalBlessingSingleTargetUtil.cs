/// <summary>
/// 单体定向深渊馈赠 BUFF 的辅助逻辑：在选取馈赠时随机锁定一只防守生物
/// </summary>
public static class AbyssalBlessingSingleTargetUtil
{
    /// <summary>
    /// 从当前战斗的防守生物池(dlDefenseCreatureData)中随机取一只，返回其 creatureUUId；池为空或不在战斗中返回 null
    /// <para>注意：dlDefenseCreatureData 仅含普通防守生物(不含防守核心)，且与玩家存档共享引用，故此处只读取 UUID、绝不修改其属性。</para>
    /// </summary>
    public static string PickRandomDefenseCreatureUUId()
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var dlDefenseCreatureData = gameFightLogic?.fightData?.dlDefenseCreatureData;
        if (dlDefenseCreatureData == null || dlDefenseCreatureData.List.Count == 0)
        {
            return null;
        }
        CreatureBean randomCreatureData = dlDefenseCreatureData.List.GetRandomData();
        return randomCreatureData?.creatureUUId;
    }
}
