using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRangedArc : AttackModeRanged
{
    //抛物线高度
    public float arcHeight = 3f;

    //当前抛物线分段进度(0~1；protected 供弹跳类子类重置分段)
    protected float progress = 0f;

    public override void StartAttack()
    {
        base.StartAttack();
        progress = 0f;
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        progress = 0f;
    }

    /// <summary>
    /// 收集本帧射线检测请求：抛物线前半程(progress<0.5)不检测，故不入队
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        if (progress < 0.5f)
            return;
        EnqueueSingleRay(batch);
    }

    /// <summary>
    /// 检测碰撞
    /// </summary>
    /// <returns></returns>
    public override FightCreatureEntity CheckHitTargetForSingle()
    {
        //前面抛物线不检测
        if (progress < 0.5f)
        {
            return null;
        }
        return base.CheckHitTargetForSingle();
    }

    /// <summary>
    /// 处理移动
    /// </summary>
    public override void HandleForMove()
    {
        if (progress < 1f)
        {
            progress += GameFightLogic.GetFightDeltaTime() * GetMoveSpeed();

            // 计算抛物线路径
            float parabola = 1.0f - 4.0f * (progress - 0.5f) * (progress - 0.5f);
            Vector3 nextPos = Vector3.Lerp(attackModeData.startPos, attackModeData.targetPos, progress);
            nextPos.y += parabola * arcHeight;

            SetPosition(nextPos);
        }
        else
        {
            // 到达终点
            SetPosition(attackModeData.targetPos);
            HandleForReachEnd();
        }
    }

    /// <summary>
    /// 到达抛物线终点处理：默认直接回收；子类可覆盖为落点范围命中等收尾逻辑
    /// </summary>
    protected virtual void HandleForReachEnd()
    {
        //攻击完了就回收这个攻击
        Destroy();
    }

}
