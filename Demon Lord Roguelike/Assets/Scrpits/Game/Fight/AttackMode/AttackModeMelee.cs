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
            //获取伤害
            int attDamage = attacker.fightCreatureData.GetAttDamage();
            //扣血
            attacked.UnderAttack(attDamage, out int leftLife, out int leftArmor);
            //如果被攻击对象死亡
            if (leftLife <= 0)
            {
                attacked.SetCreatureDead();
            }
        }

        //攻击完了就回收这个攻击
        Destory();
        //攻击结束回调
        actionForAttackEnd?.Invoke();
    }
}
