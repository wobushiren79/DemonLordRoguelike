using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AIIntentDefCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttacking = 0;

    //目标AI
    public AIDefCreatureEntity selfAIEntity;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttackPre = 0;
        attackState = 0;
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttackPre += Time.deltaTime;
            float attCD = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetAttCD();
            if (timeUpdateAttackPre >= attCD)
            {
                timeUpdateAttackPre = 0;
                AttackAttCreature();
            }
        }
        //攻击中
        else if (attackState == 1)
        {
            timeUpdateAttacking += Time.deltaTime;
            float attAnimCastTime = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetAttAnimCastTime();
            if (timeUpdateAttacking >= attAnimCastTime)
            {
                timeUpdateAttacking = 0;
                AttackDefCreatureStartEnd();
            }
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
        //播放攻击动画
        selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Attack, false);
        selfAIEntity.selfDefCreatureEntity.AddAnim(0, AnimationCreatureStateEnum.Idle, true, 1);
    }

    /// <summary>
    /// 攻击结束
    /// </summary>
    public virtual void AttackDefCreatureStartEnd()
    {
        attackState = 2;
        var creatureInfo = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetCreatureInfo();
        //获取攻击方式
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
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
