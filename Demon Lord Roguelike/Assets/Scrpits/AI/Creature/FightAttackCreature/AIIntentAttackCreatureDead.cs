using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttackCreatureDead : AIBaseIntent
{
    public float timeUpdateForDead = 0f;
    public float timeUpdateForDeadCD = 1.1f;
    
    public AIAttackCreatureEntity selfAIEntity;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForDead = 0;
        selfAIEntity = aiEntity as AIAttackCreatureEntity;

        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Dead, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForDead += Time.deltaTime;
        if (timeUpdateForDead >= timeUpdateForDeadCD)
        {
            timeUpdateForDead = 0;
            var selfFightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
            CreatureHandler.Instance.RemoveFightCreatureEntity(selfAIEntity.selfCreatureEntity, CreatureTypeEnum.FightAttack);
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadEnd, selfFightCreatureData);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
