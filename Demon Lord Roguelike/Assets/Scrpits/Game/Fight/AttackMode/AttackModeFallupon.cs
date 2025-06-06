using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeFallupon : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            //扣血
            attacked.UnderAttack(this);
            //播放击中粒子特效
            PlayEffectForHit(attacked.creatureObj.transform.position);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
