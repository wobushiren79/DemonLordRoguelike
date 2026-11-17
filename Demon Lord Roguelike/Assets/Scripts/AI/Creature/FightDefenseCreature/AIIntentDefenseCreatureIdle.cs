using UnityEngine;

/// <summary>
/// 防守生物待机并索敌。防守召唤物在没有目标时沿本道路前进。
/// </summary>
public class AIIntentDefenseCreatureIdle : AIBaseIntent
{
    #region 字段
    AIDefenseCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;
    public float timeUpdateForFindTargetCD = 0.2f;
    FightCreatureBean fightCreatureData;
    /// <summary>是否允许搜索身后敌人（配置门控，IntentEntering 缓存，避免每次搜索重复读配置）</summary>
    private bool isAttackSearchBack = false;
    /// <summary>召唤物的道路右边界。</summary>
    private float summonedRoadEndX;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 恢复正面朝向。召唤物立即索敌并播放行走动画。
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateForFindTarget = 0;
        //初始化相关数据
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        //缓存身后搜索开关
        isAttackSearchBack = fightCreatureData.creatureData.creatureInfo.IsAttackSearchBack();
        //回到待机即恢复朝正面(右)：上一轮若曾转身打身后，此处转回来（SetFaceDirection 已对朝向未变去重）
        bool isSummoned = fightCreatureData.isSummoned;
        if (isAttackSearchBack || isSummoned)
        {
            selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Right);
        }
        if (isSummoned)
        {
            var fightData = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>().fightData;
            summonedRoadEndX = 0.5f + fightData.sceneRoadLength;
            timeUpdateForFindTarget = timeUpdateForFindTargetCD;
        }
        selfAIEntity.selfCreatureEntity.PlayAnim(isSummoned ? SpineAnimationStateEnum.Walk : SpineAnimationStateEnum.Idle, true);
    }

    /// <summary>
    /// 按职业配置索敌并切换至原有战斗意图。召唤物找不到目标时向右前进。
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        bool isSummoned = fightCreatureData.isSummoned;
        var creature = selfAIEntity.selfCreatureEntity;
        if (isSummoned && deltaTime <= 0f)
            return;

        bool hasReachedRoadEnd = isSummoned && creature.creatureObj.transform.position.x >= summonedRoadEndX;
        timeUpdateForFindTarget += deltaTime;
        if (timeUpdateForFindTarget > timeUpdateForFindTargetCD || hasReachedRoadEnd)
        {
            timeUpdateForFindTarget = 0;
            timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
            //搜索目标（正面优先，正面无目标且开启身后搜索时才向背后补搜）
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSingeFrontThenBack(DirectionEnum.Right, isAttackSearchBack);
            if (selfAIEntity.targetCreatureEntity != null)
            {
                //如果攻击模式是防守则进入防守状态
                if (fightCreatureData.creatureData.creatureInfo.attack_mode == 0)
                {
                    ChangeIntent(AIIntentEnum.DefenseCreatureDefend);
                }
                //其他情况进入攻击状态
                else
                {
                    ChangeIntent(AIIntentEnum.DefenseCreatureAttack);
                }
                return;
            }
        }

        if (hasReachedRoadEnd)
        {
            creature.SetCreatureDead();
        }
        else if (isSummoned)
        {
            var creatureTransform = creature.creatureObj.transform;
            var position = creatureTransform.position;
            float moveSpeed = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.MSPD);
            float moveSpeedFinal = MathUtil.InterpolationLerp(moveSpeed, 0, 100, 0, 2f);
            position.x = Mathf.Min(summonedRoadEndX, position.x + moveSpeedFinal * deltaTime);
            creatureTransform.position = position;
        }
    }

    /// <summary>
    /// 离开时复位索敌计时。
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        timeUpdateForFindTargetCD = 0.2f;
    }
    #endregion
}
