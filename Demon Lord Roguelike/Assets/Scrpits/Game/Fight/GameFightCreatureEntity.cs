using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightCreatureEntity
{
    public GameObject creatureObj;
    public FightCreatureBean fightCreatureData;
    public AIBaseEntity aiEntity;

    /// <summary>
    /// 是否已经死亡
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        //如果目标生物已经无了
        if (creatureObj == null || fightCreatureData == null || fightCreatureData.liftCurrent <= 0)
        {
            return true;
        }
        return false;
    }
}
