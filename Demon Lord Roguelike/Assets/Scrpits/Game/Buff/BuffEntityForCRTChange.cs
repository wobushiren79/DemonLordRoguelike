using UnityEditor;
using UnityEngine;

public class BuffEntityForCRTChange : BuffBaseEntity
{
    public override float GetChangeRateDataForCRT(BuffEntityBean buffEntityData)
    {
        if (CheckIsPre(buffEntityData))
        {
            return buffEntityData.buffInfo.trigger_value_rate;
        }
        else
        {
            return 0;
        }
    }
}