public class BuffEntityBaseDRChange : BuffBaseEntity
{
    #region 护甲变化
    /// <summary>
    /// 执行通用 BUFF 触发判定，供范围子类使用。
    /// </summary>
    protected bool InvokeBaseTriggerBuff(BuffEntityBean buffEntityData) => base.TriggerBuff(buffEntityData);

    /// <summary>
    /// 按持有者护甲上限计算护甲变化。
    /// </summary>
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        if (!base.TriggerBuff(buffEntityData)) return false;
        return DRChange(buffEntityData, GetFightCreatureEntityForTarget());
    }

    /// <summary>
    /// 对存活目标应用护甲变化。范围效果可指定施加者的护甲上限作为计算基准。
    /// </summary>
    public static bool DRChange(BuffEntityBean buffEntityData, FightCreatureEntity fightCreatureEntity, float? sourceMax = null)
    {
        var targetData = fightCreatureEntity?.fightCreatureData;
        if (targetData == null || fightCreatureEntity.IsDead()) return false;

        float max = sourceMax ?? targetData.GetAttribute(CreatureAttributeTypeEnum.DR);
        int change = (int)buffEntityData.GetTriggerValue() + (int)(max * buffEntityData.GetTriggerValueRate());
        string targetId = targetData.creatureData.creatureUUId;
        if (change > 0)
        {
            fightCreatureEntity.RegainDR(buffEntityData.applierCreatureUUId, targetId, change);
        }
        else if (change < 0)
        {
            var attackData = FightHandler.Instance.GetFightUnderAttackData(buffEntityData, -change);
            attackData.attackedId = targetId;
            fightCreatureEntity.UnderAttack(attackData);
        }
        return change != 0;
    }
    #endregion
}
