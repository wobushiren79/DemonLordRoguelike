using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureIdle : AIBaseIntent
{
    AIDefCreatureEntity selfEntity;
    public float timeUpdateForFindTarget = 0;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfEntity = aiEntity as AIDefCreatureEntity;
        timeUpdateForFindTarget = 0;
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > 0.25f)
        {
            timeUpdateForFindTarget = 0;
            selfEntity.targetAttCreatureEntity = null;
            selfEntity.targetAttCreatureEntity = selfEntity.FindAttCreatureDisMinEntity(selfEntity.selfDefCreatureEntity.fightCreatureData.positionCreate.z);
            if (selfEntity.targetAttCreatureEntity != null)
            {
                ChangeIntent(AIIntentEnum.DefCreatureAttack);
            }
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
