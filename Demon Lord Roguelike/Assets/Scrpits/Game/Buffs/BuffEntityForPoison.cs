using UnityEditor;
using UnityEngine;

public class BuffEntityForPoison : BuffBaseEntity
{
    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.creatureId, CreatureTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return;
        }
        else
        {
            int damage = 0;
            //固定伤害计算
            if (buffEntityData.buffInfo.trigger_value > 0)
            {
                damage += (int)buffEntityData.buffInfo.trigger_value;
            }
            //百分比伤害计算
            if (buffEntityData.buffInfo.trigger_value_rate > 0)
            {
                damage += (int)((targetCreature.fightCreatureData.HPMax + targetCreature.fightCreatureData.DRMax) * buffEntityData.buffInfo.trigger_value_rate);
            }
            targetCreature.UnderAttack(buffEntityData.creatureId,buffEntityData.creatureId, damage);
        }
    }
}