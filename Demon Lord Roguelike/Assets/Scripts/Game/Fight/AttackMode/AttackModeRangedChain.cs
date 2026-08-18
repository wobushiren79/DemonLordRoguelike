using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 直线连锁弹道（哥布林魔法师 火球/冰球）
/// <para>直线追踪：锁定目标后沿直线飞行，目标存活时每帧将目标点跟随其当前位置（敌人移动不落空）；
/// 命中后结算并以命中者为圆心搜索下一个连锁目标，继续直线追踪，直到连锁次数耗尽或无候选时销毁。</para>
/// <para>只认锁定目标：射线命中结果必须等于当前锁定目标才结算，挡路者直接穿过——保证「恰好击中 chainMax+1 个目标」的确定性。</para>
/// <para>目标死亡即哑弹：锁定目标中途死亡则不再命中任何人，直飞到最后已知位置即销毁，不再继续连锁（与跳跳斧"死后仍可撞人"刻意区分）。</para>
/// <para>伤害不递减：每次命中均为全额 attackerDamage（与跳跳斧逐跳减半刻意区分）；命中时 buff 字段配置的异常属性由
/// <see cref="FightCreatureEntity.UnderAttack(BaseAttackMode)"/> → AddBuff 自动按概率附加。</para>
/// <para>【配置】hit_max=连锁次数（2=连锁2次共击中3目标）；collider_area_size 第1项=连锁传递半径（缺省1单位）；
/// buff=命中概率附加的异常属性；speed_move=直线弹速；visual_name 走 DSP 批量渲染（火球/冰球 billboard 自动朝向飞行方向）。</para>
/// </summary>
public class AttackModeRangedChain : AttackModeRanged
{
    #region 字段
    /// <summary>连锁次数上限（从配置 hit_max 读取；0=不连锁，命中首目标即销毁）</summary>
    public int chainMax;
    /// <summary>已连锁次数</summary>
    private int chainCurrent;
    /// <summary>当前锁定目标（追踪用，发射/每次连锁时写入；死亡置null转直飞落点销毁）</summary>
    private FightCreatureEntity currentTarget;
    /// <summary>全程命中去重名单（同一弹道的命中目标不重复；复用避免每次发射 new）</summary>
    private readonly HashSet<string> hitCreatureIds = new HashSet<string>();
    /// <summary>连锁候选缓冲（复用，避免每次 new List 产生 GC）</summary>
    private readonly List<FightCreatureEntity> listChainCandidate = new List<FightCreatureEntity>();
    /// <summary>连锁传递搜索半径（发射时从攻击模块配置 collider_area_size.x 读取，默认 1 单位）</summary>
    private float chainSearchRadius = 1f;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：清空本次攻击状态，从配置读连锁次数/传递半径，并经 attackedId 解析初始锁定目标（已死→null 则直飞落点消失）
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        hitCreatureIds.Clear();
        listChainCandidate.Clear();
        chainCurrent = 0;
        chainMax = attackModeInfo.hit_max;
        // 连锁传递半径取配置 collider_area_size 的第 1 项（未配/配 0 则默认 1 单位）
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        if (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0)
            chainSearchRadius = arrAreaSize[0];
        else
            chainSearchRadius = 1f;
        // 解析初始锁定目标（searchCreatureType 由 base 按被攻击者层级缓存，生物发射/纯数据发射两条路径都可用）
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        currentTarget = fightLogic?.fightData?.GetCreatureById(attackModeData.attackedId, searchCreatureType);
        if (currentTarget != null && currentTarget.IsDead())
            currentTarget = null;
    }
    #endregion

    #region 射线收集
    /// <summary>
    /// 收集本帧射线检测请求：仅锁定目标存活时，先朝目标当前位置刷新直线朝向再入队射线（与 Update 内的方向计算保持一致）；
    /// 目标已死则不入队（本段不再命中任何人，直飞落点销毁）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        if (currentTarget != null)
        {
            attackModeData.attackDirection = (currentTarget.creatureObj.transform.position - position).SetY(0).normalized;
            EnqueueSingleRay(batch);
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：先追踪刷新目标点，再走「命中检测 → 直线移动（到点结算/销毁） → 边界」流程
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        RefreshTracking();
        FightCreatureEntity hitTarget = CheckHitTargetForSingle();
        if (hitTarget != null)
        {
            HandleForHitTarget(hitTarget);
            return;
        }
        HandleForMove();
        //本帧已在移动/命中中销毁则跳过（防同一帧二次 Destroy 重复入池）
        if (isValid)
            HandleForBound();
    }

    /// <summary>
    /// 追踪刷新：锁定目标存活则目标点跟随其当前位置并刷新直线朝向；目标死亡则置 null（targetPos 保留最后已知位置，直飞过去销毁）
    /// </summary>
    private void RefreshTracking()
    {
        if (currentTarget != null)
        {
            if (currentTarget.IsDead() || currentTarget.creatureObj == null)
            {
                currentTarget = null;
            }
            else
            {
                attackModeData.targetPos = currentTarget.creatureObj.transform.position;
                Vector3 dir = (attackModeData.targetPos - position).SetY(0);
                if (dir != Vector3.zero)
                    attackModeData.attackDirection = dir.normalized;
            }
        }
    }
    #endregion

    #region 移动
    /// <summary>
    /// 直线移动：沿追踪朝向平移；距目标点（水平）不足一帧步长则吸附到点并做到达处理（存活保底结算/死亡直飞销毁）
    /// </summary>
    public override void HandleForMove()
    {
        float step = GameFightLogic.GetFightDeltaTime() * GetMoveSpeed();
        Vector3 toTarget = (attackModeData.targetPos - position).SetY(0);
        if (toTarget.magnitude <= step)
        {
            //保持弹道当前高度，仅水平吸附到目标点
            SetPosition(new Vector3(attackModeData.targetPos.x, position.y, attackModeData.targetPos.z));
            HandleForArrive();
            return;
        }
        TranslatePosition(attackModeData.attackDirection * step);
    }

    /// <summary>
    /// 到达处理：锁定目标仍存活且未被本弹道命中过则强制结算它（追踪下落点必在目标上，此为保险），否则销毁（目标死亡即哑弹）
    /// </summary>
    private void HandleForArrive()
    {
        if (currentTarget != null && !currentTarget.IsDead() && !IsHitBefore(currentTarget))
        {
            HandleForHitTarget(currentTarget);
            return;
        }
        Destroy();
    }
    #endregion

    #region 命中处理
    /// <summary>
    /// 检测碰撞：只认当前锁定目标——射线命中结果不等于锁定目标（挡路者）直接穿过；
    /// 锁定目标已死（未入队射线）一律返回 null，本段不再命中任何人
    /// </summary>
    public override FightCreatureEntity CheckHitTargetForSingle()
    {
        if (currentTarget == null)
            return null;
        FightCreatureEntity hitTarget = base.CheckHitTargetForSingle();
        if (hitTarget != currentTarget)
            return null;
        if (IsHitBefore(hitTarget))
            return null;
        return hitTarget;
    }

    /// <summary>
    /// 命中结算：扣血（全额不递减，异常属性由 buff 字段自动附加）→ 击中特效 → 去重登记 → 连锁决策（次数耗尽或无候选则销毁，否则锁定新目标继续直线追踪）
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fightCreatureEntity)
    {
        fightCreatureEntity.UnderAttack(this);
        //击中特效取子弹当前位置(真实命中点,带飞行高度)——目标 transform.position 是脚下原点,会把爆炸放到地上(哥布林法师火/冰球曾如此)
        PlayEffectForHit(position);
        hitCreatureIds.Add(fightCreatureEntity.fightCreatureData.creatureData.creatureUUId);

        // 连锁次数耗尽则销毁
        if (chainCurrent >= chainMax)
        {
            Destroy();
            return;
        }
        // 附近没有可连锁目标，剩余连锁次数作废
        FightCreatureEntity chainTarget = FindChainTarget(fightCreatureEntity);
        if (chainTarget == null)
        {
            Destroy();
            return;
        }
        chainCurrent++;
        currentTarget = chainTarget;
        attackModeData.targetPos = chainTarget.creatureObj.transform.position;
    }

    /// <summary>
    /// 以被命中目标为圆心搜索连锁目标：传递半径内存活、未被本弹道命中过的敌人随机取 1；无候选返回 null
    /// </summary>
    private FightCreatureEntity FindChainTarget(FightCreatureEntity hitCreature)
    {
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (fightLogic?.fightData == null)
            return null;
        //按本弹道的攻击目标类型取对应侧列表（打进攻方搜进攻列表，打防守方搜防守列表）
        var listEnemy = searchCreatureType == CreatureFightTypeEnum.FightDefense
            ? fightLogic.fightData.dlDefenseCreatureEntity.List
            : fightLogic.fightData.dlAttackCreatureEntity.List;
        Vector3 centerPos = hitCreature.creatureObj.transform.position;
        float sqrRadius = chainSearchRadius * chainSearchRadius;
        listChainCandidate.Clear();
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var enemy = listEnemy[i];
            if (enemy == null || enemy.IsDead() || enemy.creatureObj == null)
                continue;
            if (IsHitBefore(enemy))
                continue;
            if ((enemy.creatureObj.transform.position - centerPos).sqrMagnitude > sqrRadius)
                continue;
            listChainCandidate.Add(enemy);
        }
        if (listChainCandidate.Count == 0)
            return null;
        return listChainCandidate[UnityEngine.Random.Range(0, listChainCandidate.Count)];
    }

    /// <summary>
    /// 目标是否已被本弹道命中过（全程去重名单）
    /// </summary>
    private bool IsHitBefore(FightCreatureEntity creature)
    {
        var creatureData = creature?.fightCreatureData?.creatureData;
        return creatureData != null && hitCreatureIds.Contains(creatureData.creatureUUId);
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：清空本次攻击状态（连锁计数/去重名单/锁定目标），防对象池复用残留
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        chainMax = 0;
        chainCurrent = 0;
        currentTarget = null;
        hitCreatureIds.Clear();
        listChainCandidate.Clear();
        base.Destroy(isPermanently);
    }
    #endregion
}
