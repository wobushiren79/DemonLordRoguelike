using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回复生命+护甲（愈之魅魔攻击模式 500001：总回复量一份=ATK×damage_add_rate 快照，先补生命、血满后余量补护甲[串联分配，镜像受击先甲后血]，命中特效只播一次；
/// 入选/出手校验=血量或护甲任一不满，与索敌 CreatureSearchType.RoadAllyHPOrDRNoMax(12) 同口径）
/// </summary>
public class AttackModeRegainHPAndDR : AttackModeRegain
{
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
}
