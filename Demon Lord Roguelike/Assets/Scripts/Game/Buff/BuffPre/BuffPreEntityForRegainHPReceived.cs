using UnityEngine;

/// <summary>
/// 前置条件：累计"被治疗"的HP总量（接收端）
/// 在 RegainHP 事件中关注"BUFF目标作为被治疗者(attackedId)"
/// </summary>
public class BuffPreEntityForRegainHPReceived : BuffBasePreEntity
{
    /// <summary>
    /// 该前置在 RegainHP 事件中关注"BUFF目标作为被治疗者"
    /// </summary>
    public override BuffPreEventRole GetEventRole() => BuffPreEventRole.Attacked;

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        FightCreatureEntity creatureEntity = GetTargetCreatureEntity(buffEntityData.targetCreatureUUId);
        if (creatureEntity == null)
        {
            return false;
        }
        //累计被治疗的HP总量 是否满足
        if (buffEntityData.conditionalValue >= preValue)
        {
            return true;
        }
        return false;
    }
}
