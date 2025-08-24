using UnityEngine;

public class BuffBasePreEntity
{
    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public virtual bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        return false;
    }

    /// <summary>
    /// 获取触发生物
    /// </summary>
    public GameFightCreatureEntity GetTargetCreatureEntity(string creatureId)
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
        {
            return null;
        }
        var creatureEntity = gameFightLogic.fightData.GetCreatureById(creatureId);
        if (creatureEntity == null)
        {
            return null;
        }
        return creatureEntity;
    }
}
