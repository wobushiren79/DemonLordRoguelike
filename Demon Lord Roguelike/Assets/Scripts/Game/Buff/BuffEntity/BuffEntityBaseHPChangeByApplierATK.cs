using UnityEngine;

/// <summary>
/// 周期掉血BUFF-按施加者攻击力结算（如骷髅魔法师(火)的「烧伤」）：每次触发对目标造成
/// (施加者当前ATK × |trigger_value_rate| + trigger_value) 点伤害；
/// 施加者不在场/已死亡时跳过本次伤害；伤害走正常 UnderAttack 管线（伤害数字/伤害统计/击杀事件）。
/// </summary>
public class BuffEntityBaseHPChangeByApplierATK : BuffBaseEntity
{
    #region 触发
    /// <summary>
    /// 触发BUFF：按施加者攻击力百分比结算一次伤害
    /// </summary>
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuff(buffEntityData);
        if (isTriggerSuccess == false) return false;
        //获取被BUFF影响的目标生物
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null || targetCreature.IsDead())
            return false;
        //获取BUFF施加者（伤害以其当前攻击力为基数）
        var applierCreature = GetFightCreatureEntityForApplier();
        if (applierCreature == null || applierCreature.fightCreatureData == null || applierCreature.IsDead())
            return false;
        //伤害 = 施加者当前攻击力 × 配置rate绝对值 + 配置固定值
        float applierATK = applierCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int damage = (int)(applierATK * Mathf.Abs(buffEntityData.GetTriggerValueRate()) + buffEntityData.GetTriggerValue());
        if (damage <= 0) return false;
        FightUnderAttackBean fightUnderAttackData = FightHandler.Instance.GetFightUnderAttackData(buffEntityData, damage);
        targetCreature.UnderAttack(fightUnderAttackData);
        return true;
    }
    #endregion
}
