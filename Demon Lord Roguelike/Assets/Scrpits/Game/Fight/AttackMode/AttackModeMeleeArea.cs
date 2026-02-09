using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AttackModeMeleeArea : BaseAttackMode
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
        //击中之后的回调
        CheckHitTargetArea(attackModeData.startPos, (targetFightCreatureEntity) =>
        {
            //扣血
            targetFightCreatureEntity.UnderAttack(this);
        });
        //播放击中粒子特效
        PlayEffectForHit(attackModeData.startPos);
        //攻击完了就回收这个攻击
        Destroy();
    }
}
