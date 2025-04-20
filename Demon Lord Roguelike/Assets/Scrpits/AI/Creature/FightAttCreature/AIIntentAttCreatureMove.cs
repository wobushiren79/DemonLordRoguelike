using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureMove : AIBaseIntent
{
    //目标AI
    public AIAttCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //设置移动动作
        string animNameAppoint = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_walk;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Walk, true, animNameAppoint:animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //查询敌人
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > 0.25f)
        {
            timeUpdateForFindTarget = 0;
            var findTargetCreature = selfAIEntity.FindCreatureEntityForDis(Vector3.left, CreatureTypeEnum.FightDefense);
            if (findTargetCreature != null)
            {
                selfAIEntity.targetCreatureEntity = findTargetCreature;
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureAttack);
                return;
            }
        }

        //如果目标是魔王
        if (selfAIEntity.targetMovePos.x <= 0)
        {
            //检测是否靠近目标
            if (CheckIsCloseTarget())
            {
                selfAIEntity.selfCreatureEntity.SetCreatureDead();
                return;
            }
        }
        //如果不是魔王
        else
        {
            //如果目标已经死了
            if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureIdle);
                return;
            }
        }

        float moveSpeed = selfAIEntity.selfCreatureEntity.fightCreatureData.GetMoveSpeed();
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeed);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
    }

    /// <summary>
    /// 检测是否靠近了目标
    /// </summary>
    /// <returns></returns>
    public bool CheckIsCloseTarget()
    {
        var currentPosition = selfAIEntity.selfCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        if (dis <= 0.05f)
        {
            return true;
        }
        return false;
    }
}
