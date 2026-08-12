using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 重叠攻击：以自身为中心的范围触碰检测，命中目标走正常 UnderAttack 伤害管线（扣血/伤害数字/受击特效/伤害统计）。
/// 若需要"只上DEBUFF不造成伤害"的重叠攻击（如烂泥史莱姆/毒液史莱姆），改用子类 AttackModeOverlapNoDamage。
/// </summary>
public class AttackModeOverlap : BaseAttackMode
{
    public override void StartAttack()
    {
        base.StartAttack();
        AttackHandle();
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            //攻击完了就回收这个攻击
            Destroy();
            return;
        }
        AttackHandle();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }

    public void AttackHandle()
    {
        //检测周围的敌人
        CheckHitTargetArea(attackModeData.startPos, (FightCreatureEntity itemAttacked) =>
        {
            if (itemAttacked != null && !itemAttacked.IsDead())
            {
                //命中处理（默认扣血，子类可改为无伤害触碰等）
                HitTarget(itemAttacked);
            }
        });
        //攻击完了就回收这个攻击
        Destroy();
    }

    /// <summary>
    /// 命中单个目标的处理：默认走正常受击伤害管线
    /// </summary>
    protected virtual void HitTarget(FightCreatureEntity itemAttacked)
    {
        //扣血
        itemAttacked.UnderAttack(this);
    }
}
