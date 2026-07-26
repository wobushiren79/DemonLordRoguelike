using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 远程穿透弹道-沿路碾压（深渊馈赠「失控的矿车」）
/// <para>沿所在道路从左向右直线行驶（方向+x），穿透整条路、每个目标只撞一次（复用穿透弹的射线批处理与去重），穿透数无上限；</para>
/// <para>伤害逐目标递减：第1个被撞目标按攻击模块数据的 attackerDamage 全额结算，之后每多撞一个伤害减半（向下取整、保底1点）；</para>
/// <para>行驶到道路尽头（x ≥ 0.5+道路长度+余量）即销毁消失。</para>
/// </summary>
public class AttackModeRangedPiercingRoad : AttackModeRangedPiercing
{
    #region 字段
    /// <summary>路尽头世界X（StartAttackBase时按当场战斗道路长度计算，越过即销毁）</summary>
    protected float roadEndPosX = 15f;
    /// <summary>已撞目标数（伤害递减计数，供调试查看）</summary>
    protected int hitNum = 0;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：追加计算路尽头X并清零递减计数（两条StartAttack路径都经由此处，对象池复用不残留）
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        var gameLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (gameLogic != null && gameLogic.fightData != null)
        {
            //路右端=地面右缘(0.5+路长)，再加0.5余量保证驶出画面外才消失
            roadEndPosX = 0.5f + gameLogic.fightData.sceneRoadLength + 0.5f;
        }
        else
        {
            roadEndPosX = 15f;
        }
        hitNum = 0;
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 更新处理：对本帧命中的每个未撞过的目标按「先近后远」依次结算并递减伤害，不设穿透数上限，驶出道路尽头即销毁
    /// </summary>
    public override void Update()
    {
        List<FightCreatureEntity> listHitTarget = CheckHitTarget();
        if (!listHitTarget.IsNull())
        {
            //同帧撞多个时按x从近到远依次结算（矿车向右行驶，先撞到的x更小），保证伤害递减顺序正确
            listHitTarget.Sort((a, b) => a.creatureObj.transform.position.x.CompareTo(b.creatureObj.transform.position.x));
            for (int i = 0; i < listHitTarget.Count; i++)
            {
                var itemCreature = listHitTarget[i];
                string itemCreatureId = itemCreature.fightCreatureData.creatureData.creatureUUId;
                //已撞过的目标跳过，避免重复扣血
                if (listPierceCreature.Contains(itemCreatureId))
                {
                    continue;
                }
                HandleForHitTarget(itemCreature);
                listPierceCreature.Add(itemCreatureId);
            }
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 处理击中生物之后的逻辑：按当前伤害结算一次，随后伤害减半（保底1点），矿车不销毁继续行驶
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fghtCreatureEntity)
    {
        //按当前伤害结算（第1个目标=满额伤害）
        fghtCreatureEntity.UnderAttack(this);
        //伤害逐目标减半，保底1点
        hitNum++;
        attackModeData.attackerDamage = Math.Max(1, attackModeData.attackerDamage / 2);
    }

    /// <summary>
    /// 检测是否到达边界：驶过本路尽头即视为越界（覆盖基类的全图范围判定）
    /// </summary>
    public override bool CheckIsMoveBound()
    {
        return position.x >= roadEndPosX;
    }
    #endregion
}
