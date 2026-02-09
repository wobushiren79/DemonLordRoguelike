using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                //扣血
                itemAttacked.UnderAttack(this);
            }
        });
        //攻击完了就回收这个攻击
        Destroy();
    }
}
