using UnityEngine;

/// <summary>
/// 防守生物攻击意图：在通用攻击意图基础上，支持"正面优先、正面无目标时转身攻击身后敌人"。
/// 是否开启身后搜索由配置 CreatureInfo.attack_search_back 门控（IsAttackSearchBack）。
/// <para>人类弓箭手（creatureId=1002）额外启用指定目标保靶：目标仍命中搜索时持续攻击，失效后回 Idle 重搜。</para>
/// </summary>
public class AIIntentDefenseCreatureAttack : AIIntentCreatureAttack
{
    #region 字段
    /// <summary>持续保靶的生物配置ID（当前仅人类弓箭手）</summary>
    private const long LOCK_TARGET_CREATURE_ID = 1002;
    /// <summary>是否允许搜索并转身攻击身后敌人（IntentEntering 缓存，避免每次攻击循环重复读配置）</summary>
    private bool isAttackSearchBack = false;
    /// <summary>当前生物是否启用指定目标持续保靶</summary>
    private bool isLockTargetCreature = false;
    /// <summary>进入攻击意图时的目标 UUID 快照，防止实体池复用误判</summary>
    private string lockedTargetUUId = null;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入防守攻击意图：指定待机/死亡回退意图，并缓存身后搜索开关
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        intentForIdle = AIIntentEnum.DefenseCreatureIdle;
        intentForDead = AIIntentEnum.DefenseCreatureDead;
        //缓存身后搜索开关（配置门控，仅骷髅战士等配置为1的生物开启）
        isAttackSearchBack = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.IsAttackSearchBack();
        isLockTargetCreature = false;
        lockedTargetUUId = null;
        base.IntentEntering(aiEntity);
        //base 初始化本轮战斗数据后再建立锁靶身份，AI/实体池复用时不会继承上次目标
        isLockTargetCreature = fightCreatureData.creatureData.creatureId == LOCK_TARGET_CREATURE_ID;
        if (isLockTargetCreature)
        {
            lockedTargetUUId = selfAIEntity.targetCreatureEntity?.fightCreatureData?.creatureData?.creatureUUId;
        }
    }

    /// <summary>
    /// 离开攻击意图：保留基类连发/额外攻击清理，并清除人类弓箭手的保靶运行时数据。
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        base.IntentLeaving(aiEntity);
        if (isLockTargetCreature && selfAIEntity != null)
        {
            selfAIEntity.targetCreatureEntity = null;
        }
        isLockTargetCreature = false;
        lockedTargetUUId = null;
    }
    #endregion

    #region 攻击流程覆盖
    /// <summary>
    /// 重新搜索目标：人类弓箭手保留仍命中搜索的原目标，其他防守生物维持正面优先、可选背后搜索。
    /// </summary>
    protected override FightCreatureEntity FindNextTarget(BaseAttackMode attackMode)
    {
        if (isLockTargetCreature)
        {
            return CheckLockedTargetInSearchResult() ? selfAIEntity.targetCreatureEntity : null;
        }
        return selfAIEntity.FindCreatureEntityForSingeFrontThenBack(DirectionEnum.Right, isAttackSearchBack);
    }

    /// <summary>
    /// 复查攻击目标：人类弓箭手检查指定目标是否仍命中搜索，不能用同方向存在任意敌人代替。
    /// </summary>
    protected override bool CheckTargetInAttackRange()
    {
        if (!isLockTargetCreature)
            return base.CheckTargetInAttackRange();
        var selfCreature = selfAIEntity?.selfCreatureEntity;
        var targetCreature = selfAIEntity?.targetCreatureEntity;
        //与基类兜底一致：自身已回收或目标为空时交原攻击流程处理，避免切 Idle 引入空引用
        if (selfCreature == null || selfCreature.creatureObj == null || targetCreature == null || targetCreature.creatureObj == null)
            return true;
        return CheckLockedTargetInSearchResult();
    }

    /// <summary>
    /// 发射连发：人类弓箭手仅在未经过当帧目标复查的 attackState==2 窗口补做完整搜索，避免重复遍历。
    /// </summary>
    protected override void FireExtraShot()
    {
        if (!isLockTargetCreature)
        {
            base.FireExtraShot();
            return;
        }
        var selfCreature = selfAIEntity?.selfCreatureEntity;
        if (selfCreature == null || selfCreature.IsDead())
        {
            extraShotsLeft = 0;
            return;
        }
        bool isTargetValid = attackState == 2 ? CheckLockedTargetInSearchResult() : IsLockedTargetIdentityValid();
        if (!isTargetValid)
        {
            extraShotsLeft = 0;
            return;
        }
        FightHandler.Instance.StartCreateAttackMode(selfCreature, selfAIEntity.targetCreatureEntity, null);
    }

    /// <summary>
    /// 按目标相对自身的位置转身：目标在右→朝右，目标在左（身后）→朝左；不开启身后搜索时无需转身
    /// </summary>
    protected override void RefreshFaceForTarget()
    {
        if (!isAttackSearchBack)
            return;
        var targetEntity = selfAIEntity.targetCreatureEntity;
        if (targetEntity == null || targetEntity.creatureObj == null)
            return;
        float selfX = selfAIEntity.selfCreatureEntity.creatureObj.transform.position.x;
        float targetX = targetEntity.creatureObj.transform.position.x;
        //目标在自身右侧(含同x)→朝右迎正面来敌；在左侧→转身朝背后。SetFaceDirection 内部已对朝向未变做去重跳过
        Direction2DEnum faceDirection = targetX >= selfX ? Direction2DEnum.Right : Direction2DEnum.Left;
        selfAIEntity.selfCreatureEntity.SetFaceDirection(faceDirection);
    }
    #endregion

    #region 人类弓箭手保靶
    /// <summary>
    /// 检查锁定目标身份未变且仍命中正前方搜索，不在攻击意图内重复道路或坐标规则。
    /// </summary>
    private bool CheckLockedTargetInSearchResult()
    {
        return IsLockedTargetIdentityValid()
            && selfAIEntity.CheckCreatureEntityInSearchResult(DirectionEnum.Right, selfAIEntity.targetCreatureEntity, lockedTargetUUId);
    }

    /// <summary>
    /// 执行 O(1) 锁定身份检查：目标存活、表现对象存在且 UUID 与进入攻击意图时的快照一致。
    /// </summary>
    private bool IsLockedTargetIdentityValid()
    {
        if (selfAIEntity == null || string.IsNullOrEmpty(lockedTargetUUId))
            return false;
        var targetCreature = selfAIEntity.targetCreatureEntity;
        return targetCreature != null && !targetCreature.IsDead() && targetCreature.creatureObj != null
            && targetCreature.fightCreatureData?.creatureData?.creatureUUId == lockedTargetUUId;
    }
    #endregion
}
