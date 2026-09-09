using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeMelee : BaseAttackMode
{
    public override void StartAttack()
    {
        base.StartAttack();
        //攻击完了就回收这个攻击
        Destroy();
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        OnMeleeStartAttack(attacker, attacked, actionForAttackEnd);
    }

    /// <summary>
    /// 近战出手结算（虚方法，供多段近战等子类改写结算节奏；默认单段：命中→回收→回调）
    /// </summary>
    protected virtual void OnMeleeStartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        MeleeHit(attacker, attacked);
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }

    /// <summary>
    /// 近战单段命中：扣血 + 播放击中粒子特效（攻击者物体已销毁时特效回退到目标位置）
    /// </summary>
    protected void MeleeHit(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            //扣血
            attacked.UnderAttack(this);
            //播放击中粒子特效
            Vector3 hitPos = attacker.creatureObj != null ? attacker.creatureObj.transform.position : attacked.creatureObj.transform.position;
            PlayEffectForHit(hitPos);
        }
    }
}
