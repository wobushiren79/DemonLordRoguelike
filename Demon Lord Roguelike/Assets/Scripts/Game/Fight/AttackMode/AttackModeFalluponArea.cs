using System;
using UnityEngine;

public class AttackModeFalluponArea : BaseAttackMode
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
        if (gameObject != null)
        {
            float randomOffsetX = UnityEngine.Random.Range(-0.1f, 0.1f);
            float randomOffsetZ = UnityEngine.Random.Range(-0.1f, 0.1f);
            gameObject.transform.position = new Vector3(attackModeData.targetPos.x + randomOffsetX, attackModeData.startPos.y, attackModeData.targetPos.z + randomOffsetZ);
        }
        currentFallSpeed = attackModeInfo.speed_move;
        isFalling = true;
    }

    /// <summary>
    /// 更新-处理下落移动，到达地面时触发范围攻击
    /// </summary>
    public override void Update()
    {
        base.Update();
        if (!isFalling) return;

        if (gameObject == null)
        {
            AttackHandle();
            return;
        }

        gameObject.transform.Translate(Vector3.down * (Time.deltaTime * currentFallSpeed));
        currentFallSpeed += GravityAcceleration * Time.deltaTime;

        if (attackModeData.targetPos.y >= gameObject.transform.position.y)
        {
            gameObject.transform.position = attackModeData.targetPos;
            isFalling = false;
            AttackHandle();
        }
    }
    #endregion

    #region 攻击处理
    /// <summary>
    /// 到达地面后触发范围攻击
    /// </summary>
    public void AttackHandle()
    {
        CheckHitTargetArea(attackModeData.targetPos, (targetFightCreatureEntity) =>
        {
            targetFightCreatureEntity.UnderAttack(this);
        });
        PlayEffectForHit(attackModeData.targetPos);
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
