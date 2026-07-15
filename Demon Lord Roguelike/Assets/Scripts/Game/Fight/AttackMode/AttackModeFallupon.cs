using System;
using UnityEngine;

public class AttackModeFallupon : BaseAttackMode
{
    /// <summary>
    /// 重力加速度（m/s²），用于模拟下落时的加速效果
    /// </summary>
    private const float GravityAcceleration = 9.81f;

    private bool isFalling = false;
    private Action<BaseAttackMode> actionForAttackEnd;
    /// <summary>
    /// 当前下落速度，每帧由重力加速度累加，使下落轨迹呈非线性
    /// </summary>
    private float currentFallSpeed;

    #region 攻击入口
    /// <summary>
    /// 攻击-基础
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        StartFalling();
    }

    /// <summary>
    /// 攻击-生物
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            Destroy();
            return;
        }
        this.actionForAttackEnd = actionForAttackEnd;
        StartFalling();
    }
    #endregion

    #region 下落逻辑
    /// <summary>
    /// 将gameObject定位到目标正上方（高度取attackModeData.startPos.y），XZ各加[-0.1,0.1]随机偏移，开始下落
    /// </summary>
    private void StartFalling()
    {
        //定位到目标正上方 + XZ 随机偏移，走 position 权威源(自动同步 transform)
        float randomOffsetX = UnityEngine.Random.Range(-0.1f, 0.1f);
        float randomOffsetZ = UnityEngine.Random.Range(-0.1f, 0.1f);
        SetPosition(new Vector3(attackModeData.targetPos.x + randomOffsetX, attackModeData.startPos.y, attackModeData.targetPos.z + randomOffsetZ));
        currentFallSpeed = attackModeInfo.speed_move;
        isFalling = true;
    }

    /// <summary>
    /// 收集本帧射线检测请求：下落途中在当前位置入队一条单体射线
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        if (!isFalling)
            return;
        EnqueueSingleRay(batch);
    }

    /// <summary>
    /// 更新-处理下落移动，下落途中持续检测单体碰撞与边界，命中或越界时结束攻击
    /// </summary>
    public override void Update()
    {
        base.Update();
        if (!isFalling) return;

        if (gameObject == null)
        {
            EndAttack();
            return;
        }

        //下落途中持续做单体碰撞检测，碰到对象就立即攻击并结束
        FightCreatureEntity fightCreatureEntity = CheckHitTargetForSingle();
        if (fightCreatureEntity != null)
        {
            HandleForHitTarget(fightCreatureEntity);
            return;
        }

        //移动处理
        HandleForMove();
        //边界处理（下落到 y=-5 等越界位置自动结束）
        HandleForBound();
    }
    #endregion

    #region 攻击处理
    /// <summary>
    /// 处理击中单个目标的逻辑：扣血、播放命中特效、结束攻击
    /// </summary>
    public virtual void HandleForHitTarget(FightCreatureEntity fightCreatureEntity)
    {
        fightCreatureEntity.UnderAttack(this);
        PlayEffectForHit(fightCreatureEntity.creatureObj.transform.position);
        EndAttack();
    }

    /// <summary>
    /// 移动处理：向下平移并叠加重力加速度
    /// </summary>
    public virtual void HandleForMove()
    {
        TranslatePosition(Vector3.down * (Time.deltaTime * currentFallSpeed));
        currentFallSpeed += GravityAcceleration * Time.deltaTime;
    }

    /// <summary>
    /// 边界处理：超出地图范围（含 y<-5）则结束
    /// </summary>
    public virtual void HandleForBound()
    {
        if (CheckIsMoveBound())
        {
            EndAttack();
        }
    }

    /// <summary>
    /// 结束攻击：触发回调并销毁自身
    /// </summary>
    private void EndAttack()
    {
        isFalling = false;
        actionForAttackEnd?.Invoke(this);
        Destroy();
    }

    /// <summary>
    /// 清理状态
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        isFalling = false;
        currentFallSpeed = 0f;
        actionForAttackEnd = null;
        base.Destroy(isPermanently);
    }
    #endregion
}
