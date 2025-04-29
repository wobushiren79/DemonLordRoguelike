using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureDead : AIBaseIntent
{
    public float timeUpdateForDead = 0f;
    public float timeUpdateForDeadCD = 1.1f;

    //目标AI
    public AIDefCreatureEntity selfAIEntity;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForDead = 0;
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        string animNameAppoint = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_dead;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Dead, false, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForDead += Time.deltaTime;
        if (timeUpdateForDead >= timeUpdateForDeadCD)
        {
            timeUpdateForDead = 0;
            CreatureHandler.Instance.RemoveCreatureEntity(selfAIEntity.selfCreatureEntity, CreatureTypeEnum.FightDefense);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
