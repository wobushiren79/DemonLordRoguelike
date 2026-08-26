using System;
using UnityEngine;

public class AttackModeFalluponArea : BaseAttackMode
{
    private bool isFalling = false;
    private Action<BaseAttackMode> actionForAttackEnd;
    /// <summary>
    /// 当前下落速度，每帧由重力加速度(attackModeInfo.GetGravity(),other_data 键 gravity,默认 9.81)累加，使下落轨迹呈非线性
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
    /// 更新-处理下落移动，到达地面时触发范围攻击
    /// </summary>
    public override void Update()
    {
        base.Update();
        if (!isFalling) return;

        //gameObject 消失的销毁兜底仅限老 prefab 模式；DSP 视觉模式(visual_name 非空)本就无实体，继续走 position 权威源下落
        if (attackModeInfo.visual_name.IsNull() && gameObject == null)
        {
            AttackHandle();
            return;
        }

        float deltaTime = GameFightLogic.GetFightDeltaTime();
        TranslatePosition(Vector3.down * (deltaTime * currentFallSpeed));
        currentFallSpeed += attackModeInfo.GetGravity() * deltaTime;

        if (attackModeData.targetPos.y >= position.y)
        {
            SetPosition(attackModeData.targetPos);
            isFalling = false;
            AttackHandle();
        }
    }
    #endregion

    #region 速度钳制基准
    /// <summary>
    /// 瞬移钳制基准=当前下落速度：本类为重力加速弹道，末速远超 speed_move 初值，
    /// 沿用基类(配置速度×攻速)会把后半程每帧差分速度误判为瞬移清零，DSP 火星甩尾消失
    /// </summary>
    public override float GetVelocityClampSpeed()
    {
        return currentFallSpeed;
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
