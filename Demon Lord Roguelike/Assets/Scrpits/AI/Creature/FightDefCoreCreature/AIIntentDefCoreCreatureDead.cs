using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCoreCreatureDead : AIBaseIntent
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        var targetDefCreatureEntity = aiEntity as AIDefCreatureEntity;
        CreatureHandler.Instance.RemoveCreatureEntity(targetDefCreatureEntity.selfCreatureEntity, CreatureTypeEnum.FightDefenseCore);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {

    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
