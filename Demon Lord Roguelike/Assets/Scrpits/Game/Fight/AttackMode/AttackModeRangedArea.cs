using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class AttackModeRangedArea : AttackModeRanged
{
    public override void HandleForHitTarget(GameFightCreatureEntity gameFightCreatureEntity)
    {
        //播放一个范围攻击特效
        if (!attackModeInfo.effect_hit.IsNull())
        {
            EffectHandler.Instance.ShowBoomEffect(attackModeInfo.effect_hit, gameObject.transform.position, attackModeInfo.collider_area_size);
        }
        //检测范围内的敌人
        CheckHitTargetArea(gameObject.transform.position, (targetCreature) =>
        {
            targetCreature.UnderAttack(this);
        });
        Destory();
    }
}
