public class BuffEntityBaseDRChange : BuffBaseEntity
{    

    /// <summary>
    /// 基础方法
    /// </summary>
    protected bool InvokeBaseTriggerBuff(BuffEntityBean buffEntityData) => base.TriggerBuff(buffEntityData);


    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuff(buffEntityData);
        if (isTriggerSuccess == false) return false;
        //获取指定生物
        var targetCreature =  GetFightCreatureEntityForTarget();
        return DRChange(buffEntityData, targetCreature);
    }



    /// <summary>
    /// HP改变
    /// </summary>
    public static bool DRChange(BuffEntityBean buffEntityData, FightCreatureEntity fightCreatureEntity)
    {
        if (fightCreatureEntity == null || fightCreatureEntity.fightCreatureData == null || fightCreatureEntity.IsDead())
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
                float DRMax = fightCreatureEntity.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.DR);
                changeDRData += (int)(DRMax * triggerValueRate);
            }
            //如果改变的DR大于0 则回复DR
            if (changeDRData > 0)
            {
                fightCreatureEntity.RegainDR(buffEntityData.targetCreatureUUId, buffEntityData.targetCreatureUUId, changeDRData);
            }
            //如果小于0 则受到攻击
            else
            {
                FightUnderAttackBean fightUnderAttackData = FightHandler.Instance.GetFightUnderAttackData(buffEntityData, -changeDRData);
                fightCreatureEntity.UnderAttack(fightUnderAttackData);
            }
            return true;
        }
    }

}