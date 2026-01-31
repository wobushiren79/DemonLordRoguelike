public class BuffEntityPeriodicDRChange : BuffEntityPeriodic
{
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        //获取指定生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureId, CreatureFightTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return false;
        }
        else
        {
            int changeDRData = 0;
            var buffInfo = buffEntityData.GetBuffInfo();

            //固定计算
            if (buffInfo.trigger_value > 0)
            {
                float triggerValue = buffEntityData.GetTriggerValue();
                changeDRData += (int)triggerValue;
            }

            //百分比计算
            float triggerValueRate = buffEntityData.GetTriggerValueRate();
            if (triggerValueRate > 0)
            {
                float DRMax = targetCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.DR);
                changeDRData += (int)(DRMax * triggerValueRate);
            }
            //如果改变的DR大于0 则回复DR
            if (changeDRData > 0)
            {
                targetCreature.RegainDR(buffEntityData.targetCreatureId, buffEntityData.targetCreatureId, changeDRData);
            }
            //如果小于0 则受到攻击
            else
            {
                FightUnderAttackStruct fightUnderAttackStruct = new FightUnderAttackStruct(buffEntityData, -changeDRData);
                targetCreature.UnderAttack(fightUnderAttackStruct);
            }
            return true;
        }
    }

}