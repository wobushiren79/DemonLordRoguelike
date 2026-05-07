using UnityEngine;

/// <summary>
/// 属性BUFF-攻击时间
/// </summary>
public class BuffEntityAttributeAttackTime : BuffBaseEntity
{
    /// <summary>
    /// 改变攻击时间数据
    /// </summary>
    public virtual void ChangeAttackTimeData(ref float timeAttackPre, ref float timeAttacking)
    {
        float rate = buffEntityData.buffData.trigger_value_rate;
        timeAttackPre *= rate;
        timeAttacking *= rate;
    }
}
