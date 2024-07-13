using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeMelee : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && attacked.IsDead())
        {
            //��ȡ�˺�
            int attDamage = attacker.fightCreatureData.GetAttDamage();
            //��Ѫ
            attacked.UnderAttack(attDamage, out int leftLife, out int leftArmor);
            //�����������������
            if (leftLife <= 0)
            {
                attacked.SetCreatureDead();
            }
        }

        //�������˾ͻ����������
        Destory();
        //���������ص�
        actionForAttackEnd?.Invoke();
    }
}
