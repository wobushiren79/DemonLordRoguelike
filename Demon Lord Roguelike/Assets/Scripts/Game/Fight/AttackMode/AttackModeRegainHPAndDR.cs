using System;

/// <summary>
/// 先用回复量补生命，余量再补护甲；愈之魅魔回复选中的友军，勇者技能配置为只回复自身。
/// </summary>
public class AttackModeRegainHPAndDR : AttackModeRegain
{
    #region 释放及回复结算
    /// <summary>
    /// 自疗时将目标设为施法者，沿用血甲缺口校验、回复和特效流程。
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        if (attackModeInfo.IsRegainSelf())
        {
            attacked = attacker;
        }
        base.StartAttack(attacker, attacked, actionForAttackEnd);
    }

    /// <summary>
    /// 定时自疗在施法者缺血或缺甲时才进入施法，血甲全满时保留就绪。
    /// </summary>
    public override bool CheckCanTriggerSkill(FightCreatureEntity attacker, AttackModeInfoBean attackModeInfo)
    {
        if (attackModeInfo?.IsRegainSelf() != true)
        {
            return base.CheckCanTriggerSkill(attacker, attackModeInfo);
        }
        return attacker != null && !attacker.IsDead() && CheckTargetRegainable(attacker);
    }

    /// <summary>
    /// 使用同一份回复量，先补生命，再将余量补给护甲。
    /// </summary>
    public override void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {
        base.HandleRegain(attacker, attacked);
        attacked.RegainHPAndDR(this);
    }

    /// <summary>
    /// 血甲回复校验：目标血量或护甲任一不满才可回复（与索敌 RoadAllyHPOrDRNoMax 筛选同口径，int 截断见 FightCreatureBean.IsHPNotFull/IsDRNotFull）
    /// </summary>
    public override bool CheckTargetRegainable(FightCreatureEntity attacked)
    {
        var fightData = attacked.fightCreatureData;
        return fightData != null && (fightData.IsHPNotFull() || fightData.IsDRNotFull());
    }
    #endregion
}
