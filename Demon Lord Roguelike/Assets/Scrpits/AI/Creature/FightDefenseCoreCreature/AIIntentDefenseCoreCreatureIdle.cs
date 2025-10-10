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
        string animNameAppoint = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint : animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {

    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
