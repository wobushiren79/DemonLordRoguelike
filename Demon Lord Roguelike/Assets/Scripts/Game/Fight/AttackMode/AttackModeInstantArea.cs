using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击模式-瞬时落点范围：无弹道飞行，StartAttack 当帧立即对目标点(targetPos)做一次范围攻击。
/// <para>支持配置 hit_max 单次命中目标数上限（&gt;0 时按距落点近者优先截断，保证落点主目标必中；0=不限）。</para>
/// <para>支持发射方注入 <see cref="filterCreatureIds"/> 快照名单（非空时只命中名单内生物）——
/// 供「触发瞬间快照全场、间隔期新刷敌人不受波及」类攻击（如深渊馈赠-闪电）使用，
/// 写法照 <see cref="AttackModeRangedSplitChild.targetRoad"/> 先例：StartAttack 前写入、Destroy 时置空防对象池残留。</para>
/// </summary>
public class AttackModeInstantArea : BaseAttackMode
{
    #region 字段
    /// <summary>快照过滤名单（发射方在 StartAttack 前写入；null/空=不过滤命中所有）</summary>
    public HashSet<string> filterCreatureIds;
    #endregion

    #region 攻击入口
    /// <summary>
    /// 攻击-基础（纯数据发射路径）
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        AttackHandle();
    }

    /// <summary>
    /// 攻击-生物
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            Destroy();
            return;
        }
        AttackHandle();
        actionForAttackEnd?.Invoke(this);
    }
    #endregion

    #region 攻击处理
    /// <summary>
    /// 立即对落点做范围攻击：快照过滤 + 命中上限截断 + 逐个 UnderAttack，最后播放命中特效并回收
    /// </summary>
    public virtual void AttackHandle()
    {
        Vector3 centerPos = attackModeData.targetPos;
        int hitMax = attackModeInfo.hit_max;
        Collider[] targetColliders = GetHitTargetAreaCollider(centerPos);
        if (!targetColliders.IsNull())
        {
            //命中数受限时按距落点近远升序（截断时近者优先，保证落点主目标必中）
            if (hitMax > 0)
            {
                Array.Sort(targetColliders, (a, b) =>
                    (a.transform.position - centerPos).sqrMagnitude.CompareTo((b.transform.position - centerPos).sqrMagnitude));
            }
            //循环外缓存 GameFightLogic，避免每个 collider 命中都做一次查询
            GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
            int hitNum = 0;
            for (int i = 0; i < targetColliders.Length; i++)
            {
                //单次命中目标数达上限则停止
                if (hitMax > 0 && hitNum >= hitMax)
                    break;
                string creatureId = targetColliders[i].gameObject.name;
                //快照名单非空时只命中名单内生物（快照期之后新刷的目标不受波及）
                if (filterCreatureIds != null && filterCreatureIds.Count > 0 && !filterCreatureIds.Contains(creatureId))
                    continue;
                var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, searchCreatureType);
                if (targetCreature == null || targetCreature.IsDead())
                    continue;
                targetCreature.UnderAttack(this);
                hitNum++;
            }
        }
        //命中特效（无论是否命中目标都播，落点表现）
        PlayHitEffect(centerPos);
        Destroy();
    }

    /// <summary>
    /// 播放落点命中特效（默认走配置 effect_hit；子类可换专用特效通道）
    /// </summary>
    protected virtual void PlayHitEffect(Vector3 centerPos)
    {
        PlayEffectForHit(centerPos);
    }

    /// <summary>
    /// 清理状态（对象池复用前清空注入名单，防残留）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        filterCreatureIds = null;
        base.Destroy(isPermanently);
    }
    #endregion
}
