using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeExplosion : BaseAttackMode
{
    public override void StartAttack()
    {
        base.StartAttack();
        AttackHandle();
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            //攻击完了就回收这个攻击
            Destroy();
            return;
        }
        AttackHandle();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
        //自己也死
        attacker.SetCreatureDead();
    }

    public void AttackHandle()
    {
        Vector3 centerPos = attackModeData.startPos;
        //播放击中粒子特效
        PlayEffectForHit(centerPos);
        //爆心伤害快照（衰减以此为基准逐目标改写 attackerDamage，照 AttackModeInstantArea 递减先例）
        int baseDamage = attackModeData.attackerDamage;
        //爆炸半径（仅球形范围检测参与衰减；非球形/未配半径/零伤害时保持原全额行为）
        float radius = 0;
        if (attackModeInfo.GetColliderAreaSerachType() == CreatureSearchType.AreaSphere)
        {
            float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
            if (colliderAreaSize != null && colliderAreaSize.Length > 0)
                radius = colliderAreaSize[0];
        }
        //边缘最低伤害倍率（other_data 键 falloff_min，默认0.5=最边缘伤害为中心的1/2）
        float falloffMinRate = attackModeInfo.GetFalloffMinRate();
        //检测周围的敌人
        CheckHitTargetArea(centerPos, (FightCreatureEntity itemAttacked) =>
        {
            if (itemAttacked != null && !itemAttacked.IsDead())
            {
                //伤害按距爆心距离线性衰减：中心全额→检测球边缘 falloffMinRate 倍，保底1点
                if (radius > 0 && baseDamage > 0 && itemAttacked.creatureObj != null)
                {
                    float distanceRate = Mathf.Clamp01(Vector3.Distance(itemAttacked.creatureObj.transform.position, centerPos) / radius);
                    attackModeData.attackerDamage = Math.Max(1, (int)(baseDamage * Mathf.Lerp(1f, falloffMinRate, distanceRate)));
                }
                //扣血
                itemAttacked.UnderAttack(this);
            }
        });

        //攻击完了就回收这个攻击
        Destroy();
    }
}
