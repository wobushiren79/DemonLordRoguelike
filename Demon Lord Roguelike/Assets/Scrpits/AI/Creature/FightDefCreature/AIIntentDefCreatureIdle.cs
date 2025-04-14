using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureIdle : AIBaseIntent
{
    AIDefCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        timeUpdateForFindTarget = 0;

        string animNameAppoint = selfAIEntity.selfDefCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > 0.25f)
        {
            timeUpdateForFindTarget = 0;
            selfAIEntity.targetAttCreatureEntity = null;
            selfAIEntity.targetAttCreatureEntity = selfAIEntity.FindAttCreatureDisMinEntity(selfAIEntity.selfDefCreatureEntity.fightCreatureData.positionCreate.z);
            if (selfAIEntity.targetAttCreatureEntity != null)
            {
                ChangeIntent(AIIntentEnum.DefCreatureAttack);
            }
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
