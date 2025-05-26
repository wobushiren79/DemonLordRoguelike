using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRangedArc : AttackModeRanged
{
    //抛物线高度
    public float arcHeight = 3f;

    private Vector3 startPosition;
    private float progress = 0f;

    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        startPosition = gameObject.transform.position;
        progress = 0f;
    }

    /// <summary>
    /// 检测碰撞
    /// </summary>
    /// <returns></returns>
    public override GameFightCreatureEntity CheckHitTargetForSingle()
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
            progress += Time.deltaTime * attackModeInfo.speed_move;

            // 计算抛物线路径
            float parabola = 1.0f - 4.0f * (progress - 0.5f) * (progress - 0.5f);
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPos, progress);
            nextPos.y += parabola * arcHeight;

            gameObject.transform.position = nextPos;
        }
        else
        {
            // 到达终点
            gameObject.transform.position = targetPos;
            //攻击完了就回收这个攻击
            Destroy();
        }
    }

}
