using UnityEditor;
using UnityEngine;

public class BuffEntityForMSPDChange : BuffBaseEntity
{
    public override float GetChangeDataForMSPD(BuffEntityBean buffEntityData)
    {
        if (CheckIsPre(buffEntityData))
        {
            return buffEntityData.buffInfo.trigger_value;
        }
        else
        {
            return 0;
        }
    }

    public override float GetChangeRateDataForMSPD(BuffEntityBean buffEntityData)
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