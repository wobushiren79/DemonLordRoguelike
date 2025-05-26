using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged​Piercing : AttackModeRanged​
{
    //最大穿透数量
    public int numPierceMax = 3;
    //已经穿透的生物
    public HashSet<string> listPierceCreature;

    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        listPierceCreature = new HashSet<string>();
    }

    public override void Update()
    {
        List<GameFightCreatureEntity> listHitTarget = CheckHitTarget();
        if (!listHitTarget.IsNull())
        {
            for (int i = 0; i < listHitTarget.Count; i++)
            {
                var itemCreature = listHitTarget[i];
                string itemCreatureId = itemCreature.fightCreatureData.creatureData.creatureId;
                if (listPierceCreature.Contains(itemCreatureId))
                {
                    continue;
                }
                HandleForHitTarget(itemCreature);
                listPierceCreature.Add(itemCreatureId);
                if (listPierceCreature.Count >= numPierceMax)
                {
                    Destroy();
                    return;
                }
            }
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }


    public override void HandleForHitTarget(GameFightCreatureEntity gameFightCreatureEntity)
    {
        //扣血
        gameFightCreatureEntity.UnderAttack(this);
    }
}
