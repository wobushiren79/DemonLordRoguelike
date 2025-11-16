using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefenseCoreCreatureIdle : AIBaseIntent
{
    public AIDefenseCoreCreatureEntity selfAIEntity;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCoreCreatureEntity;
        //设置动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {

    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
