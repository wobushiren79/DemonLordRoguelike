using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeMelee : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            //扣血
            attacked.UnderAttack(this);
            //播放击中粒子特效
            PlayEffectForHit(attacker.creatureObj.transform.position);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
