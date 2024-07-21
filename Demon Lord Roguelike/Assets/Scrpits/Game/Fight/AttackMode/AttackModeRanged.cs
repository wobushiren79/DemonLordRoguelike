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
            if (targetCreature != null && !targetCreature.IsDead())
            {
                //��Ѫ
                targetCreature.UnderAttack(attackerDamage, attackDirection, out int leftLife, out int leftArmor);
                //�������˾ͻ����������
                Destory();
                return;
            }
        }
        gameObject.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
        //�߽紦��
        if (gameObject.transform.position.x > 15 || gameObject.transform.position.x < -5)
        {
            Destory();
        }
    }
}
