using UnityEngine;

/// <summary>
/// 条件触发-伤害吸血（SSR稀有度BUFF「吸一吸」）：自身造成伤害时按rate转化为自身生命，
/// 走正式 RegainHP 治疗路径（真实回血>0会派发生物回复事件，可喂养治疗计数类BUFF）；
/// 按 attackerId 判定，BUFF来源伤害（反伤/再攻击等）同样触发吸血；AOE每个目标各触发一次。
/// </summary>
public class BuffEntityConditionalLifeSteal : BuffEntityConditional
{
    #region 事件回调
    /// <summary>
    /// 事件触发-被攻击（整体重写做归属过滤：仅自己打出的伤害才吸血；不调base避免空pre_info时全场攻击都累积条件值）
    /// </summary>
    public override void EventForUnderAttack(FightUnderAttackBean fightUnderAttack)
    {
        if (buffEntityData == null || buffEntityData.isValid == false) return;
        //必须是自己打出的伤害；被打者是自己时不处理
        if (!fightUnderAttack.attackerId.Equals(buffEntityData.targetCreatureUUId)) return;
        if (fightUnderAttack.attackedId.Equals(buffEntityData.targetCreatureUUId)) return;
        var selfCreature = GetFightCreatureEntityForTarget();
        if (selfCreature == null || selfCreature.fightCreatureData == null || selfCreature.IsDead()) return;
        //吸血量=名义伤害×rate
        float rate = buffEntityData.GetTriggerValueRate();
        int healHP = (int)(fightUnderAttack.attackerDamage * rate);
        if (healHP <= 0) return;
        //触发判定（几率+粒子）
        if (!TriggerBuffConditional(buffEntityData)) return;
        selfCreature.RegainHP(buffEntityData.targetCreatureUUId, buffEntityData.targetCreatureUUId, healHP);
    }
    #endregion
}
