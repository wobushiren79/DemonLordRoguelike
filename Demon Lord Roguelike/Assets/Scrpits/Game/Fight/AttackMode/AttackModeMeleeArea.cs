using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeMeleeArea : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        //击中之后的回调
        CheckHitTargetArea(startPostion, (targetFightCreatureEntity) =>
        {
            //扣血
            targetFightCreatureEntity.UnderAttack(this);
        });
        //播放一个范围攻击特效
        if (attackModeInfo.effect_hit != 0)
        {
            float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
            EffectHandler.Instance.ShowEffect(attackModeInfo.effect_hit, gameObject.transform.position, colliderAreaSize[0]);
        }
        //攻击完了就回收这个攻击
        Destory();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
