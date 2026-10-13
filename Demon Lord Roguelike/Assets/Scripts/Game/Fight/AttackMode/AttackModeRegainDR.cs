using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单体回复护甲（策略池保留类；当前无配置使用者——盾之魅魔已移除、原 500002 配置已删，愈之魅魔血甲双补走 <see cref="AttackModeRegainHPAndDR"/>）
/// </summary>
public class AttackModeRegainDR : AttackModeRegain
{
    public override void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {
        base.HandleRegain(attacker, attacked);
        attacked.RegainDR(this);
    }

    /// <summary>
    /// DR 回复校验：目标当前护甲不满才可回复（int 截断口径，见 FightCreatureBean.IsDRNotFull）
    /// </summary>
    public override bool CheckTargetRegainable(FightCreatureEntity attacked)
    {
        var fightData = attacked.fightCreatureData;
        return fightData != null && fightData.IsDRNotFull();
    }
}
