using UnityEditor;
using UnityEngine;

public class FightBuffEntityForMoveSpeedDown : FightBuffBaseEntity
{
    public override FightBuffEntityChangeDataStruct GetChangeDataForMoveSpeed(FightBuffBean fightBuffData)
    {
        FightBuffEntityChangeDataStruct fightBuffEntityChangeData = new FightBuffEntityChangeDataStruct();
        fightBuffEntityChangeData.change = fightBuffData.fightBuffStruct.triggerValue;
        fightBuffEntityChangeData.changeRate = fightBuffData.fightBuffStruct.triggerValueRate;
        return fightBuffEntityChangeData;
    }
}