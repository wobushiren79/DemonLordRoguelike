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

        string animNameAppoint = selfAIEntity.selfDefCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, false, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttackPre += Time.deltaTime;
            float attCD = selfAIEntity.selfDefCreatureEntity.fightCreatureData.creatureData.GetAttackCD();
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
            float attAnimCastTime = selfAIEntity.selfDefCreatureEntity.fightCreatureData.creatureData.GetAttackAnimCastTime();
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
        string animNameAppointAttack = selfAIEntity.selfDefCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_attack;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false, animNameAppoint: animNameAppointAttack);
        string animNameAppointIdle = selfAIEntity.selfDefCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfDefCreatureEntity.AddAnim(0, SpineAnimationStateEnum.Idle, true, 1, animNameAppoint: animNameAppointIdle);
    }

    /// <summary>
    /// 攻击结束
    /// </summary>
    public virtual void AttackDefCreatureStartEnd()
    {
        attackState = 2;
        //开始创建攻击模块
        FightHandler.Instance.StartCreateAttackMode(selfAIEntity.selfDefCreatureEntity, selfAIEntity.targetAttCreatureEntity, ActionForAttackEnd);
    }

    /// <summary>
    /// 攻击结束回调
    /// </summary>
    public void ActionForAttackEnd(BaseAttackMode attackMode)
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
