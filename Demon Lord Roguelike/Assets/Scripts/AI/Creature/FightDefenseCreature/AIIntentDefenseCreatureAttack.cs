using UnityEngine;

public class AIIntentDefenseCreatureAttack : AIIntentCreatureAttack
{

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        intentForIdle = AIIntentEnum.DefenseCreatureIdle;
        intentForDead = AIIntentEnum.DefenseCreatureDead;
        base.IntentEntering(aiEntity);
    }

}
