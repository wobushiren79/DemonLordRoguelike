using UnityEditor;
using UnityEngine;

public class BuffEntityForATKChange : BuffBaseEntity
{
    public override float GetChangeDataForATK(BuffEntityBean buffEntityData)
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

    public override float GetChangeRateDataForATK(BuffEntityBean buffEntityData)
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