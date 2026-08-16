using System;
using UnityEngine;
using UnityEngine;

/// <summary>
/// 抛物线落点跟踪弹道：抛射飞向目标，飞行中落点实时跟踪目标（目标脚底 + 本发弹道起始点相对攻击者的偏移，即与发射口同高）；
/// 目标死亡则落点锁定在其死亡点继续下落，
/// 到达落点时按 collider_area_type/collider_area_size 做范围检测，取距落点最近的一个存活敌人结算单体伤害
/// （范围仅作检测窗口、非AOE——落点处站着其他敌人也会被命中，但每次只命中一个）。
/// <para>弹道途中不命中任何目标（抛射越过途中敌人），命中只发生在落点。</para>
/// <para>射空收尾：落点范围无存活敌人时按 other_data 的插地键处理——stuck_time>0 弹体插入地面停留（stuck_sink 下沉深度），倒计时结束回收；未配置则落地即销毁。</para>
/// <para>视觉：弹体朝向每帧按抛物线切线角旋转（上升段上仰、顶点水平、下落段下俯），切线角叠加在武器 StartRotate 修正基准角上；弧高 2。</para>
/// <para>【配置】class_name + collider_area_type(11=AreaSphere) + collider_area_size(落点检测半径) + other_data(stuck_time/stuck_sink，可选)，视觉/音效/拖尾配置同普通远程。</para>
/// </summary>
public class AttackModeRangedArcTracking : AttackModeRangedArc
{
    #region 字段
    /// <summary>落点跟踪目标（死亡后落点锁定其最后位置，即死亡点）</summary>
    public FightCreatureEntity attacked;
    /// <summary>弹体贴图朝向修正基准角（发射时取武器 StartRotate 写入的 visualStartAngle，切线角在其上叠加）</summary>
    protected float baseVisualAngle;
    /// <summary>落点相对目标脚底的偏移（=本发弹道起始点相对攻击者位置的偏移，发射时缓存；落点与发射口同高）</summary>
    protected Vector3 targetPosOffset;
    /// <summary>插地剩余停留时长（&lt;0=未插地；由 other_data 的 stuck_time 键驱动，射空落地时启用，倒计时走 GetFightDeltaTime 跟随游戏速度）</summary>
    protected float stuckTimeRemaining = -1f;
    #endregion

    #region 构造函数
    /// <summary>
    /// 构造函数：弧高设为 2（基类默认 3 对本弹道偏高；对象池复用不重置此字段，构造一次即恒为2）
    /// </summary>
    public AttackModeRangedArcTracking()
    {
        arcHeight = 2f;
    }
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：重置插地状态（对象池复用不残留上一发的插地倒计时）
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        stuckTimeRemaining = -1f;
    }

    /// <summary>
    /// 开始攻击-纯数据发射：无跟踪目标直接销毁（本弹道必须挂目标发射）
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击-生物：缓存落点跟踪目标、记录武器 StartRotate 朝向基准角，并缓存落点偏移（起始点相对攻击者的偏移）
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        this.attacked = attacked;
        //记录发射时武器 StartRotate 写入的修正角（DSP 每帧读 visualStartAngle 建矩阵，切线角在其上叠加）
        baseVisualAngle = visualStartAngle;
        //落点偏移=弹道起始点相对攻击者位置的偏移（落点=目标脚底+该偏移，起落同高）
        if (attacker != null && attacker.creatureObj != null)
        {
            targetPosOffset = attackModeData.startPos - attacker.creatureObj.transform.position;
        }
        else
        {
            targetPosOffset = Vector3.zero;
        }
        //初始落点同步应用偏移（基类已把 targetPos 设为目标脚底）
        attackModeData.targetPos += targetPosOffset;
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 收集本帧射线检测请求：全程不入队（命中只发生在落点范围检测，不用弹道射线）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
    }

    /// <summary>
    /// 更新处理：插地阶段只倒计时（到期回收，不再移动/跟踪/边界检测）；否则目标存活时落点实时跟踪目标位置（死亡则落点停在其最后位置），弹体朝向跟随抛物线切线，再推进飞行
    /// </summary>
    public override void Update()
    {
        //插地停留阶段：位置/朝向冻结在落地瞬间状态（下俯切线角天然呈斜插地面姿态），倒计时结束回收
        if (stuckTimeRemaining >= 0f)
        {
            stuckTimeRemaining -= GameFightLogic.GetFightDeltaTime();
            if (stuckTimeRemaining < 0f)
                Destroy();
            return;
        }
        //落点跟踪：目标存活则落点跟随目标当前位置（脚底+起始偏移）；死亡则不再更新（targetPos 停于死亡点）
        if (attacked != null && !attacked.IsDead() && attacked.creatureObj != null)
        {
            attackModeData.targetPos = attacked.creatureObj.transform.position + targetPosOffset;
        }
        //弹体朝向跟随抛物线切线：切线垂直分量=8h(0.5-progress)（上升正/顶点零/下落负），水平分量=落点与起点x差
        float dirX = attackModeData.targetPos.x - attackModeData.startPos.x;
        if (Mathf.Abs(dirX) > 0.001f)
        {
            float tangentAngle = Mathf.Atan2(8f * arcHeight * (0.5f - progress), dirX) * Mathf.Rad2Deg;
            visualStartAngle = baseVisualAngle + tangentAngle;
        }
        //移动处理（到达落点时由 HandleForReachEnd 做范围命中并回收）
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 到达落点：范围检测取距落点最近的一个存活敌人结算单体伤害（死亡点有敌人同样会被命中），命中即回收；
    /// 射空（无存活敌人）则按 other_data 的 stuck_time/stuck_sink 配置插地停留或落地即回收
    /// </summary>
    protected override void HandleForReachEnd()
    {
        //播放击中粒子特效
        PlayEffectForHit(position);
        //落点范围检测（仅检测窗口），取最近一个敌人单体结算
        FightCreatureEntity hitTarget = null;
        float nearestDis = float.MaxValue;
        Collider[] targetColliders = GetHitTargetAreaCollider(position);
        if (targetColliders != null)
        {
            GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
            for (int i = 0; i < targetColliders.Length; i++)
            {
                var targetCreature = gameFightLogic.fightData.GetCreatureById(targetColliders[i].gameObject.name, CreatureFightTypeEnum.None);
                if (targetCreature != null && !targetCreature.IsDead() && targetCreature.creatureObj != null)
                {
                    float dis = Vector3.Distance(targetCreature.creatureObj.transform.position, position);
                    if (dis < nearestDis)
                    {
                        nearestDis = dis;
                        hitTarget = targetCreature;
                    }
                }
            }
        }
        if (hitTarget != null)
        {
            hitTarget.UnderAttack(this);
            Destroy();
            return;
        }
        //射空收尾：配置 stuck_time>0 则插地停留（弹体仍挂 dlAttackModePrefab，场景清理由 ClearAttackModePrefab/Clear 自动覆盖）；未配置落地即回收
        AttackModeStuckConfig stuckConfig = attackModeInfo.GetStuckConfig();
        if (!stuckConfig.enable)
        {
            Destroy();
            return;
        }
        //按配置下沉插入地面；关拖尾防插地期间轨迹在原地继续采样残留
        if (stuckConfig.sink > 0f)
            SetPosition(position + new Vector3(0, -stuckConfig.sink, 0));
        trailMode = AttackModeTrailType.None;
        stuckTimeRemaining = stuckConfig.time;
    }
    #endregion
}
