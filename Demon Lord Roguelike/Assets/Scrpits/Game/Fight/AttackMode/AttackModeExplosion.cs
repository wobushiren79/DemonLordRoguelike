using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeExplosion : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            //攻击完了就回收这个攻击
            Destroy();
            return;
        }
        //播放击中粒子特效
        PlayEffectForHit(attacker.creatureObj.transform.position);
        //检测周围的敌人
        CheckHitTargetArea(attacker.creatureObj.transform.position, (FightCreatureEntity itemAttacked) =>
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
        //自己也死
        attacker.SetCreatureDead();
    }
}
