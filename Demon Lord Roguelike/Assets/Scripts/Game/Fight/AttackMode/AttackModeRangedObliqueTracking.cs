using System;
using UnityEngine;

/// <summary>
/// 直线斜射跟踪弹道（范围伤害版）：从高处出手（如生物 attack_start_position 配 0,2.5,0）沿直线斜射向目标，
/// 飞行中方向实时跟踪目标（含 Y 不拍平——区别于 <see cref="AttackModeRangedTracking"/> 的水平追踪），
/// 瞄准点=目标脚底 + other_data 键 aim_up 上抬高度（默认 0=瞄脚底，配 0.5 即瞄准身体上段）；
/// 目标死亡则方向冻结朝其死亡点继续直飞，全程命中检测有效（可命中路径上任意敌人），直至命中或飞出边界回收。
/// 命中时按 collider_area_type/collider_area_size 做范围检测，范围内所有存活敌人全部结算伤害（AOE，hit_max 可配命中上限）。
/// <para>与基类差异：①跟踪含 Y（能从头顶斜射下压）；②命中检测全程有效（基类目标死亡即关检测与射线）；③命中为范围 AOE（基类单体）；
/// ④贴身保底命中——0.1 短射线在「目标迎面相撞（相对位移&gt;射线长）穿进碰撞体」后永久失效（射线不打背面），
/// 跟踪弹瞄准点在体内会绕点空转（挂身抖动 BUG），故与瞄准点距离 ≤ HitTouchDistance 时直接判中；
/// ⑤触地即毁——下落中弹体中心 y ≤ GroundHitY(0.05) 时播命中特效回收（无伤害结算），目标死亡直飞等无目标场景不会穿地消失。</para>
/// <para>弹体朝向（两条通道按视觉类型各尽其用）：①面片 quad 类视觉走 visualStartAngle = 武器 StartRotate 基准角 + atan2(dir.y, dir.x)（仅 X-Y 平面俯仰，dz 纵深不参与，与抛物线切线角同规约）；
/// ②火球/冰球这类 billboard shader 视觉的实例矩阵旋转对核心无效（posOS 恒原点、世界空间 billboard 展开），改走 visualVelocityOrient=true → _VelocityWS.w>0.5，
/// 由 shader 把 billboard 角点按速度屏幕角旋转、贴图头（默认朝右）对准飞行方向（InitAttackModeShow 里开启，ResetVisualParams 每发复位故须逐发写）。</para>
/// <para>【配置】class_name + collider_area_type(11=AreaSphere) + collider_area_size(命中AOE半径) + other_data 键 aim_up(瞄准点上抬) + 可选 hit_max，视觉/音效/拖尾配置同普通远程。</para>
/// </summary>
public class AttackModeRangedObliqueTracking : AttackModeRangedTracking
{
    #region 常量
    /// <summary>贴身判定距离：弹体与瞄准点距离 ≤ 此值时直接判中（射线失效场景的保底，取值&gt;高速迎面相撞的单帧相对位移）</summary>
    protected const float HitTouchDistance = 0.2f;
    /// <summary>触地判定高度：弹体中心 y ≤ 此值视为触地（生物脚底地面 y≈0，留少许高度让爆炸特效贴在地面之上而非沉入地里）</summary>
    protected const float GroundHitY = 0.05f;
    #endregion

    #region 字段
    /// <summary>弹体贴图朝向修正基准角（发射时取武器 StartRotate 写入的 visualStartAngle，斜射俯仰角在其上叠加）</summary>
    protected float baseVisualAngle;
    /// <summary>瞄准点相对目标脚底的上抬高度（发射时取攻击模式配置 other_data 键 aim_up，默认0=瞄脚底）</summary>
    protected float aimUpHeight;
    #endregion

    #region 初始化外形
    /// <summary>
    /// 初始化攻击外形：基类流程后开启速度朝向（火球/冰球 billboard shader 按飞行速度旋转、贴图头对准飞行方向，斜射弹体视觉"低头"下压；
    /// ResetVisualParams 每发复位该标记，故须逐发重写）
    /// </summary>
    public override void InitAttackModeShow()
    {
        base.InitAttackModeShow();
        visualVelocityOrient = true;
    }
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击-生物：缓存朝向基准角与瞄准上抬高度，并按「出手点→瞄准点」重算首发方向（基类算的是攻击者脚底→目标，丢了出手高度，不配的话第一帧会先平飞）
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        baseVisualAngle = visualStartAngle;
        aimUpHeight = attackModeInfo.GetAimUpHeight();
        RefreshTrackDirection();
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 收集本帧射线检测请求：全程入队（目标死亡后仍要命中路径上的敌人，检测不随目标死亡关闭）；入队前同步最新方向与 Update 保持一致
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        RefreshTrackDirection();
        EnqueueSingleRay(batch);
    }

    /// <summary>
    /// 更新处理：目标存活则方向实时跟踪（含 Y），死亡则冻结方向直飞死亡点；贴身保底命中→射线命中→移动→边界
    /// </summary>
    public override void Update()
    {
        RefreshTrackDirection();
        //弹体朝向跟随飞行方向（X-Y 平面俯仰角，叠加武器 StartRotate 基准角）
        Vector3 dir = attackModeData.attackDirection;
        if (Mathf.Abs(dir.x) > 0.001f || Mathf.Abs(dir.y) > 0.001f)
        {
            visualStartAngle = baseVisualAngle + Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        //贴身保底命中：与瞄准点距离足够近直接判中（短射线在目标迎面相撞穿进碰撞体后永久失效，不穿这一层会挂在目标身上空转）
        if (attacked != null && !attacked.IsDead() && attacked.creatureObj != null)
        {
            float disToAim = Vector3.Distance(position, attacked.creatureObj.transform.position + Vector3.up * aimUpHeight);
            if (disToAim <= HitTouchDistance)
            {
                HandleForHitTarget(attacked);
                return;
            }
        }
        //全程命中检测（含目标死亡后：命中路径上其他敌人）
        FightCreatureEntity hitCreature = CheckHitTargetForSingle();
        if (hitCreature != null)
        {
            HandleForHitTarget(hitCreature);
            return;
        }
        //移动处理
        HandleForMove();
        //触地即毁：仅下落中判定（上升/水平飞行不误判），到达地面高度播命中特效回收（无伤害结算——目标死亡直飞等无目标场景在此收尾，防穿地）
        if (attackModeData.attackDirection.y < 0f && position.y <= GroundHitY)
        {
            HandleForHitGround();
            return;
        }
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 触地处理：播放命中特效后回收弹道（无伤害结算——纯视觉收尾；命中音效 sound_hit 由受击生物侧播放，触地无受击者不播）
    /// </summary>
    protected virtual void HandleForHitGround()
    {
        PlayEffectForHit(position);
        Destroy();
    }

    /// <summary>
    /// 处理击中生物之后的逻辑：命中点范围检测，范围内敌人全部结算伤害（AOE），随后回收弹道
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fightCreatureEntity)
    {
        //播放击中粒子特效
        PlayEffectForHit(position);
        //命中点范围检测，范围内敌人全部命中（复用基类AOE：hit_max>0 时近者优先截断+同生物去重，0=不限）
        CheckHitTargetArea(position, (targetCreature) =>
        {
            targetCreature.UnderAttack(this);
        });
        Destroy();
    }

    /// <summary>
    /// 刷新跟踪方向：目标存活时方向=当前位置→瞄准点（目标脚底+aim_up 上抬，含 Y 不拍平）；目标死亡则保持上一帧方向（朝死亡点直飞）
    /// </summary>
    protected void RefreshTrackDirection()
    {
        if (attacked != null && !attacked.IsDead() && attacked.creatureObj != null)
        {
            attackModeData.attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position + Vector3.up * aimUpHeight - position);
        }
    }
    #endregion
}
