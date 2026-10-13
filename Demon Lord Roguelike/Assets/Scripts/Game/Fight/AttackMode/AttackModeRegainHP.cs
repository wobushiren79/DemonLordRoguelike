using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单体回复生命（策略池保留类；当前无配置使用者——愈之魅魔 500001 已改为血甲双补 <see cref="AttackModeRegainHPAndDR"/>，原 500001/500002 配置不再指向本类）
/// </summary>
public class AttackModeRegainHP : AttackModeRegain
{
    public override void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {
        base.HandleRegain(attacker, attacked);
        attacked.RegainHP(this);
    }

    /// <summary>
    /// HP 回复校验：目标当前血量不满才可回复（int 截断口径，见 FightCreatureBean.IsHPNotFull）
    /// </summary>
    public override bool CheckTargetRegainable(FightCreatureEntity attacked)
    {
        var fightData = attacked.fightCreatureData;
        return fightData != null && fightData.IsHPNotFull();
    }
}
