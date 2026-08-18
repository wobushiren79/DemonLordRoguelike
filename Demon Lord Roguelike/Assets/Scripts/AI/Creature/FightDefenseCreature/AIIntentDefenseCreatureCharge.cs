using UnityEngine;

/// <summary>
/// 防守生物冲锋意图（charge_attack=1 冲锋自爆型生物专用，如哥布林敢死队）：
/// 放卡后立即向前(+X)冲锋，冲锋开始即释放原占位格(isPositionReleased=true，原格可立刻放第二只魔物)；
/// 前方 attack_search_range 内发现敌人、或冲到道路尽头时直接 SetCreatureDead——爆炸统一走
/// 「死亡即引爆」路径（FightCreatureEntity.SetCreatureDeadForDefense 按 charge_attack 配置在死亡位置创建爆炸攻击模块），
/// 被打死时也走同一入口，SetCreatureDead 的 IsDead 幂等守卫保证只爆一次。
/// </summary>
public class AIIntentDefenseCreatureCharge : AIBaseIntent
{
    #region 字段
    /// <summary>冲锋速度倍率（在MSPD换算速度基础上再乘，10点MSPD≈1格/秒）</summary>
    public const float ChargeSpeedRate = 5f;
    /// <summary>目标AI</summary>
    public AIDefenseCreatureEntity selfAIEntity;
    /// <summary>战斗生物数据</summary>
    public FightCreatureBean fightCreatureData;
    /// <summary>索敌已计时（秒）</summary>
    public float timeUpdateForFindTarget = 0;
    /// <summary>索敌节奏（秒，由 attack_search_time 换算，冲锋生物建议配0.1保证触发粒度）</summary>
    public float timeUpdateForFindTargetCD = 0.1f;
    /// <summary>道路尽头X（>=即引爆），进入时缓存（与击退/冲击波同口径：0.5+路长）</summary>
    public float roadEndPosX;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入冲锋意图：释放原占位格、缓存道路尽头、朝右播移动动画，并立即安排首次索敌
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        //冲锋开始即释放原占位格（占位/删除扫描按此标记跳过本生物）
        fightCreatureData.isPositionReleased = true;
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        roadEndPosX = 0.5f + gameFightLogic.fightData.sceneRoadLength;
        //朝右（进攻方来向）并播移动动画（动画速度=冲锋倍率，与冲锋移速同步）
        selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Right);
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Walk, true, animSpeed: ChargeSpeedRate);
        //进来立即安排一次索敌（贴脸放卡即爆）
        timeUpdateForFindTarget = timeUpdateForFindTargetCD;
    }

    /// <summary>
    /// 每帧更新：按索敌节奏检测前方敌人（发现即死亡引爆），到路尽头同样引爆，否则持续前进
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //战斗帧时间（跟随游戏速度，2倍速时移动与索敌节奏同步翻倍）
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        //按索敌节奏检测前方敌人（生物Ray索敌，range=attack_search_range 即前方触发距离）
        timeUpdateForFindTarget += deltaTime;
        if (timeUpdateForFindTarget >= timeUpdateForFindTargetCD)
        {
            timeUpdateForFindTarget = 0;
            var findTargetCreature = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Right);
            if (findTargetCreature != null)
            {
                selfAIEntity.selfCreatureEntity.SetCreatureDead();
                return;
            }
        }
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        //冲到道路尽头仍无敌：直接死亡引爆
        if (selfTF.position.x >= roadEndPosX)
        {
            selfAIEntity.selfCreatureEntity.SetCreatureDead();
            return;
        }
        //前进（MSPD换算 × 冲锋倍率）
        float moveSpeed = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.MSPD);
        float moveSpeedFinal = MathUtil.InterpolationLerp(moveSpeed, 0, 100, 0, 2f) * ChargeSpeedRate;
        selfTF.Translate(Vector3.right * deltaTime * moveSpeedFinal);
    }

    /// <summary>
    /// 离开冲锋意图：重置计时
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        timeUpdateForFindTargetCD = 0.1f;
    }
    #endregion
}
