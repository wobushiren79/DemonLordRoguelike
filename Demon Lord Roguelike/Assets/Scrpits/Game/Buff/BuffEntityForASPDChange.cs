using UnityEditor;
using UnityEngine;

public class BuffEntityForASPDChange : BuffBaseEntity
{
    public override float GetChangeDataForASPD(BuffEntityBean buffEntityData)
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

    public override float GetChangeRateDataForASPD(BuffEntityBean buffEntityData)
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