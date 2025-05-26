using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeOverlap : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            //攻击完了就回收这个攻击
            Destroy();
            return;
        }
        //检测周围的敌人
        CheckHitTargetArea(attacker.creatureObj.transform.position, (GameFightCreatureEntity itemAttacked) =>
        {
            if (itemAttacked != null && !itemAttacked.IsDead())
            {
                //扣血
                itemAttacked.UnderAttack(this);
            }
        });

        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
