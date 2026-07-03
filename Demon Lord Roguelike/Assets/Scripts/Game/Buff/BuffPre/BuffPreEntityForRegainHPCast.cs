using UnityEngine;

/// <summary>
/// 前置条件：累计"施放治疗"的HP总量（施放端）
/// 在 RegainHP 事件中关注"BUFF目标作为治疗者(attackerId)"
/// </summary>
public class BuffPreEntityForRegainHPCast : BuffBasePreEntity
{
    /// <summary>
    /// 该前置在 RegainHP 事件中关注"BUFF目标作为治疗者"
    /// </summary>
    public override BuffPreEventRole GetEventRole() => BuffPreEventRole.Attacker;

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
        //累计施放治疗的HP总量 是否满足
        if (buffEntityData.conditionalValue >= preValue)
        {
            return true;
        }
        return false;
    }
}
