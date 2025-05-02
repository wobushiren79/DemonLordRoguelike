using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRangedTracking :  AttackModeRanged
{
    public GameFightCreatureEntity attacked;

    /// <summary>
    /// 开始攻击
    /// </summary>
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if(attacked != null && !attacked.IsDead())
        {        
            this.attacked = attacked;
        }
        else
        {
            Destory();
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
            attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - gameObject.transform.position);
            //高度不变
            attackDirection = attackDirection.SetY(0);
            //检测是否击中目标
            GameFightCreatureEntity gameFightCreatureEntity = CheckHitTarget();
            if (gameFightCreatureEntity != null)
            {
                HandleForHitTarget(gameFightCreatureEntity);
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
        gameObject.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
    }

}
