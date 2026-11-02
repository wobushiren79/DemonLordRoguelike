using System.Collections.Generic;

/// <summary>
/// 远程穿透弹道-沿路碾压（深渊馈赠「失控的矿车」）
/// <para>沿所在道路从左向右直线行驶（方向+x），穿透整条路、每个目标只撞一次（复用穿透弹的射线批处理与去重），穿透数无上限；</para>
/// <para>伤害固定：每个被撞目标均按攻击模块数据的 attackerDamage 全额结算，不随命中数递减；</para>
/// <para>行驶到道路尽头（x ≥ 0.5+道路长度+余量）即销毁消失。</para>
/// </summary>
public class AttackModeRangedPiercingRoad : AttackModeRangedPiercing
{
    #region 字段
    /// <summary>路尽头世界X（StartAttackBase时按当场战斗道路长度计算，越过即销毁）</summary>
    protected float roadEndPosX = 15f;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：追加计算路尽头X（两条StartAttack路径都经由此处，对象池复用不残留）
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
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 更新处理：对本帧命中的每个未撞过的目标结算固定伤害，不设穿透数上限，驶出道路尽头即销毁
    /// </summary>
    public override void Update()
    {
        List<FightCreatureEntity> listHitTarget = CheckHitTarget();
        if (!listHitTarget.IsNull())
        {
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
    /// 处理击中生物之后的逻辑：按攻击模块数据的固定伤害结算，矿车不销毁继续行驶
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fghtCreatureEntity)
    {
        fghtCreatureEntity.UnderAttack(this);
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
