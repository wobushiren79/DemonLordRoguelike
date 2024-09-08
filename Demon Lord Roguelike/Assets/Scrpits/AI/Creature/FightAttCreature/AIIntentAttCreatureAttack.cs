using UnityEngine;

public class AIIntentAttCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttacking = 0;
    //目标AI
    public AIAttCreatureEntity selfAIEntity;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;

        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        attackState = 0;

        //设置待机动作
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttackPre += Time.deltaTime;
            float attCD = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.GetAttCD();
            if (timeUpdateAttackPre >= attCD)
            {
                timeUpdateAttackPre = 0;
                AttackDefCreatureStart();
            }
        }
        //攻击中
        else if (attackState == 1)
        {
            timeUpdateAttacking += Time.deltaTime;
            float attAnimCastTime = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.GetAttAnimCastTime();
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
    /// 攻击开始
    /// </summary>
    public virtual void AttackDefCreatureStart()
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
        //播放攻击动画
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false);
    }

    /// <summary>
    /// 攻击结束
    /// </summary>
    public virtual void AttackDefCreatureStartEnd()
    {
        attackState = 2;
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.creatureInfo;
        //获取攻击方式
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
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
