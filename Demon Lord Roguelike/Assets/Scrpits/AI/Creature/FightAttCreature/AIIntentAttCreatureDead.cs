using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureDead : AIBaseIntent
{
    public float timeUpdateForDead = 0f;
    public float timeForDeadTime = 1.1f;
    //Ä¿±êAI
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
            CreatureHandler.Instance.RemoveCreatureEntity(selfAIEntity.selfAttCreatureEntity, CreatureTypeEnum.FightAtt);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
