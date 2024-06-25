using System.Collections;
using System.Collections.Generic;
using UnityEditor.TestTools.CodeCoverage;
using UnityEngine;

public class GameFightCreatureEntity
{
    public string creatureId;
    public GameObject creatureObj;
    public FightCreatureBean fightCreatureData;
    public AIBaseEntity aiEntity;
    public CreatureStateEnum creatureState = CreatureStateEnum.None;

    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureId = $"Creature_{SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N)}";
        this.creatureState = CreatureStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = creatureId;
    }

    /// <summary>
    /// 收到攻击
    /// </summary>
    /// <returns></returns>
    public void UnderAttack(int attDamage, out int leftLife, out int leftArmor)
    {
        //先扣除护甲
        leftArmor = fightCreatureData.ChangeArmor(-attDamage, out int outArmorChangeData);
        //如果护甲已经全部扣完 再扣除生命
        if (outArmorChangeData != 0)
        {
            fightCreatureData.ChangeLife(outArmorChangeData);
        }
        leftLife = fightCreatureData.liftCurrent;
    }

    /// <summary>
    /// 是否已经死亡
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        if (creatureState == CreatureStateEnum.Dead)
            return true;
        //如果目标生物已经无了
        //if (creatureObj == null || fightCreatureData == null || fightCreatureData.liftCurrent <= 0)
        //{
        //    return true;
        //}
        return false;
    }

    /// <summary>
    /// 设置生物死亡
    /// </summary>
    public void SetCreatureDead()
    {
        creatureState = CreatureStateEnum.Dead;
        if (aiEntity is AIAttCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.AttCreatureDead);
        }
        else if (aiEntity is AIDefCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefCreatureDead);
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDead, fightCreatureData);
    }
}
