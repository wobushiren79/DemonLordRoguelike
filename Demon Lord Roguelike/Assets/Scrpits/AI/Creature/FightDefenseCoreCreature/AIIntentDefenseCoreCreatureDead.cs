using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefenseCoreCreatureDead : AIIntentCreatureDead
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCoreCreatureEntity;
        creatureFightType = CreatureFightTypeEnum.FightDefenseCore;
        base.IntentEntering(selfAIEntity);
    }
}
