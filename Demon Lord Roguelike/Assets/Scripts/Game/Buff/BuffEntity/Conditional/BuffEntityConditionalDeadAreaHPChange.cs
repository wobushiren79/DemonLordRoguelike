public class BuffEntityConditionalDeadAreaHPChange : BuffEntityConditionalDead
{
    #region 死亡范围生命变化
    /// <summary>
    /// 死亡时按死者的生命上限结算范围内友军的生命变化。
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        if (!base.TriggerBuffConditional(buffEntityData)) return false;
        var source = GetFightCreatureEntityForTarget();
        if (source == null) return false;

        float sourceMax = source.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
        return BuffEntityBaseHPChangeArea.HPChangeArea(buffEntityData, source, sourceMax: sourceMax);
    }
    #endregion
}
