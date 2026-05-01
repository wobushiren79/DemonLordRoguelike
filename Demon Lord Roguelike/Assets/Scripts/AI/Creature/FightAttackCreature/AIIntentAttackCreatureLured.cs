using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureLured : AIBaseIntent
{
    //目标AI
    public AIAttackCreatureEntity selfAIEntity;
    public FightCreatureEntity selfFightCreatureEntity;
    public FightCreatureBean fightCreatureData;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        selfFightCreatureEntity = selfAIEntity.selfCreatureEntity;
        fightCreatureData = selfFightCreatureEntity.fightCreatureData;
        //设置移动目标
        Transform selfTF = selfFightCreatureEntity.creatureObj.transform;
        selfAIEntity.targetMovePos = new Vector3(selfTF.position.x, selfTF.position.y, fightCreatureData.roadIndex);
        //设置移动动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Walk, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //如果目标已经死了
        if (CheckIsCloseTarget())
        {
            selfFightCreatureEntity.creatureObj.transform.position = selfAIEntity.targetMovePos;
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            return;
        }
        float moveSpeed = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.MSPD);
        float moveSpeedFinal = MathUtil.InterpolationLerp(moveSpeed, 0, 100, 0, 10f);

        Transform selfTF = selfFightCreatureEntity.creatureObj.transform;
        selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeedFinal);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

    /// <summary>
    /// 检测是否靠近了目标
    /// </summary>
    /// <returns></returns>
    public bool CheckIsCloseTarget()
    {
        var currentPosition = selfFightCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        if (dis <= 0.01f)
        {
            return true;
        }
        return false;
    }
}
