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

    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //监测是否在攻击范围内
        if (CheckIsAttRange())
        {
            selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureAttack);
            return;
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
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetCreatureInfo();
        if (dis <= creatureInfo.att_range)
        {
            return true;
        }
        return false;
    }
}
