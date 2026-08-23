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
    /// <summary>道路右缘X(=0.5+路长，与击退意图 roadMaxX 同口径)：自身x大于此值说明还没走进道路，不索敌、沿本道路直行</summary>
    public float roadMaxX = 10.5f;
    /// <summary>攻击魔王的靠近距离阈值：与魔王距离小于此值时固定触发一次攻击并让魔王死亡</summary>
    public const float CloseCoreDistance = 0.25f;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        //按当场路长缓存道路右缘（征服模式路长随机，不能写死）
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic?.fightData != null)
        {
            roadMaxX = 0.5f + gameFightLogic.fightData.sceneRoadLength;
        }
        //这里的攻击检测时间可能过长 后续考虑可以减少
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        //第一次进来检测一次攻击
        timeUpdateForFindTarget = timeUpdateForFindTargetCD;
        //设置移动动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Walk, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //战斗帧时间（跟随游戏速度，2倍速时移动与索敌节奏同步翻倍）
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        //走进道路范围内才开始索敌：未进道路(x>道路右缘)时目标锁定魔王核心，沿本道路直行
        if (selfTF.position.x > roadMaxX)
        {
            //目标已是魔王核心则跳过，避免每帧重复查询赋值
            if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.fightCreatureData.creatureFightType != CreatureFightTypeEnum.FightDefenseCore)
            {
                var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                selfAIEntity.targetCreatureEntity = gameFightLogic.fightData.fightDefenseCoreCreature;
            }
        }
        else
        {
            //查询敌人
            timeUpdateForFindTarget += deltaTime;
            if (timeUpdateForFindTarget >= timeUpdateForFindTargetCD)
            {
                timeUpdateForFindTarget = 0;
                timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
                var findTargetCreature = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);
                if (findTargetCreature != null)
                {
                    selfAIEntity.targetCreatureEntity = findTargetCreature;
                    selfAIEntity.targetMovePos = selfAIEntity.targetCreatureEntity.creatureObj.transform.position;
                    //走进道路后索到目标即进入攻击意图
                    selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureAttack);
                    return;
                }
                else
                {
                    var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                    selfAIEntity.targetCreatureEntity = gameFightLogic.fightData.fightDefenseCoreCreature;
                }
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
                selfTF.Translate(Vector3.Normalize(new Vector3(0, 0, selfAIEntity.selfCreatureEntity.fightCreatureData.roadIndex) - selfTF.transform.position) * deltaTime * moveSpeedFinal);
            }
            else
            {
                selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * deltaTime * moveSpeedFinal);
            }
            return;
        }

        selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * deltaTime * moveSpeedFinal);
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
