using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightCreatureEntity
{
    public GameObject creatureObj;
    public FightCreatureBean fightCreatureData;
    public AIBaseEntity aiEntity;

    /// <summary>
    /// �Ƿ��Ѿ�����
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        //���Ŀ�������Ѿ�����
        if (creatureObj == null || fightCreatureData == null || fightCreatureData.liftCurrent <= 0)
        {
            return true;
        }
        return false;
    }
}
