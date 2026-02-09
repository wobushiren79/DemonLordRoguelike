using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AttackModeFalluponArea : BaseAttackMode
{    
    /// <summary>
    /// 攻击-基础
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        AttackHandle();
    }

    /// <summary>
    /// 攻击-生物
    /// </summary>
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

    /// <summary>
    /// 通用攻击处理
    /// </summary>
    public void AttackHandle()
    {
        //击中之后的回调
        CheckHitTargetArea(attackModeData.targetPos, (targetFightCreatureEntity) =>
        {
            //扣血
            targetFightCreatureEntity.UnderAttack(this);
        });
        //播放击中粒子特效
        PlayEffectForHit(attackModeData.targetPos);
        //攻击完了就回收这个攻击
        Destroy();
    }
}
