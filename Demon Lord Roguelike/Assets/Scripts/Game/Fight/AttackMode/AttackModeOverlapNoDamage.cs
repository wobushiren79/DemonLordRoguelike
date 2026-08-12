using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无伤害重叠攻击（如烂泥史莱姆粘液减速、毒液史莱姆中毒）：纯DEBUFF触碰变体。
/// 命中目标只附加 buff 字段配置的BUFF并播放命中音效，不掉血、不跳伤害数字、不播受击特效、不进伤害统计。
/// </summary>
public class AttackModeOverlapNoDamage : AttackModeOverlap
{
    /// <summary>
    /// 命中单个目标的处理：无伤害触碰，只上BUFF
    /// </summary>
    protected override void HitTarget(FightCreatureEntity itemAttacked)
    {
        itemAttacked.UnderAttackNoDamage(this);
    }
}
