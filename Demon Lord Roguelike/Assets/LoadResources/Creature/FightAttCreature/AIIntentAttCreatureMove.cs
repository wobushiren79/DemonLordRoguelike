using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureMove : AIBaseIntent
{
    //目标AI
    public AIAttCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //设置移动动作
        string animNameAppoint = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_walk;
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Walk, true, animNameAppoint:animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //如果目标是魔王
        if (selfAIEntity.targetMovePos.x <= 0)
        {
            //检测是否靠近目标
            if (CheckIsCloseTarget())
            {
                selfAIEntity.selfAttCreatureEntity.SetCreatureDead();
                return;
            }
        }
        //如果不是魔王
        else
        {
            //如果目标已经死了
            if (selfAIEntity.targetDefCreatureEntity == null || selfAIEntity.targetDefCreatureEntity.IsDead())
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureIdle);
                return;
            }
            //监测是否在攻击范围内
            if (CheckIsAttRange())
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureAttack);
                return;
            }
        }

        float moveSpeed = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetMoveSpeed();
        Transform selfTF = selfAIEntity.selfAttCreatureEntity.creatureObj.transform;
        selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeed);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

    /// <summary>
    /// 检测是否在攻击范围内
    /// </summary>
    public bool CheckIsAttRange()
    {
        var currentPosition = selfAIEntity.selfAttCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.creatureInfo;
        if (dis <= creatureInfo.attack_range)
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 检测是否靠近了目标
    /// </summary>
    /// <returns></returns>
    public bool CheckIsCloseTarget()
    {
        var currentPosition = selfAIEntity.selfAttCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        if (dis <= 0.05f)
        {
            return true;
        }
        return false;
    }
}
