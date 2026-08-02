using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹跳斧头弹道（深渊馈赠「跳跳斧」）
/// <para>分段式抛物线：起点→锁定目标飞一段抛物线，命中后结算并以命中点为起点向下一个弹跳目标再飞一段抛物线，
/// 直到弹跳次数耗尽或附近无可弹跳目标时销毁。每段均为抛物线（需求：弹跳也以抛物线处理弹道）。</para>
/// <para>追踪：目标存活时每帧将目标点跟随其当前位置（敌人移动不落空）；目标中途死亡则飞到最后已知位置，落地无碰撞即销毁。</para>
/// <para>命中规则：每段仅**下落阶段**（progress≥0.5，抛物线顶点之后）才检测命中，上升阶段不命中敌人（继承 Arc 的后半程检测规则，progress 每段重置故天然分段生效）；
/// 同一斧头全程去重（已命中目标直接穿过不重复结算）；伤害逐目标减半保底1；落地时锁定目标仍存活未命中则强制结算（追踪保底）。</para>
/// <para>弹跳目标：以被命中者为圆心 1 单位距离内的存活敌人随机取 1（排除已命中者），无候选则不弹跳。</para>
/// <para>分段节奏：分段时长 = max(距离/速度, MinSegmentTime=0.5s)——水平方向按距离归一化保证恒定世界速度，
/// 但抛物线竖直速度∝弧高/距离，1 格弹跳若按距离折算仅 ~0.125s（竖直速度数倍于首段、观感比首段还快），故用时长下限托底；
/// 弧高：首段=完整 arcHeight，第 2 次及以后跳跃=其一半（BounceArcHeightRate=0.5）。</para>
/// <para>弹体自旋：InitAttackModeShow 写 spinSpeed/spinAxis（DSP 自旋子桶，shader 按 _Time 自转 + 每发随机相位）。</para>
/// <para>纯数据发射路径：由 BuffEntityPeriodicAttackBounceAxe 创建，伤害/起终点/弹跳次数由 BUFF 侧注入
/// （bounceMax 在 StartAttack 前写入，Destroy 清零防对象池残留）。无 prefab（走 DSP visual_name 批量渲染）。</para>
/// </summary>
public class AttackModeRangedArcBounce : AttackModeRangedArc
{
    #region 常量
    /// <summary>伤害递减保底</summary>
    private const int MinDamage = 1;
    /// <summary>弹跳段弧高倍率：第 2 次及以后跳跃的高度 = 首段射出高度 × 此值</summary>
    private const float BounceArcHeightRate = 0.5f;
    /// <summary>单段最短飞行时长（秒）：短距离弹跳按距离折算的时长过短（1格弹跳仅~0.125s），
    /// 抛物线竖直速度∝弧高/距离会被放大数倍、观感比首段还快，故设时长下限把短段放慢（2026-07 手感调到 1.0s，与首段慢一倍后的节奏一致）</summary>
    private const float MinSegmentTime = 1.0f;
    /// <summary>弹体自旋速度（度/秒；负值绕 -Z 轴）</summary>
    private const float SpinSpeed = -720f;
    #endregion

    #region 字段
    /// <summary>弹跳次数上限（BUFF 在 StartAttack 前注入；0=不弹跳）</summary>
    public int bounceMax;
    /// <summary>已弹跳次数</summary>
    private int bounceCurrent;
    /// <summary>当前锁定目标（追踪用，发射/每次弹跳时写入）</summary>
    private FightCreatureEntity currentTarget;
    /// <summary>全程命中去重名单（同一斧头的命中目标不重复）</summary>
    private readonly HashSet<string> hitCreatureIds = new HashSet<string>();
    /// <summary>弹跳候选缓冲（复用，避免每次 new List 产生 GC）</summary>
    private readonly List<FightCreatureEntity> listBounceCandidate = new List<FightCreatureEntity>();
    /// <summary>当前分段距离（progress 按它归一化，保证恒定世界速度）</summary>
    private float segmentDistance;
    /// <summary>当前分段弧高（首段=完整 arcHeight，弹跳段=其一半 BounceArcHeightRate）</summary>
    private float currentArcHeight;
    /// <summary>当前分段弹跳搜索半径（发射时从攻击模块配置 collider_area_size.x 读取，默认 1 单位）</summary>
    private float bounceSearchRadius = 1f;
    #endregion

    #region 初始化外形
    /// <summary>
    /// 初始化攻击外形：斧头走纯数据发射（无武器视觉、无 prefab），只需还原视觉参数 → 写自旋 → 登记 DSP 桶。
    /// <para>⚠️不调 base：自旋必须在 EnsureAttackModeVisual 之前写好（桶签名含自旋，登记时才克隆材质并写入 _RotateSpeed）。</para>
    /// </summary>
    public override void InitAttackModeShow()
    {
        ResetVisualParams();
        spinSpeed = SpinSpeed;
        spinAxis = Vector3.forward;
        FightHandler.Instance.manager.EnsureAttackModeVisual(this);
    }
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：清空本次攻击状态，经 attackedId 解析初始锁定目标（BUFF 纯数据发射注入），并设置首个飞行分段。
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        hitCreatureIds.Clear();
        listBounceCandidate.Clear();
        bounceCurrent = 0;
        // 弹跳搜索半径取配置 collider_area_size 的第 1 项（未配/配 0 则默认 1 单位）
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        if (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0)
            bounceSearchRadius = arrAreaSize[0];
        else
            bounceSearchRadius = 1f;
        // 解析初始锁定目标（可能已死 → null 则飞快照落点）
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        currentTarget = fightLogic?.fightData?.GetCreatureById(attackModeData.attackedId, CreatureFightTypeEnum.FightAttack);
        if (currentTarget != null && currentTarget.IsDead())
            currentTarget = null;
        SetupSegment(attackModeData.startPos, attackModeData.targetPos);
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：先追踪刷新目标点与飞行方向，再走远程弹道的「命中检测 → 移动 → 边界」流程
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        RefreshTracking();
        base.Update();
    }

    /// <summary>
    /// 追踪刷新：锁定目标存活则目标点跟随其当前位置（目标死亡则飞到最后已知位置）；并刷新朝向供射线检测
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
            }
        }
        Vector3 dir = attackModeData.targetPos - position;
        if (dir != Vector3.zero)
            attackModeData.attackDirection = dir.normalized;
    }
    #endregion

    #region 移动
    /// <summary>
    /// 抛物线移动：分段时长 = max(距离/速度, MinSegmentTime)，避免短距离弹跳被压缩成"比首段还快"的快跳；到达终点走落地处理而非直接销毁
    /// </summary>
    public override void HandleForMove()
    {
        if (progress < 1f)
        {
            // 水平方向按距离归一化保证恒定世界速度；竖直方向速度∝弧高/时长，短段用 MinSegmentTime 托底
            float segmentTime = Mathf.Max(segmentDistance / GetMoveSpeed(), MinSegmentTime);
            progress += GameFightLogic.GetFightDeltaTime() / segmentTime;
            float p = Mathf.Min(progress, 1f);
            // 计算抛物线路径
            float parabola = 1.0f - 4.0f * (p - 0.5f) * (p - 0.5f);
            Vector3 nextPos = Vector3.Lerp(attackModeData.startPos, attackModeData.targetPos, p);
            nextPos.y += parabola * currentArcHeight;
            SetPosition(nextPos);
        }
        else
        {
            // 到达终点
            SetPosition(attackModeData.targetPos);
            HandleForArrive();
        }
    }

    /// <summary>
    /// 边界处理：本帧已在移动/命中中销毁则跳过（防同一帧二次 Destroy 重复入池）
    /// </summary>
    public override void HandleForBound()
    {
        if (isValid)
            base.HandleForBound();
    }

    /// <summary>
    /// 落地处理：锁定目标仍存活且未被本斧头命中过则强制结算它（追踪下落点必在目标上，此为保险），否则销毁
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

    /// <summary>
    /// 设置新分段：写起终点、计算分段距离（progress 归一化用）与弧高（首段完整 arcHeight，弹跳段减半），并重置分段进度
    /// </summary>
    private void SetupSegment(Vector3 segStart, Vector3 segEnd)
    {
        attackModeData.startPos = segStart;
        attackModeData.targetPos = segEnd;
        segmentDistance = Vector3.Distance(segStart, segEnd);
        // bounceCurrent==0 为首段（扔出），>=1 为弹跳段（高度减半）
        currentArcHeight = bounceCurrent == 0 ? arcHeight : arcHeight * BounceArcHeightRate;
        progress = 0f;
    }
    #endregion

    #region 命中处理
    /// <summary>
    /// 检测碰撞：过滤掉本斧头已命中过的目标（直接穿过，不重复结算）。
    /// <para>仅下落阶段生效——基类 Arc 在 progress&lt;0.5（抛物线顶点前的上升段）返回 null，只有顶点后的下落段才做命中检测（每段 progress 重置，分段天然生效）。</para>
    /// </summary>
    public override FightCreatureEntity CheckHitTargetForSingle()
    {
        FightCreatureEntity hitTarget = base.CheckHitTargetForSingle();
        if (hitTarget != null && IsHitBefore(hitTarget))
            return null;
        return hitTarget;
    }

    /// <summary>
    /// 命中结算：扣血 → 伤害减半(保底1) → 击中特效 → 去重登记 → 弹跳决策（次数耗尽或无候选则销毁，否则开启新分段）
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fightCreatureEntity)
    {
        fightCreatureEntity.UnderAttack(this);
        attackModeData.attackerDamage = Math.Max(MinDamage, attackModeData.attackerDamage / 2);
        PlayEffectForHit(fightCreatureEntity.creatureObj.transform.position);
        hitCreatureIds.Add(fightCreatureEntity.fightCreatureData.creatureData.creatureUUId);

        // 弹跳次数耗尽则销毁
        if (bounceCurrent >= bounceMax)
        {
            Destroy();
            return;
        }
        // 附近没有可弹跳目标，剩余弹跳次数作废
        FightCreatureEntity bounceTarget = FindBounceTarget(fightCreatureEntity);
        if (bounceTarget == null)
        {
            Destroy();
            return;
        }
        bounceCurrent++;
        currentTarget = bounceTarget;
        SetupSegment(position, bounceTarget.creatureObj.transform.position);
    }

    /// <summary>
    /// 以被命中目标为圆心搜索弹跳目标：弹跳搜索半径内存活、未被本斧头命中过的敌人随机取 1；无候选返回 null
    /// </summary>
    private FightCreatureEntity FindBounceTarget(FightCreatureEntity hitCreature)
    {
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (fightLogic?.fightData == null)
            return null;
        var listEnemy = fightLogic.fightData.dlAttackCreatureEntity.List;
        Vector3 centerPos = hitCreature.creatureObj.transform.position;
        float sqrRadius = bounceSearchRadius * bounceSearchRadius;
        listBounceCandidate.Clear();
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var enemy = listEnemy[i];
            if (enemy == null || enemy.IsDead() || enemy.creatureObj == null)
                continue;
            if (IsHitBefore(enemy))
                continue;
            if ((enemy.creatureObj.transform.position - centerPos).sqrMagnitude > sqrRadius)
                continue;
            listBounceCandidate.Add(enemy);
        }
        if (listBounceCandidate.Count == 0)
            return null;
        return listBounceCandidate[UnityEngine.Random.Range(0, listBounceCandidate.Count)];
    }

    /// <summary>
    /// 目标是否已被本斧头命中过（全程去重名单）
    /// </summary>
    private bool IsHitBefore(FightCreatureEntity creature)
    {
        var creatureData = creature?.fightCreatureData?.creatureData;
        return creatureData != null && hitCreatureIds.Contains(creatureData.creatureUUId);
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：清空本次攻击状态（弹跳计数/去重名单/锁定目标/注入参数），防对象池复用残留
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        bounceMax = 0;
        bounceCurrent = 0;
        currentTarget = null;
        hitCreatureIds.Clear();
        listBounceCandidate.Clear();
        base.Destroy(isPermanently);
    }
    #endregion
}
