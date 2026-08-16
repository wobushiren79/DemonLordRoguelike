using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeLure : BaseAttackMode
{
    public override void StartAttack()
    {
        base.StartAttack();
        //攻击完了就回收这个攻击
        Destroy();
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            //被攻击者改变线路
            attacked.ChangeRoad(attacker.fightCreatureData.roadIndex);
            //魅惑成功音效：走攻击模式配置的命中音效 sound_hit（600001 配 sound_medicine_1=470001）
            AudioHandler.Instance.PlaySound(attackModeInfo.sound_hit);
            //播放击中粒子特效：敌人位置 + 攻击者攻击位置偏移(attack_start_position, 4003 配 0,0.5,0)
            Vector3 attackStartOffset = attacker.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
            PlayEffectForHit(attacked.creatureObj.transform.position + attackStartOffset);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
