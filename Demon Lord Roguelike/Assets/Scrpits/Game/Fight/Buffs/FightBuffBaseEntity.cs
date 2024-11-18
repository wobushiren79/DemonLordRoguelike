using UnityEditor;
using UnityEngine;

public class FightBuffBaseEntity
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    /// <param name="fightBuffData"></param>
    public virtual void TriggerBuff(FightBuffBean fightBuffData)
    {
        //GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //var targetCreature = gameFightLogic.fightData.GetFightCreatureById(fightBuffData.creatureId);
    }

    /// <summary>
    /// 获取移动速度影响BUFF
    /// </summary>
    public virtual FightBuffEntityChangeDataStruct GetChangeDataForMoveSpeed(FightBuffBean fightBuffData)
    {
        FightBuffEntityChangeDataStruct targetData = new FightBuffEntityChangeDataStruct();
        targetData.changeRate = 0;
        targetData.change = 0;
        return targetData;
    }
}

public struct FightBuffEntityChangeDataStruct
{
    public float changeRate;
    public float change;
}