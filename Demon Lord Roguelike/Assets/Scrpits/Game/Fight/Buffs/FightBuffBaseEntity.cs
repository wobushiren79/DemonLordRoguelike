using UnityEditor;
using UnityEngine;

public class FightBuffBaseEntity
{
    public virtual void HandleBuff(FightBuffBean fightBuffData)
    {
        //GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //var targetCreature = gameFightLogic.fightData.GetFightCreatureById(fightBuffData.creatureId);
    }

    /// <summary>
    /// 获取移动速度影响BUFF
    /// </summary>
    public virtual FightBuffEntityChangeDataStruct GetChangeDataForMoveSpeed()
    {
        FightBuffEntityChangeDataStruct targetData = new FightBuffEntityChangeDataStruct();
        targetData.changeRate = 1;
        targetData.change = 0;
        return targetData;
    }
}

public struct FightBuffEntityChangeDataStruct
{
    public float changeRate;
    public float change;
}