using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttactPre = 0;
    //目标AI
    public AIDefCreatureEntity selfAIEntity;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttactPre = 0;
        attackState = 0;
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttactPre += Time.deltaTime;
            float attCD = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetAttCD();
            if (timeUpdateAttactPre >= attCD)
            {
                timeUpdateAttactPre = 0;
                AttackAttCreature();
            }
        }
        //攻击中
        else if (attackState == 1)
        {

        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

    /// <summary>
    /// 攻击生物
    /// </summary>
    public virtual void AttackAttCreature()
    {
        attackState = 1;
        //如果目标生物已经无了
        if (selfAIEntity.targetAttCreatureEntity == null || selfAIEntity.targetAttCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureIdle);
            return;
        }
        //如果自己死了
        if (selfAIEntity.selfDefCreatureEntity == null || selfAIEntity.selfDefCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureDead);
            return;
        }
        var creatureInfo = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetCreatureInfo();
        //获取攻击方式
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
            selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Attack, false);
            //开始攻击
            targetAttackMode.StartAttack(selfAIEntity.selfDefCreatureEntity, selfAIEntity.targetAttCreatureEntity, ActionForAttackEnd);
        });
    }


    /// <summary>
    /// 攻击结束回调
    /// </summary>
    public void ActionForAttackEnd()
    {
        //如果目标生物已经无了 则重新寻找目标
        if (selfAIEntity.targetAttCreatureEntity == null || selfAIEntity.targetAttCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureIdle);
            return;
        }
        //继续攻击
        else
        {
            attackState = 0;
        }
    }
}
