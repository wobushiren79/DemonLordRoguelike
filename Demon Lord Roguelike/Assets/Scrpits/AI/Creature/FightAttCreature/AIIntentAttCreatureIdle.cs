using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureIdle : AIBaseIntent
{
    //目标AI
    public AIAttCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //寻找一条路线上的敌人
        selfAIEntity.targetDefCreatureEntity = null;
        int selfRoad = selfAIEntity.selfAttCreatureEntity.fightCreatureData.positionCreate.z;
        selfAIEntity.targetDefCreatureEntity = selfAIEntity.FindDefCreatureDisMinEntity(selfRoad);

        //触发待机动作
        selfAIEntity.selfAttCreatureEntity.SetFaceDirection(Direction2DEnum.Left);
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);

        //如果没有数据 说明这条路上没有防守生物，则直接前往路的尽头
        if (selfAIEntity.targetDefCreatureEntity == null)
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            selfAIEntity.targetDefCreatureEntity = gameFightLogic.fightData.fightDefCoreCreature;
            selfAIEntity.targetMovePos = new Vector3(0, 0, selfRoad);
        }
        else
        {
            selfAIEntity.targetMovePos = selfAIEntity.targetDefCreatureEntity.creatureObj.transform.position;
        }
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureMove);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
