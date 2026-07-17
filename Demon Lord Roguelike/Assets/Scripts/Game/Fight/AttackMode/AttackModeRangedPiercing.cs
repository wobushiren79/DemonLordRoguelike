using System;
using System.Collections.Generic;

/// <summary>
/// 远程穿透弹道：直线飞行，可依次穿透多个目标，每个目标只打一次，穿满 numPierceMax 个后销毁。
/// <para>命中检测复用 <see cref="AttackModeRanged"/> 的单射线批处理（穿透靠读同一条射线的多命中窗口，
/// 上限见 <see cref="FightRaycastBatch.MaxHitsPerRay"/>）。</para>
/// </summary>
public class AttackModeRangedPiercing : AttackModeRanged
{
    #region 字段
    /// <summary>最大穿透数量（穿满即销毁）</summary>
    public int numPierceMax = 3;
    /// <summary>本发已穿透过的生物ID（去重，避免同一目标被反复扣血）</summary>
    public HashSet<string> listPierceCreature;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击-默认
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        listPierceCreature = new HashSet<string>();
    }

    /// <summary>
    /// 开始攻击-生物
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        listPierceCreature = new HashSet<string>();
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 更新处理：对本帧命中的每个未穿透过的目标扣血，穿满即销毁，否则继续飞行
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
                //已穿透过的目标跳过，避免重复扣血
                if (listPierceCreature.Contains(itemCreatureId))
                {
                    continue;
                }
                HandleForHitTarget(itemCreature);
                listPierceCreature.Add(itemCreatureId);
                if (listPierceCreature.Count >= numPierceMax)
                {
                    Destroy();
                    return;
                }
            }
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 处理击中生物之后的逻辑：只扣血不销毁（穿透弹继续飞，销毁由 Update 里的穿透数上限判定）
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fghtCreatureEntity)
    {
        //扣血
        fghtCreatureEntity.UnderAttack(this);
    }
    #endregion
}
