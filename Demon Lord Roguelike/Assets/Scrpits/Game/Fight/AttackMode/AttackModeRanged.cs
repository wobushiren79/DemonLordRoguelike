using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AttackModeRanged : BaseAttackMode
{
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        gameObject.transform.position = attacker.creatureObj.transform.position + new Vector3(0, 0.5f, 0);
        actionForAttackEnd?.Invoke();
    }

    public override void Update()
    {
        base.Update();
        RayUtil.RayToCast(gameObject.transform.position, attackDirection, attackModeInfo.collider_size, 1 << attackedLayer, out RaycastHit hit);
        if (hit.collider != null)
        {
            string creatureId = hit.collider.gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetFightCreatureById(creatureId);
            if (targetCreature != null)
            {
                //扣血
                targetCreature.UnderAttack(attackerDamage, out int leftLife, out int leftArmor);
                //如果被攻击对象死亡
                if (leftLife <= 0)
                {
                    targetCreature.SetCreatureDead();
                }
                //攻击完了就回收这个攻击
                Destory();
                return;
            }
        }
        gameObject.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
        //边界处理
        if (gameObject.transform.position.x > 15 || gameObject.transform.position.x < -5)
        {
            Destory();
        }
    }
}
