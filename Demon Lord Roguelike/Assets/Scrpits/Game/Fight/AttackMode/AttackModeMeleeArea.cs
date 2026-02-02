using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AttackModeMeleeArea : BaseAttackMode
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
        //击中之后的回调
        CheckHitTargetArea(startPostion, (targetFightCreatureEntity) =>
        {
            //扣血
            targetFightCreatureEntity.UnderAttack(this);
        });
        //播放击中粒子特效
        PlayEffectForHit(attacker.creatureObj.transform.position);
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
