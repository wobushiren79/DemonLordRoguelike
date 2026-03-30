using UnityEngine;

public class BuffEntityConditionalAttackAgain : BuffEntityConditional
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;
        
        //获取指定战斗生物
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null || targetCreature.IsDead())
        {
            return false;
        }
        //如果生物没有再攻击中就不用执行
        if (targetCreature.aiEntity.currentIntent is AIIntentCreatureAttack aIIntentCreatureAttack)
        {
            aIIntentCreatureAttack.AttackImm();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 处理检测
    /// </summary>
    public override void HandleForEvent()
    {
        base.HandleForEvent();
        if (CheckIsPre(buffEntityData))
        {
            buffEntityData.conditionalValue = 0;
            //触发BUFF
            TriggerBuffConditional(buffEntityData);
        }
    }
}