using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged : BaseAttackMode
{
    /// <summary>
    /// 开始攻击
    /// </summary>
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        actionForAttackEnd?.Invoke(this);
    }

    /// <summary>
    /// 更新处理
    /// </summary>
    public override void Update()
    {
        base.Update();
        if (CheckHitTarget(out GameFightCreatureEntity gameFightCreatureEntity))
        {
            HandleForHitTarget(gameFightCreatureEntity);
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual bool CheckHitTarget(out GameFightCreatureEntity gameFightCreatureEntity)
    {
        gameFightCreatureEntity = null;
        RayUtil.RayToCast(gameObject.transform.position, attackDirection, attackModeInfo.collider_size, 1 << attackedLayer, out RaycastHit hit);
        if (hit.collider != null)
        {
            string creatureId = hit.collider.gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureTypeEnum.None);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                gameFightCreatureEntity = targetCreature;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 处理击中生物之后的逻辑
    /// </summary>
    public virtual void HandleForHitTarget(GameFightCreatureEntity gameFightCreatureEntity)
    {
        //扣血
        gameFightCreatureEntity.UnderAttack(this);
        //攻击完了就回收这个攻击
        Destory();
    }

    /// <summary>
    /// 移动处理
    /// </summary>
    public virtual void HandleForMove()
    {
        gameObject.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
    }

    /// <summary>
    /// 边界处理 飞太远的情况
    /// </summary>
    public virtual void HandleForBound()
    {
        if (gameObject.transform.position.x > 15 || gameObject.transform.position.x < -5 || gameObject.transform.position.y < -5)
        {
            Destory();
        }
    }
}
