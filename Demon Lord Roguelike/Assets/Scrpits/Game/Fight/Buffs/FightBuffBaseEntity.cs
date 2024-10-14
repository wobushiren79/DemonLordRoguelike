using UnityEditor;
using UnityEngine;

public class FightBuffBaseEntity
{
    public virtual void HandleBuff(FightBuffBean fightBuffData)
    {
        //GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //var targetCreature = gameFightLogic.fightData.GetFightCreatureById(fightBuffData.creatureId);
    }
}