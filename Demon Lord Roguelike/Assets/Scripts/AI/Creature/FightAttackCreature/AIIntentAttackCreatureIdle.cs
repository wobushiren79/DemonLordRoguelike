using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

public class AIIntentAttackCreatureIdle : AIBaseIntent
{
    //目标AI
    public AIAttackCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        //寻找路线上的敌人（走进道路范围内才索敌：未进道路时跳过，目标直接锁定魔王核心沿本道路直行）
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        float roadMaxX = 0.5f + gameFightLogic.fightData.sceneRoadLength;
        if (selfAIEntity.selfCreatureEntity.creatureObj.transform.position.x <= roadMaxX)
        {
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);
        }

        //触发待机动作
        selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Left);

        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);

        //如果没有数据 说明这条路上没有防守生物，则目标设置为魔王
        if (selfAIEntity.targetCreatureEntity == null)
        {
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
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureMove);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
