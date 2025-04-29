using UnityEngine;

public class AIIntentDefCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttackPreCD = 0.2f;
    public float timeUpdateAttacking = 0;
    public float timeUpdateAttackingCD = 0.2f;
    //目标AI
    public AIDefCreatureEntity selfAIEntity;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    public FightCreatureBean fightCreatureData;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        attackState = 0;
        timeUpdateAttackPreCD = fightCreatureData.creatureData.GetAttackCDTime();
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
                timeUpdateAttackPreCD = fightCreatureData.creatureData.GetAttackCDTime();
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
            ChangeIntent(AIIntentEnum.DefCreatureIdle);
            return;
        }
        //如果自己死了
        if (selfAIEntity.selfCreatureEntity == null || selfAIEntity.selfCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureDead);
            return;
        }
        var selfCreatureInfo = fightCreatureData.creatureData.creatureInfo;
        string animNameAppointAttack = selfCreatureInfo.anim_attack;
        string animNameAppointIdle = selfCreatureInfo.anim_idle;
        //播放攻击动画
        if (selfCreatureInfo.anim_attack_loop == 1)//如果是循环 只播放攻击动画
        {
            selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, true, animNameAppoint: animNameAppointAttack);
        }
        else//如果不是循环，先播放打击再播放待机
        {
            selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false, animNameAppoint: animNameAppointAttack);
            selfAIEntity.selfCreatureEntity.AddAnim(0, SpineAnimationStateEnum.Idle, true, 1, animNameAppoint: animNameAppointIdle);
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
        var findTargetCreature = selfAIEntity.FindCreatureEntityForDis(Vector3.right);
        //如果没有找到最近的生物
        if(findTargetCreature == null)
        {
            ChangeIntent(AIIntentEnum.DefCreatureIdle);
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
