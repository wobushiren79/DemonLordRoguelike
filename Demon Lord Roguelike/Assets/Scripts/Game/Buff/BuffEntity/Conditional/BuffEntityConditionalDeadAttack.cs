using UnityEngine;

public class BuffEntityConditionalDeadAttack : BuffEntityConditionalDead
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        var targetFightCreatureEntity = GetFightCreatureEntityForTarget();
        if (targetFightCreatureEntity == null)
            return false;
        //开始攻击
        BuffEntityConditionalAttack.StartCreateAttack( buffEntityData,  targetFightCreatureEntity);
        return true;
    }
}