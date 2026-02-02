using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeRegain : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            HandleRegain(attacker, attacked);
            //播放一个范围攻击特效
            PlayEffectForHit(attacked.creatureObj.transform.position);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }

    /// <summary>
    /// 处理回复逻辑
    /// </summary>
    public virtual void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {

    }
}
