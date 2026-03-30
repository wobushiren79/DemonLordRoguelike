using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttackCreatureDead : AIIntentCreatureDead
{    
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        creatureFightType = CreatureFightTypeEnum.FightAttack;
        base.IntentEntering(aiEntity);
    }

}
