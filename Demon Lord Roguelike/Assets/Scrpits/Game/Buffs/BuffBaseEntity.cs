using UnityEditor;
using UnityEngine;

public class BuffBaseEntity
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    /// <param name="buffEntityData"></param>
    public virtual void TriggerBuff(BuffEntityBean buffEntityData)
    {
        
    }

    /// <summary>
    /// 获取移动速度影响BUFF
    /// </summary>
    public virtual BuffEntityChangeDataStruct GetChangeDataForMoveSpeed(BuffEntityBean buffEntityData)
    {
        BuffEntityChangeDataStruct targetData = new BuffEntityChangeDataStruct();
        targetData.changeRate = 0;
        targetData.change = 0;
        return targetData;
    }
}

public struct BuffEntityChangeDataStruct
{
    public float changeRate;
    public float change;
}