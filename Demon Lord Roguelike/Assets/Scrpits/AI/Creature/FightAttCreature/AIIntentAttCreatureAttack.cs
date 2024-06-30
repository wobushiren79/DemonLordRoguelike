using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttactPre = 0;
    //目标AI
    public AIAttCreatureEntity selfAIEntity;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;

        timeUpdateAttactPre = 0;
        attackState = 0;

        //设置待机动作
        selfAIEntity.selfAttCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttactPre += Time.deltaTime;
            float attCD = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetAttCD();
            if (timeUpdateAttactPre >= attCD)
            {
                timeUpdateAttactPre = 0;
                AttackDefCreature();
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
    /// 攻击防守生物
    /// </summary>
    public virtual void AttackDefCreature()
    {
        attackState = 1;
        //如果目标生物已经无了
        if (selfAIEntity.targetDefCreatureEntity == null || selfAIEntity.targetDefCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureIdle);
            return;
        }
        //如果自己死了
        if (selfAIEntity.selfAttCreatureEntity == null || selfAIEntity.selfAttCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureDead);
            return;
        }
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetCreatureInfo();
        //获取攻击方式
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
            //设置移动动作
            selfAIEntity.selfAttCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Attack, false);
            //开始攻击
            targetAttackMode.StartAttack(selfAIEntity.selfAttCreatureEntity, selfAIEntity.targetDefCreatureEntity, ActionForAttackEnd);
        });
    }


    /// <summary>
    /// 攻击结束回调
    /// </summary>
    public void ActionForAttackEnd()
    {
        //如果目标生物已经无了 则重新寻找目标
        if (selfAIEntity.targetDefCreatureEntity == null || selfAIEntity.targetDefCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureIdle);
            return;
        }
        //继续攻击
        else
        {
            attackState = 0;
        }
    }
}
