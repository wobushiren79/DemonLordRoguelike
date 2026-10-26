using System;
using UnityEngine;

/// <summary>
/// 追踪远程弹道（水平追踪）：发射时锁定目标，飞行中每帧把方向重算为"弹道→目标当前位置"（Y 拍平，纯水平追踪）；
/// 目标死亡或被对象池复用（UUId 快照不符）时，沿本方进攻方向重锁最近的存活敌人继续追——防守方向右、进攻方向左；
/// 该方向再无敌人则保持最后方向直飞，由边界判定回收（不设存活时长）。
/// <para>贴身保底：与锁定目标的水平距离 ≤ HitTouchDistance 直接判中——命中射线只有 collider_size(0.1) 长，
/// 目标迎面相撞穿进碰撞体后射线打不到背面，不补这层会绕着目标空转。</para>
/// <para>重锁按 RetargetInterval 节流：无敌人时不做全量索敌，避免每帧遍历全部生物；热路径（目标有效）零分配。</para>
/// </summary>
public class AttackModeRangedTracking :  AttackModeRanged
{
    #region 常量
    /// <summary>贴身判定距离（水平距离，Y 拍平）：弹体与锁定目标距离 ≤ 此值直接判中</summary>
    protected const float HitTouchDistance = 0.25f;
    /// <summary>重锁失败后的重试间隔（秒，跟随战斗时钟）：节流全量索敌，避免无目标弹道每帧遍历全场</summary>
    protected const float RetargetInterval = 0.3f;
    #endregion

    #region 字段
    /// <summary>当前锁定的目标</summary>
    public FightCreatureEntity attacked;
    /// <summary>目标身份快照（实体池复用后 UUId 会变，防把复用成的新生物误判为存活目标继续追踪）</summary>
    protected string attackedUUId;
    /// <summary>重锁计时（秒，跟随战斗时钟）：距上次重锁失败累计的时间，达到 RetargetInterval 才再索敌一次</summary>
    protected float retargetTimer;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击-纯数据发射：无跟踪目标直接销毁（本弹道必须挂目标发射）
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击-生物：锁定目标并记录 UUId 快照；目标已死/为空时沿进攻方向重锁，重锁不到才销毁（防出手动画期间目标死亡造成空放）
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        this.attacked = null;
        attackedUUId = null;
        //首发允许立即重锁（重锁失败后才按间隔节流）
        retargetTimer = RetargetInterval;
        if (attacked != null && !attacked.IsDead())
        {
            LockTarget(attacked);
        }
        else if (!TryRetarget())
        {
            Destroy();
        }
    }
    #endregion

    #region 目标锁定
    /// <summary>
    /// 锁定目标并记录身份快照
    /// </summary>
    protected void LockTarget(FightCreatureEntity target)
    {
        attacked = target;
        attackedUUId = target?.fightCreatureData?.creatureData?.creatureUUId;
    }

    /// <summary>
    /// 确认当前目标仍有效，失效则尝试重锁
    /// </summary>
    protected bool EnsureTarget()
    {
        if (CheckTargetEntityValid(attacked, attackedUUId))
            return true;
        return TryRetarget();
    }

    /// <summary>
    /// 沿本方进攻方向重锁最近的存活敌人（该方向无目标返回 false，由调用方保持直飞）
    /// <para>按 RetargetInterval 节流：失败后 0.3 秒内不再做全量索敌。</para>
    /// </summary>
    protected bool TryRetarget()
    {
        if (retargetTimer < RetargetInterval)
            return false;
        retargetTimer = 0f;
        //DisMinByAll 按 direction 符号过滤 x 前后半场：向右=取 x≥弹道x 的敌人，向左=取 x≤弹道x 的敌人
        var listData = FightCreatureSearchUtil.FindCreatureEntity(
            CreatureSearchType.DisMinByAll,
            searchCreatureType,
            position,
            GetRetargetDirection(),
            Vector3.zero);
        if (listData.IsNull())
            return false;
        LockTarget(listData[0]);
        return true;
    }

    /// <summary>
    /// 重锁搜索方向：searchCreatureType 是"被打者"阵营——打进攻方（我方防守）向右，打防守方（我方进攻）向左
    /// </summary>
    protected Vector3 GetRetargetDirection()
    {
        return searchCreatureType == CreatureFightTypeEnum.FightAttack ? Vector3.right : Vector3.left;
    }

    /// <summary>
    /// 与锁定目标的水平距离（Y 拍平：弹道在 y≈0.7 起飞、目标脚底 y=0，不拍平永远够不到贴身阈值）
    /// </summary>
    protected float GetHorizontalDistanceToTarget()
    {
        Vector3 delta = attacked.creatureObj.transform.position - position;
        delta.y = 0f;
        return delta.magnitude;
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 收集本帧射线检测请求：先确认/重锁目标，再按当前位置实时更新朝向目标的方向，最后入队射线（与 Update 内方向计算保持一致）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        //仅当目标有效（含本帧重锁成功）时才检测
        if (EnsureTarget())
        {
            //先拍平 Y 再归一化：顺序反了会先归一化再抹掉 Y 分量，方向长度<1 导致贴近目标时移动变慢
            attackModeData.attackDirection = (attacked.creatureObj.transform.position - position).SetY(0).normalized;
            EnqueueSingleRay(batch);
        }
    }

    /// <summary>
    /// 更新处理：目标有效则实时转向并检测命中（先贴身保底、再射线）；否则保持最后方向直飞
    /// </summary>
    public override void Update()
    {
        //重锁计时：跟随战斗时钟（2倍速/暂停同步），只累计到间隔上限
        if (retargetTimer < RetargetInterval)
            retargetTimer += GameFightLogic.GetFightDeltaTime();
        //如果还存在目标（UUId 双判防实体池复用成新生物后误追踪）
        if (CheckTargetEntityValid(attacked, attackedUUId))
        {
            //实时改变方向（先拍平 Y 再归一化：顺序反了会让方向长度随距离缩短而衰减，表现为越靠近敌人越慢）
            attackModeData.attackDirection = (attacked.creatureObj.transform.position - position).SetY(0).normalized;
            //贴身保底命中：短射线在目标迎面相撞穿进碰撞体后打不到背面，不补这层会绕点空转
            if (GetHorizontalDistanceToTarget() <= HitTouchDistance)
            {
                HandleForHitTarget(attacked);
                return;
            }
            //检测是否击中目标
            FightCreatureEntity FightCreatureEntity = CheckHitTargetForSingle();
            if (FightCreatureEntity != null)
            {
                HandleForHitTarget(FightCreatureEntity);
                return;
            }
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 移动处理：转向时方向已归一化为纯水平单位向量，恒速飞行（不随与目标的距离衰减）
    /// </summary>
    public override void HandleForMove()
    {
        TranslatePosition(attackModeData.attackDirection * GameFightLogic.GetFightDeltaTime() * GetMoveSpeed());
    }

    /// <summary>
    /// 回收：清空跟踪目标与其身份快照，防对象池复用残留
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        attacked = null;
        attackedUUId = null;
        retargetTimer = 0f;
        base.Destroy(isPermanently);
    }
    #endregion
}
