using UnityEngine;

/// <summary>
/// 属性BUFF-攻击时间
/// </summary>
public class BuffEntityAttributeAttackTime : BuffBaseEntity
{
    /// <summary>
    /// 攻击时间rate最小值 防止配置为0或多重叠加后时间趋近于0导致AI无限攻击
    /// </summary>
    private const float MIN_RATE = 0.1f;

    /// <summary>
    /// 改变攻击时间数据
    /// </summary>
    public virtual void ChangeAttackTimeData(ref float timeAttackPre, ref float timeAttacking)
    {
        float rate = buffEntityData.buffData.trigger_value_rate;
        if (rate < MIN_RATE)
        {
            rate = MIN_RATE;
        }
        timeAttackPre *= rate;
        timeAttacking *= rate;
    }
}
