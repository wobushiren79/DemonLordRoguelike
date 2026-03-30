public class BuffEntityBaseHPChange : BuffBaseEntity
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
        var targetCreature = GetFightCreatureEntityForTarget();
        return HPChange(buffEntityData, targetCreature);
    }

    /// <summary>
    /// HP改变
    /// </summary>
    public static bool HPChange(BuffEntityBean buffEntityData, FightCreatureEntity fightCreatureEntity)
    {
        if (fightCreatureEntity == null || fightCreatureEntity.fightCreatureData == null || fightCreatureEntity.IsDead())
        {
            return false;
        }
        else
        {
            int changeHPData = 0;
            var buffInfo = buffEntityData.GetBuffInfo();

            //固定计算
            if (buffInfo.trigger_value > 0)
            {
                float triggerValue = buffEntityData.GetTriggerValue();
                changeHPData += (int)triggerValue;
            }
            //百分比计算
            float triggerValueRate = buffEntityData.GetTriggerValueRate();
            if (triggerValueRate > 0)
            {
                float HPMax = fightCreatureEntity.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
                changeHPData += (int)(HPMax * triggerValueRate);
            }
            //如果改变的HP大于0 则回复HP
            if (changeHPData > 0)
            {
                fightCreatureEntity.RegainHP(buffEntityData.targetCreatureUUId, buffEntityData.targetCreatureUUId, changeHPData);
            }
            //如果小于0 则受到攻击
            else
            {
                FightUnderAttackBean fightUnderAttackData = FightHandler.Instance.GetFightUnderAttackData(buffEntityData, -changeHPData);
                fightCreatureEntity.UnderAttack(fightUnderAttackData);
            }
            return true;
        }
    }

}