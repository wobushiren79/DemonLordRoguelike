using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回旋镖弹道（深渊馈赠「死亡回旋」）
/// <para>飞行路径：起点 → 目标锁定位置（不跟踪）→ 超出一格（折返点）→ 返回魔王身边。</para>
/// <para>速度曲线（时间参数化驱动，保证精确到点）：去程前半程全速、后半程线性减速到 0（折返点静止）；
/// 返程从 0 匀加速到 ReturnMaxSpeedRate 倍基础速度（回到魔王时最快）。</para>
/// <para>弹体自旋：发射时在 InitAttackModeShow 写入 spinSpeed/spinAxis（DSP 自旋子桶，shader 按 _Time 自转 + 每发随机相位）。</para>
/// <para>伤害逐目标减半（保底1点）；去重名单在返程开始时刷新，去程不能重复命中同一目标，返程途中可再次命中。</para>
/// <para>纯数据发射路径：由 BuffEntityPeriodicAttackBoomerang 创建，伤害与目标位置由 BUFF 侧注入。</para>
/// <para>无 prefab（走 DSP visual_name 批量渲染），不用射线批处理（gameObject 为空，走 live Physics 回退）。</para>
/// </summary>
public class AttackModeRangedBoomerang : BaseAttackMode
{
    #region 阶段枚举
    private enum Phase
    {
        /// <summary>去程：起点 → 折返点（前半程全速，后半程线性减速到 0）</summary>
        Outbound,
        /// <summary>返程：折返点 → 魔王身边（从 0 匀加速到 ReturnMaxSpeedRate 倍）</summary>
        Return
    }
    #endregion

    #region 字段
    private Phase phase;
    /// <summary>锁定目标位置（发射时快照，不跟踪目标移动）</summary>
    private Vector3 targetLockPosition;
    /// <summary>折返点（超过目标一格的位置，去程终点 = 返程起点）</summary>
    private Vector3 turnPosition;
    /// <summary>魔王位置（发射时快照，返回终点）</summary>
    private Vector3 ownerPosition;
    /// <summary>去程固定方向（从魔王指向目标），返程取其反方向</summary>
    private Vector3 outboundDirection;
    /// <summary>去程总路程（起点→折返点）</summary>
    private float outboundTotalDistance;
    /// <summary>基础飞行速度（发射时快照 GetMoveSpeed，避免每帧重算）</summary>
    private float baseSpeed;
    /// <summary>当前阶段已耗时（秒，时间参数化驱动位置）</summary>
    private float phaseTime;
    /// <summary>去程总时长（全速段 + 减速段）</summary>
    private float outboundDuration;
    /// <summary>返程总时长</summary>
    private float returnDuration;
    /// <summary>超出目标的格数（世界单位）</summary>
    private const float PassThroughDistance = 1.0f;
    private const int MinDamage = 1;
    /// <summary>返程末端最大速度倍率（相对基础速度，回到魔王身边时最快）</summary>
    private const float ReturnMaxSpeedRate = 1.5f;
    /// <summary>弹体自旋速度（度/秒；负值绕 -Z 轴，与视觉材质 Mat_AttackModeVisual_Boomerang_1 的 authored 自旋一致）</summary>
    private const float SpinSpeed = -720f;
    /// <summary>当前阶段去重名单：去程用，进入返程时清空</summary>
    private HashSet<string> hitTargetsInPhase = new HashSet<string>();
    #endregion

    #region 初始化外形
    /// <summary>
    /// 初始化攻击外形：回旋镖走纯数据发射（无武器视觉、无 prefab），只需还原视觉参数 → 写自旋 → 登记 DSP 桶。
    /// <para>⚠️不调 base：自旋必须在 EnsureAttackModeVisual 之前写好（桶签名含自旋，登记时才克隆材质并写入 _RotateSpeed；
    /// 基础桶注册会把材质自旋关键字关掉，不写自旋即为"转不起来"的原因）。</para>
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
    /// 开始攻击基础：无参 StartAttack() 与生物路径 StartAttack(attacker,attacked) 最终都走到这里。
    /// 在此快照目标位置、魔王位置、去程方向与速度，并算出去/返程时长（时间参数化运动的总时长）。
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        targetLockPosition = attackModeData.targetPos;
        ownerPosition = attackModeData.startPos;
        Vector3 dir = targetLockPosition - ownerPosition;
        // 方向为零（起点与目标重合，极端情况）则直接回收
        if (dir == Vector3.zero)
        {
            Destroy();
            return;
        }
        baseSpeed = GetMoveSpeed();
        // 基础速度非法（配置 speed_move=0 等）不会飞行，直接回收
        if (baseSpeed <= 0f)
        {
            Destroy();
            return;
        }
        outboundDirection = dir.normalized;
        outboundTotalDistance = dir.magnitude + PassThroughDistance;
        turnPosition = ownerPosition + outboundDirection * outboundTotalDistance;
        // 去程：前半程全速(时长 half/v)，后半程 v→0 线性减速(平均 v/2，时长 half/(v/2)=D/v)，总时长 1.5D/v
        outboundDuration = 1.5f * outboundTotalDistance / baseSpeed;
        // 返程：0→ReturnMaxSpeedRate*v 匀加速，路程=折返点回魔王的距离(=去程总路程)，时长 2D/vmax
        returnDuration = 2f * outboundTotalDistance / (ReturnMaxSpeedRate * baseSpeed);
        attackModeData.attackDirection = outboundDirection;
        phase = Phase.Outbound;
        phaseTime = 0f;
        hitTargetsInPhase.Clear();
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：刷新方向 → 命中检测（去重）→ 时间参数化移动（阶段切换在移动内完成）
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        // 按当前阶段刷新飞行方向（供 live Physics 命中检测 + 移动使用）
        RefreshDirection();
        // 检测命中（live Physics 路径），去重后结算
        FightCreatureEntity hitTarget = CheckHitTargetForSingle();
        if (hitTarget != null && hitTarget.fightCreatureData?.creatureData != null)
        {
            string id = hitTarget.fightCreatureData.creatureData.creatureUUId;
            if (!hitTargetsInPhase.Contains(id))
            {
                HandleForHitTarget(hitTarget);
                hitTargetsInPhase.Add(id);
            }
        }
        // 移动（含阶段切换/抵达销毁）
        HandleForMove();
        // 越界兜底销毁
        if (isValid && CheckIsMoveBound())
            Destroy();
    }
    #endregion

    #region 方向
    /// <summary>
    /// 按当前阶段计算飞行方向并写入 attackModeData.attackDirection
    /// </summary>
    private void RefreshDirection()
    {
        if (phase == Phase.Outbound)
        {
            attackModeData.attackDirection = outboundDirection;
        }
        else
        {
            Vector3 toOwner = ownerPosition - position;
            attackModeData.attackDirection = toOwner != Vector3.zero ? toOwner.normalized : -outboundDirection;
        }
    }
    #endregion

    #region 移动
    /// <summary>
    /// 时间参数化移动：去程位置 = 起点 + 方向 × s(t)（前半程匀速、后半程减速到 0），
    /// 返程位置 = 折返点 → 魔王 按匀加速 s(t) 插值；到点切换阶段/销毁。
    /// </summary>
    private void HandleForMove()
    {
        phaseTime += GameFightLogic.GetFightDeltaTime();
        if (phase == Phase.Outbound)
        {
            SetPosition(ownerPosition + outboundDirection * GetOutboundDistance(phaseTime));
            // 抵达折返点 → 进入返程（清空去重名单，返程可再次命中）
            if (phaseTime >= outboundDuration)
            {
                phase = Phase.Return;
                phaseTime = 0f;
                hitTargetsInPhase.Clear();
            }
        }
        else
        {
            // 返程匀加速：s = vmax·t²/(2·Tr)，按路程比例在折返点→魔王间插值
            float t = Mathf.Min(phaseTime, returnDuration);
            float maxSpeed = ReturnMaxSpeedRate * baseSpeed;
            float s = maxSpeed * t * t / (2f * returnDuration);
            SetPosition(Vector3.Lerp(turnPosition, ownerPosition, s / outboundTotalDistance));
            // 回到魔王身边 → 销毁
            if (phaseTime >= returnDuration)
                Destroy();
        }
    }

    /// <summary>
    /// 去程路程函数 s(t)：前半程全速 s=v·t；后半程线性减速 s=half + v·τ - (v/2T₂)·τ²（末端速度恰为 0）
    /// </summary>
    private float GetOutboundDistance(float t)
    {
        float half = outboundTotalDistance * 0.5f;
        float fullSpeedTime = half / baseSpeed;
        if (t <= fullSpeedTime)
            return baseSpeed * t;
        // 减速段时长 T₂ = D/v（平均速度 v/2 走完后半程 half）
        float decelDuration = outboundTotalDistance / baseSpeed;
        float decelTime = Mathf.Min(t - fullSpeedTime, decelDuration);
        return half + baseSpeed * decelTime - (baseSpeed / (2f * decelDuration)) * decelTime * decelTime;
    }
    #endregion

    #region 命中处理
    /// <summary>
    /// 对单个目标结算伤害，随后伤害减半（保底1），不销毁弹道；播放击中特效。
    /// </summary>
    private void HandleForHitTarget(FightCreatureEntity creature)
    {
        creature.UnderAttack(this);
        attackModeData.attackerDamage = Math.Max(MinDamage, attackModeData.attackerDamage / 2);
        PlayEffectForHit(creature.creatureObj.transform.position);
    }
    #endregion
}
