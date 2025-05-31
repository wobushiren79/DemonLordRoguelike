using UnityEditor;
using UnityEngine;

public class BuffEntityForEVAChange : BuffBaseEntity
{

    public override float GetChangeRateDataForEVA(BuffEntityBean buffEntityData)
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