using UnityEditor;
using UnityEngine;

public class BuffEntityPeriodic : BuffBaseEntity
{ 
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuff(buffEntityData);
        if (isTriggerSuccess == false) return false;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureId, CreatureTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return false;
        }
        else
        {
            int changeHPData = 0;
            //固定伤害计算
            if (buffEntityData.buffInfo.trigger_value > 0)
            {
                changeHPData += (int)buffEntityData.buffInfo.trigger_value;
            }
            //百分比伤害计算
            if (buffEntityData.buffInfo.trigger_value_rate > 0)
            {
                changeHPData += (int)(targetCreature.fightCreatureData.HPMax * buffEntityData.buffInfo.trigger_value_rate);
            }
            //如果改变的HP大于0 则回复HP
            if (changeHPData > 0)
            {
                targetCreature.RegainHP(buffEntityData.targetCreatureId, buffEntityData.targetCreatureId, changeHPData);
            }
            //如果小于0 则受到攻击
            else
            {
                FightUnderAttackStruct fightUnderAttackStruct = new FightUnderAttackStruct(buffEntityData, -changeHPData);
                targetCreature.UnderAttack(fightUnderAttackStruct);
            }
             return true;
        }
    }
}