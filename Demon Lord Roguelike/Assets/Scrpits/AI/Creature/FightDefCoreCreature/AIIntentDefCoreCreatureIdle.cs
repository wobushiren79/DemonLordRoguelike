using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCoreCreatureIdle : AIBaseIntent
{
    public AIDefCoreCreatureEntity selfAIEntity;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefCoreCreatureEntity;
        //�����ƶ�����
        selfAIEntity.selfDefCoreCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {

    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
