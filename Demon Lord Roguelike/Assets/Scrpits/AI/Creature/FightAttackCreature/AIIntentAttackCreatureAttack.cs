using UnityEngine;

public class AIIntentAttackCreatureAttack : AIIntentCreatureAttack
{

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        intentForIdle = AIIntentEnum.AttackCreatureIdle;
        intentForDead = AIIntentEnum.AttackCreatureDead;
        base.IntentEntering(aiEntity);
    }

}
