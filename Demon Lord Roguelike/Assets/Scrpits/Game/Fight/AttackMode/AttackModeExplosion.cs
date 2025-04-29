using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeExplosion : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            //攻击完了就回收这个攻击
            Destory();
            return;
        }
        //播放一个范围攻击特效
        if (attackModeInfo.effect_hit != 0)
        {
            float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
            EffectHandler.Instance.ShowEffect(attackModeInfo.effect_hit, attacker.creatureObj.transform.position, colliderAreaSize[0]);
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
        Destory();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
        //自己也死
        attacker.SetCreatureDead();
    }
}
