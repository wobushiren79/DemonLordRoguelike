public class BuffEntityPeriodicHPChange : BuffEntityPeriodic
{
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureId, CreatureFightTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return false;
        }
        else
        {
            int changeHPData = 0;
            var buffInfo = buffEntityData.GetBuffInfo();

            //固定伤害计算
            if (buffInfo.trigger_value > 0)
            {
                float triggerValue = buffEntityData.GetTriggerValue();
                changeHPData += (int)triggerValue;
            }

            //百分比伤害计算
            float triggerValueRate = buffEntityData.GetTriggerValueRate();
            if (triggerValueRate > 0)
            {
                float HPMax = targetCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
                changeHPData += (int)(HPMax * triggerValueRate);
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