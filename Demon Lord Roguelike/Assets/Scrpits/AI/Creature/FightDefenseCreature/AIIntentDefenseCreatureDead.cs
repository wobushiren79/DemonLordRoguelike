using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefenseCreatureDead : AIIntentCreatureDead
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        creatureFightType = CreatureFightTypeEnum.FightDefense;
        base.IntentEntering(aiEntity);
    }

}
