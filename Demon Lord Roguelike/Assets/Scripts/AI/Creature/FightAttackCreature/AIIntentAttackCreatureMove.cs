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
    /// <summary>攻击起始X阈值：自身x大于此值(出生线附近)时不进入攻击意图，继续前进</summary>
    public float attackEnablePosX = 10.5f;
    /// <summary>攻击魔王的靠近距离阈值：与魔王距离小于此值时固定触发一次攻击并让魔王死亡</summary>
    public const float CloseCoreDistance = 0.25f;

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
                //出生线附近(x>attackEnablePosX)不进入攻击意图, 继续前进直到越过该位置再攻击
                if (selfAIEntity.selfCreatureEntity.creatureObj.transform.position.x <= attackEnablePosX)
                {
                    selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureAttack);
                    return;
                }
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

        float moveSpeed = selfAIEntity.selfCreatureEntity.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.MSPD);   
        float moveSpeedFinal = MathUtil.InterpolationLerp(moveSpeed, 0, 100, 0, 2f);

        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        
        //如果目标是魔王(防守核心)
        if (selfAIEntity.targetCreatureEntity.fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightDefenseCore)
        {
            //魔王固定不动 始终以其当前位置作为移动/靠近判定目标
            selfAIEntity.targetMovePos = selfAIEntity.targetCreatureEntity.creatureObj.transform.position;
            //完全靠近魔王(距离<CloseCoreDistance)时切攻击魔王意图: 固定触发一次攻击并让魔王出血死亡, 不走AttackMode
            if (CheckIsCloseTarget(CloseCoreDistance))
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureAttackCore);
                return;
            }
            //未靠近: x>0.5先沿本路径推进到终点, 否则直冲魔王位置
            if (selfTF.position.x > 0.5f)
            {
                selfTF.Translate(Vector3.Normalize(new Vector3(0, 0, selfAIEntity.selfCreatureEntity.fightCreatureData.roadIndex) - selfTF.transform.position) * Time.deltaTime * moveSpeedFinal);
            }
            else
            {
                selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeedFinal);
            }
            return;
        }

        selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeedFinal);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        timeUpdateForFindTargetCD = 0.2f;
    }

    /// <summary>
    /// 检测是否靠近了目标(与 targetMovePos 的距离不大于 closeDistance)
    /// </summary>
    /// <param name="closeDistance">靠近判定的距离阈值(默认0.05)</param>
    /// <returns></returns>
    public bool CheckIsCloseTarget(float closeDistance = 0.05f)
    {
        var currentPosition = selfAIEntity.selfCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        if (dis <= closeDistance)
        {
            return true;
        }
        return false;
    }
}
