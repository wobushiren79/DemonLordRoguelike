using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureIdle : AIBaseIntent
{
    //目标AI
    public AIAttCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //寻找一条路线上的敌人
        selfAIEntity.FindDefCreatureDisMinEntity(selfAIEntity.selfAttCreatureEntity.fightCreatureData.positionZCurrent);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureMove);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
