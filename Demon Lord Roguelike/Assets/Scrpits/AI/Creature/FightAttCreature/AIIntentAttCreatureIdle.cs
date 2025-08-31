using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

public class AIIntentAttCreatureIdle : AIBaseIntent
{
    //目标AI
    public AIAttCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //寻找路线上的敌人
        var fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);

        //触发待机动作
        selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Left);

        string animNameAppoint = fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);

        //如果没有数据 说明这条路上没有防守生物，则目标设置为魔王
        if (selfAIEntity.targetCreatureEntity == null)
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var fightDefenseCoreCreature = gameFightLogic.fightData.fightDefenseCoreCreature;
            if (fightDefenseCoreCreature != null && fightDefenseCoreCreature.IsDead())
            {
                selfAIEntity.targetCreatureEntity = null;
                return;
            }
            selfAIEntity.targetCreatureEntity = gameFightLogic.fightData.fightDefenseCoreCreature;
        }
        if (selfAIEntity.targetCreatureEntity != null)
        {
            selfAIEntity.targetMovePos = selfAIEntity.targetCreatureEntity.creatureObj.transform.position;
        }
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        if (selfAIEntity.targetCreatureEntity != null)
        {  
            selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureMove);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
