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
    /// 更新处理
    /// </summary>
    public override void Update()
    {
        //如果还存在目标
        if (attacked != null && !attacked.IsDead())
        {
            //实时改变方向
            attackModeData.attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - gameObject.transform.position);
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
        gameObject.transform.Translate(attackModeData.attackDirection * Time.deltaTime * attackModeInfo.speed_move);
    }

}
