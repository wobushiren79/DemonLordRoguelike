using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class AttackModeRangedArea : AttackModeRanged
{
    public override void HandleForHitTarget(GameFightCreatureEntity gameFightCreatureEntity)
    {
        //播放击中粒子特效
        PlayEffectForHit(gameObject.transform.position);
        //检测范围内的敌人
        CheckHitTargetArea(gameObject.transform.position, (targetCreature) =>
        {
            targetCreature.UnderAttack(this);
        });
        Destroy();
    }
}
