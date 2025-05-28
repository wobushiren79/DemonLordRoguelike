using UnityEditor;
using UnityEngine;

public class BuffEntityForMoveSpeedDown : BuffBaseEntity
{
    public override BuffEntityChangeDataStruct GetChangeDataForMoveSpeed(BuffEntityBean buffEntityData)
    {
        BuffEntityChangeDataStruct fightBuffEntityChangeData = new BuffEntityChangeDataStruct();
        fightBuffEntityChangeData.change = -buffEntityData.buffInfo.trigger_value;
        fightBuffEntityChangeData.changeRate = -buffEntityData.buffInfo.trigger_value_rate;
        return fightBuffEntityChangeData;
    }
}