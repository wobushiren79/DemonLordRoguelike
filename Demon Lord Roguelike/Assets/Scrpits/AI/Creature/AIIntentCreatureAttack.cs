using UnityEngine;

public class AIIntentCreatureAttack : AIBaseIntent
{
    //攻击准备时间
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttackPreCD = 0.2f;
    public float timeUpdateAttacking = 0;
    public float timeUpdateAttackingCD = 0.2f;
    //当前AIEntity
    public AICreatureEntity selfAIEntity;
    //战斗生物数据
    public FightCreatureBean fightCreatureData;
    //攻击状态 0准备中 1攻击中
    public int attackState = 0;
    //待机意图
    public AIIntentEnum intentForIdle;
    //死亡意图
    public AIIntentEnum intentForDead;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        attackState = 0;
        RefreshData();
        // //设置待机动作
        // selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true); 
        //刚进来立即开始一次攻击
        timeUpdateAttackPre = timeUpdateAttackPreCD;
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    public void RefreshData()
    {
        fightCreatureData.GetAttackTimeData(out float timeAttackPre,out float timeAttacking);
        timeUpdateAttackPreCD = timeAttackPre;
        timeUpdateAttackingCD = timeAttacking;
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
                RefreshData();
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
                RefreshData();
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
    /// 立即执行一次攻击
    /// </summary>
    public virtual void AttackImm()
    {
        timeUpdateAttackPre = timeUpdateAttackPreCD;
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
            ChangeIntent(intentForIdle);
            return;
        }
        //如果自己死了
        if (selfAIEntity.selfCreatureEntity == null || selfAIEntity.selfCreatureEntity.IsDead())
        {
            ChangeIntent(intentForDead);
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
        DirectionEnum findDirectrion;
        if (attackMode.attackModeData.attackDirection.x > 0)
        {
            findDirectrion= DirectionEnum.Right;
        }
        else
        {
            findDirectrion= DirectionEnum.Left;
        }
        var findTargetCreature = selfAIEntity.FindCreatureEntityForSinge(findDirectrion);
        //如果没有找到最近的生物
        if (findTargetCreature == null)
        {
            ChangeIntent(intentForIdle);
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
            ChangeIntent(intentForIdle);
            return;
        }
        //继续攻击
        else
        {
            attackState = 0;
        }
    }
}
