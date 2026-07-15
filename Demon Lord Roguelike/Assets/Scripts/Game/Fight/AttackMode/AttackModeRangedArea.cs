using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class AttackModeRangedArea : AttackModeRanged
{
    public override void HandleForHitTarget(FightCreatureEntity fightCreatureEntity)
    {
        //播放击中粒子特效
        PlayEffectForHit(position);
        //检测范围内的敌人
        CheckHitTargetArea(position, (targetCreature) =>
        {
            targetCreature.UnderAttack(this);
        });
        Destroy();
    }
}
