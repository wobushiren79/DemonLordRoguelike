using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged​Split : BaseAttackMode
{
    public List<GameObject> childObjs;
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        //分裂2个子攻击
        childObjs = new List<GameObject>();

        actionForAttackEnd?.Invoke(this);
    }

    public override void Update()
    {
        base.Update();
    }

    /// <summary>
    /// 移动处理
    /// </summary>
    public virtual void HandleForItemMove(GameObject targetObj, int targetRoad)
    {
        if (targetObj == null)
        {
            return;
        }
        //如果已经到了目标路径
        if (Math.Abs(targetObj.transform.position.z - targetRoad) < 0.02f)
        {
            gameObject.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
        }
        //如果还没到目标路径   
        else
        {

        }
    }

    /// <summary>
    /// 边界处理 飞太远的情况
    /// </summary>
    public virtual void HandleForBound()
    {
        if (gameObject.transform.position.x > 15 || gameObject.transform.position.x < -5 ||
            gameObject.transform.position.y < -5 || gameObject.transform.position.y > 15)
        {
            Destroy();
        }
    }
}
