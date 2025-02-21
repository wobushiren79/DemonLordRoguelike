using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeMelee : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            //��Ѫ
            attacked.UnderAttack(this);
        }

        //�������˾ͻ����������
        Destory();
        //���������ص�
        actionForAttackEnd?.Invoke();
    }
}
