using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRangedTracking :  AttackModeRanged
{
    public FightCreatureEntity attacked;
    /// <summary>目标身份快照（实体池复用后 UUId 会变，防把复用成的新生物误判为存活目标继续追踪）</summary>
    protected string attackedUUId;

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
            attackedUUId = attacked.fightCreatureData?.creatureData?.creatureUUId;
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
        if (CheckTargetEntityValid(attacked, attackedUUId))
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
        //如果还存在目标（UUId 双判防实体池复用成新生物后误追踪）
        if (CheckTargetEntityValid(attacked, attackedUUId))
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

    /// <summary>
    /// 回收：清空跟踪目标与其身份快照，防对象池复用残留
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        attacked = null;
        attackedUUId = null;
        base.Destroy(isPermanently);
    }
}
