using UnityEditor;
using UnityEngine;

public class BuffEntityForHPChange : BuffBaseEntity
{
    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.getCreatureId, CreatureTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return;
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
                targetCreature.RegainHP(buffEntityData.getCreatureId, buffEntityData.getCreatureId, changeHPData);
            }
            //如果小于0 则受到攻击
            else
            {
                FightUnderAttackStruct fightUnderAttackStruct = new FightUnderAttackStruct(buffEntityData, -changeHPData);
                targetCreature.UnderAttack(fightUnderAttackStruct);
            }
        }
    }
}