using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttackCreatureMove : AIBaseIntent
{
    //目标AI
    public AIAttackCreatureEntity selfAIEntity;
    public FightCreatureBean fightCreatureData;
    public float timeUpdateForFindTarget = 0;
    public float timeUpdateForFindTargetCD = 0;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        //这里的攻击检测时间可能过长 后续考虑可以减少
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        //第一次进来检测一次攻击
        timeUpdateForFindTarget = timeUpdateForFindTargetCD;
        //设置移动动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Walk, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //查询敌人
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget >= timeUpdateForFindTargetCD)
        {
            timeUpdateForFindTarget = 0;
            timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
            var findTargetCreature = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);
            if (findTargetCreature != null)
            {
                selfAIEntity.targetCreatureEntity = findTargetCreature;
                selfAIEntity.targetMovePos = selfAIEntity.targetCreatureEntity.creatureObj.transform.position;
                selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureAttack);
                return;
            }
            else
            {
                var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                selfAIEntity.targetCreatureEntity = gameFightLogic.fightData.fightDefenseCoreCreature;
            }
        }

        //如果目标已经死了
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
        {
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            return;
        }

        float moveSpeed = selfAIEntity.selfCreatureEntity.fightCreatureData.GetMSPD();
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        
        //如果目标是魔王
        if (selfAIEntity.targetCreatureEntity.fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightDefenseCore)
        {
            //首先检测是否到达路径终点 魔王在位置x0 第一排在x1 这里取0.5
            if (selfTF.position.x <= 0.5f)
            {
                //检测是否靠近目标
                if (CheckIsCloseTarget())
                {
                    selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureAttack);
                    return;
                }
            }
            else
            {
                selfTF.Translate(Vector3.Normalize(new Vector3(0, 0, selfAIEntity.selfCreatureEntity.fightCreatureData.roadIndex) - selfTF.transform.position) * Time.deltaTime * moveSpeed);
                return;
            }
        }
         selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeed);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        timeUpdateForFindTargetCD = 0.2f;
    }

    /// <summary>
    /// 检测是否靠近了目标
    /// </summary>
    /// <returns></returns>
    public bool CheckIsCloseTarget()
    {
        var currentPosition = selfAIEntity.selfCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        if (dis <= 0.05f)
        {
            return true;
        }
        return false;
    }
}
