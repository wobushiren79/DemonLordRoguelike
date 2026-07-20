using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRangedTracking :  AttackModeRanged
{
    public FightCreatureEntity attacked;

    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if(attacked != null && !attacked.IsDead())
        {        
            this.attacked = attacked;
        }
        else
        {
            Destroy();
        }
    }

    /// <summary>
    /// 收集本帧射线检测请求：先按当前位置实时更新朝向目标的方向，再入队射线（与 Update 内的方向计算保持一致）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        //仅当目标仍存活时才检测（与 Update 一致）
        if (attacked != null && !attacked.IsDead())
        {
            attackModeData.attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - position).SetY(0);
            EnqueueSingleRay(batch);
        }
    }

    /// <summary>
    /// 更新处理
    /// </summary>
    public override void Update()
    {
        //如果还存在目标
        if (attacked != null && !attacked.IsDead())
        {
            //实时改变方向
            attackModeData.attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - position);
            //高度不变
            attackModeData.attackDirection = attackModeData.attackDirection.SetY(0);
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
    /// 移动处理
    /// </summary>
    public override void HandleForMove()
    {
        TranslatePosition(attackModeData.attackDirection * GameFightLogic.GetFightDeltaTime() * GetMoveSpeed());
    }

}
