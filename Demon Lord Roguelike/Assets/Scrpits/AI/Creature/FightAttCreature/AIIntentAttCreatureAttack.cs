using UnityEngine;

public class AIIntentAttCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttackPreCD = 0.2f;
    public float timeUpdateAttacking = 0;
    public float timeUpdateAttackingCD = 0.2f;
    //目标AI
    public AIAttCreatureEntity selfAIEntity;
    public FightCreatureBean fightCreatureData;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        attackState = 0;
        timeUpdateAttackPreCD = fightCreatureData.creatureData.GetAttackCDTime();
        timeUpdateAttackingCD = fightCreatureData.creatureData.GetAttackAnimTime();
        //设置待机动作
        string animNameAppoint = fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttackPre += Time.deltaTime;

            if (timeUpdateAttackPre >= timeUpdateAttackPreCD)
            {
                timeUpdateAttackPre = 0;
                timeUpdateAttackPreCD = fightCreatureData.creatureData.GetAttackCDTime();
                AttackDefCreatureStart();
            }
        }
        //攻击中
        else if (attackState == 1)
        {
            timeUpdateAttacking += Time.deltaTime;

            if (timeUpdateAttacking >= timeUpdateAttackingCD)
            {
                timeUpdateAttacking = 0;
                timeUpdateAttackingCD = fightCreatureData.creatureData.GetAttackAnimTime();
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
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureIdle);
            return;
        }
        //如果自己死了
        if (selfAIEntity.selfCreatureEntity == null || selfAIEntity.selfCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureDead);
            return;
        }
        //播放攻击动画
        string animNameAppoint = fightCreatureData.creatureData.creatureInfo.anim_attack;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false, animNameAppoint: animNameAppoint);
    }

    /// <summary>
    /// 攻击结束
    /// </summary>
    public virtual void AttackDefCreatureStartEnd()
    {
        attackState = 2;
        //开始创建攻击模块
        FightHandler.Instance.StartCreateAttackMode(selfAIEntity.selfCreatureEntity, selfAIEntity.targetCreatureEntity, ActionForAttackEnd);
    }

    /// <summary>
    /// 攻击结束回调
    /// </summary>
    public void ActionForAttackEnd(BaseAttackMode attackMode)
    {
        var findTargetCreature = selfAIEntity.FindCreatureEntity(DirectionEnum.Left);
        //如果没有找到最近的生物
        if (findTargetCreature == null)
        {
            ChangeIntent(AIIntentEnum.AttCreatureIdle);
            return;
        }
        //设置新目标
        if (findTargetCreature != selfAIEntity.targetCreatureEntity)
        {
            selfAIEntity.targetCreatureEntity = findTargetCreature;
        }
        //如果目标生物已经无了 则重新寻找目标
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
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
