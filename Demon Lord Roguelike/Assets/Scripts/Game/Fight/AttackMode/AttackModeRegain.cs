using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeRegain : BaseAttackMode
{
    public override void StartAttack()
    {
        base.StartAttack();
        //攻击完了就回收这个攻击
        Destroy();
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        //出手瞬间校验目标仍满足回复条件：索敌筛选只发生在锁定时，前摇期间目标可能已被其他治疗源奶满，满载则空放（不回复/不播特效/不跳字/不播音效），回调照走让 AI 重搜
        if (attacker != null && attacked != null && !attacked.IsDead() && CheckTargetRegainable(attacked))
        {
            HandleRegain(attacker, attacked);
            //播放一个范围攻击特效
            PlayEffectForHit(attacked.creatureObj.transform.position);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }

    /// <summary>
    /// 处理回复逻辑
    /// </summary>
    public virtual void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {

    }

    /// <summary>
    /// 校验目标当前是否仍可被回复（与索敌筛选同口径；子类按回复类型判定 HP 不满 / DR 不满）
    /// </summary>
    public virtual bool CheckTargetRegainable(FightCreatureEntity attacked)
    {
        return true;
    }
}
