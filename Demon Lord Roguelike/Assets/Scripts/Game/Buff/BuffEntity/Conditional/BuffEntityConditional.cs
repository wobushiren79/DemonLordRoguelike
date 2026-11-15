/// <summary>
/// 条件性触发 不受时间影响
/// </summary>
public class BuffEntityConditional : BuffBaseEntity
{
    #region 条件触发
    /// <summary>推进在场计时，效果由条件事件驱动。</summary>
    public override void UpdateBuffTime(float buffTime)
    {
        buffEntityData.timeUpdateTotal += buffTime;
    }

    /// <summary>满足前置时消耗一份累计门槛，超出部分留到下一次对应事件。</summary>
    protected bool TryConsumeTriggerCondition()
    {
        if (!CheckIsPre(buffEntityData))
            return false;

        float threshold = 0;
        var conditions = buffEntityData.GetBuffInfo().GetPreInfo();
        if (conditions != null)
        {
            foreach (var condition in conditions)
            {
                //击杀、承伤、造伤、受到治疗、施加治疗共享条件累计值。
                if (condition.Key is 2001 or 3001 or 4001 or 5001 or 6001)
                    threshold = UnityEngine.Mathf.Max(threshold, condition.Value);
            }
        }
        buffEntityData.conditionalValue = threshold > 0 ? buffEntityData.conditionalValue - threshold : 0;
        return true;
    }
    #endregion
}
