using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureIdle : AIBaseIntent
{
    //Ŀ��AI
    public AIAttCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //Ѱ��һ��·���ϵĵ���
        selfAIEntity.targetDefCreatureEntity = null;
        int selfRoad = selfAIEntity.selfAttCreatureEntity.fightCreatureData.positionCreate.z;
        selfAIEntity.targetDefCreatureEntity = selfAIEntity.FindDefCreatureDisMinEntity(selfRoad);

        //������������
        selfAIEntity.selfAttCreatureEntity.SetFaceDirection(Direction2DEnum.Left);
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);

        //���û������ ˵������·��û�з��������ֱ��ǰ��·�ľ�ͷ
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
