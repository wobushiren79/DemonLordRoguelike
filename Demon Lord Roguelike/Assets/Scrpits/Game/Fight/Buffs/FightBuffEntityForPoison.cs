using UnityEditor;
using UnityEngine;

public class FightBuffEntityForPoison : FightBuffBaseEntity
{
    public override void TriggerBuff(FightBuffBean fightBuffData)
    {
        base.TriggerBuff(fightBuffData);
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(fightBuffData.creatureId, CreatureTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return;
        }
        else
        {
            int damage = 0;
            //固定伤害计算
            if (fightBuffData.fightBuffStruct.triggerValue > 0)
            {
                damage += (int)fightBuffData.fightBuffStruct.triggerValue;
            }
            //百分比伤害计算
            if (fightBuffData.fightBuffStruct.triggerValueRate > 0)
            {
                damage += (int)((targetCreature.fightCreatureData.HPMax + targetCreature.fightCreatureData.DRMax) * fightBuffData.fightBuffStruct.triggerValueRate);
            }
            targetCreature.UnderAttack(fightBuffData.creatureId,fightBuffData.creatureId, damage);
        }
    }
}