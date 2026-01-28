using UnityEngine;

public class AIIntentDefenseCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttackPreCD = 0.2f;
    public float timeUpdateAttacking = 0;
    public float timeUpdateAttackingCD = 0.2f;
    //目标AI
    public AIDefenseCreatureEntity selfAIEntity;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    public FightCreatureBean fightCreatureData;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        attackState = 0;
        timeUpdateAttackPreCD = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ASPD);
        timeUpdateAttackingCD = fightCreatureData.creatureData.GetAttackAnimTime();
        //刚进来立即开始一次攻击
        timeUpdateAttackPre = timeUpdateAttackPreCD;
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
                timeUpdateAttackPreCD = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ASPD);
                AttackAttCreature();
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
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
    }

    /// <summary>
    /// 攻击生物
    /// </summary>
    public virtual void AttackAttCreature()
    {
        attackState = 1;
        //如果目标生物已经无了
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefenseCreatureIdle);
            return;
        }
        //如果自己死了
        if (selfAIEntity.selfCreatureEntity == null || selfAIEntity.selfCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefenseCreatureDead);
            return;
        }
        var selfCreatureInfo = fightCreatureData.creatureData.creatureInfo;
        //播放攻击动画
        if (selfCreatureInfo.anim_attack_loop == 1)//如果是循环 只播放攻击动画
        {
            selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, true);
        }
        else
        //如果不是循环，先播放打击再播放待机
        {
            selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false);
            selfAIEntity.selfCreatureEntity.AddAnim(0, SpineAnimationStateEnum.Idle, true, 1);
        }
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
        var findTargetCreature = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Right);
        //如果没有找到最近的生物
        if(findTargetCreature == null)
        {
            ChangeIntent(AIIntentEnum.DefenseCreatureIdle);
            return;
        }
        //设置新目标
        if(findTargetCreature != selfAIEntity.targetCreatureEntity)
        {
            selfAIEntity.targetCreatureEntity = findTargetCreature;
        }
        //如果目标生物已经无了 则重新寻找目标
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefenseCreatureIdle);
            return;
        }
        //继续攻击
        else
        {
            attackState = 0;
        }
    }
}
