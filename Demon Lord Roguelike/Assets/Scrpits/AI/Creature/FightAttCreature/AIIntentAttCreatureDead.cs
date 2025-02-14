using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureDead : AIBaseIntent
{
    public float timeUpdateForDead = 0f;
    public float timeForDeadTime = 1.1f;
    //Ŀ��AI
    public AIAttCreatureEntity selfAIEntity;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForDead = 0;
        selfAIEntity = aiEntity as AIAttCreatureEntity;

        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Dead, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForDead += Time.deltaTime;
        if (timeUpdateForDead >= timeForDeadTime)
        {
            timeUpdateForDead = 0;
            var selfFightCreatureData = selfAIEntity.selfAttCreatureEntity.fightCreatureData;
            CreatureHandler.Instance.RemoveCreatureEntity(selfAIEntity.selfAttCreatureEntity, CreatureTypeEnum.FightAtt);
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadEnd, selfFightCreatureData);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
