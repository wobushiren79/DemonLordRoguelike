using System;
using UnityEngine;

/// <summary>
/// 抛物线火瓶-地形火焰弹道（深渊馈赠「瓶装炼狱火」）
/// <para>双状态：飞行(Flying)→到达固定落点→燃烧(Burning)。
/// 飞行段：抛物线飞向 BUFF 注入的固定落点（不追踪目标，目标中途死亡/移动照飞原落点），纯投掷物不命中任何敌人；
/// 燃烧段：驻留落点，每1秒对半径（配置 collider_area_size[0]，默认1.2）内存活敌人跳一次伤害
/// （伤害=投掷瞬间魔王ATK×BUFF trigger_value 快照，不暴击、不递减——多片火焰叠加对同一目标多次跳伤），满5秒自毁。</para>
/// <para>视觉：飞行段 DSP 批量渲染火瓶贴图（visual_name）并自旋（弹体自旋 -720°/s，绕 -Z 轴）；到达燃烧后 visualBucketKey 置空隐藏 DSP 弹体，
/// 改由 EffectHandler.ShowFloorFireEffect 播放全局单例地面火焰粒子（粒子时长与燃烧时长同步）。</para>
/// <para>纯数据发射路径：由 BuffEntityPeriodicAttackFireBottle 创建，伤害/起终点由 BUFF 侧注入；无 prefab/武器视觉。</para>
/// </summary>
public class AttackModeRangedArcGround : AttackModeRangedArc
{
    #region 常量
    /// <summary>燃烧持续时长（秒）</summary>
    private const float BurningDuration = 5f;
    /// <summary>跳伤间隔（秒）</summary>
    private const float TickInterval = 1f;
    /// <summary>飞行段最短时长（秒）：短距离按距离折算的时长过短，抛物线竖直速度∝弧高/距离会被放大，设时长下限托底保证投掷动画观感</summary>
    private const float MinSegmentTime = 0.5f;
    /// <summary>弹体自旋速度（度/秒；负值绕 -Z 轴，飞行段火瓶旋转）</summary>
    private const float SpinSpeed = -720f;
    #endregion

    #region 状态枚举
    /// <summary>弹道阶段</summary>
    private enum FireBottleState
    {
        /// <summary>飞行（抛物线投掷）</summary>
        Flying,
        /// <summary>燃烧（驻留跳伤）</summary>
        Burning,
    }
    #endregion

    #region 字段
    /// <summary>当前阶段</summary>
    private FireBottleState state = FireBottleState.Flying;
    /// <summary>燃烧已持续时长（按 GetFightDeltaTime 累积，跟随2倍速）</summary>
    private float burningTime;
    /// <summary>跳伤计时器（每满 TickInterval 跳一次）</summary>
    private float tickTimer;
    /// <summary>当前分段距离（progress 归一化用，保证恒定世界速度）</summary>
    private float segmentDistance;
    /// <summary>燃烧落点（=BUFF 注入的固定 targetPos）</summary>
    private Vector3 centerPos;
    /// <summary>燃烧判定半径（配置 collider_area_size[0]，默认 1.2）</summary>
    private float fireRadius = 1.2f;
    #endregion

    #region 初始化外形
    /// <summary>
    /// 初始化攻击外形：火瓶走纯数据发射（无武器视觉、无 prefab），只需还原视觉参数 → 写自旋 → 登记 DSP 桶。
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
    /// 开始攻击基础：缓存固定落点/分段距离/燃烧半径，状态归位飞行
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        state = FireBottleState.Flying;
        burningTime = 0;
        tickTimer = 0;
        centerPos = attackModeData.targetPos;
        segmentDistance = Vector3.Distance(attackModeData.startPos, attackModeData.targetPos);
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        if (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0)
            fireRadius = arrAreaSize[0];
    }
    #endregion

    #region 射线（纯投掷物禁用命中）
    /// <summary>
    /// 收集本帧射线：恒不入队——火瓶是投掷物，飞行途中不砸敌人（只落到目标位置燃放）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
    }

    /// <summary>
    /// 检测碰撞：恒返回 null（禁用基类 Arc 后半程的命中检测）
    /// </summary>
    public override FightCreatureEntity CheckHitTargetForSingle()
    {
        return null;
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：按阶段分流——飞行段走抛物线移动，燃烧段做周期跳伤计时
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        if (state == FireBottleState.Flying)
            HandleForMoveFlying();
        else
            UpdateBurning();
    }

    /// <summary>
    /// 抛物线移动：分段时长 = max(距离/速度, MinSegmentTime) 保证恒定世界速度与投掷动画下限；到达终点切燃烧
    /// </summary>
    private void HandleForMoveFlying()
    {
        if (progress < 1f)
        {
            float segmentTime = Mathf.Max(segmentDistance / GetMoveSpeed(), MinSegmentTime);
            progress += GameFightLogic.GetFightDeltaTime() / segmentTime;
            float p = Mathf.Min(progress, 1f);
            float parabola = 1f - 4f * (p - 0.5f) * (p - 0.5f);
            Vector3 nextPos = Vector3.Lerp(attackModeData.startPos, attackModeData.targetPos, p);
            nextPos.y += parabola * arcHeight;
            SetPosition(nextPos);
        }
        else
        {
            SetPosition(centerPos);
            SwitchToBurning();
        }
    }

    /// <summary>
    /// 切换到燃烧阶段：停在落点、隐藏 DSP 弹体（火焰视觉交给粒子）、播放全局单例地面火焰粒子
    /// </summary>
    private void SwitchToBurning()
    {
        state = FireBottleState.Burning;
        burningTime = 0;
        tickTimer = 0;
        //隐藏 DSP 弹体（visualBucketKey 置空后 RenderAll 按空 key 跳过本发），燃烧视觉由 ShowFloorFireEffect 承担
        visualBucketKey = null;
        //播放全局单例地面火焰粒子（粒子时长与燃烧持续时间同步）
        EffectHandler.Instance.ShowFloorFireEffect(centerPos, BurningDuration);
    }

    /// <summary>
    /// 燃烧阶段：按 GetFightDeltaTime 累积时长，每满 TickInterval 对半径内存活敌人跳一次伤害；满 BurningDuration 自毁
    /// </summary>
    private void UpdateBurning()
    {
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        burningTime += deltaTime;
        tickTimer += deltaTime;
        if (tickTimer >= TickInterval)
        {
            tickTimer = 0;
            CheckHitTargetArea(centerPos, (enemy) =>
            {
                if (enemy != null && !enemy.IsDead())
                {
                    enemy.UnderAttack(this);
                }
            });
        }
        if (burningTime >= BurningDuration)
        {
            Destroy();
        }
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：状态复位飞行、清燃烧计时（防对象池复用残留）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        state = FireBottleState.Flying;
        burningTime = 0;
        tickTimer = 0;
        base.Destroy(isPermanently);
    }
    #endregion
}
