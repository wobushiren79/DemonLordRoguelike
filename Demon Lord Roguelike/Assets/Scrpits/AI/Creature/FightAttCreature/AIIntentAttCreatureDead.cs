using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureDead : AIBaseIntent
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        var targetAttCreatureEntity = aiEntity as AIAttCreatureEntity;
        CreatureHandler.Instance.RemoveCreatureEntity(targetAttCreatureEntity.selfAttCreatureEntity, CreatureTypeEnum.FightAtt);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {

    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
