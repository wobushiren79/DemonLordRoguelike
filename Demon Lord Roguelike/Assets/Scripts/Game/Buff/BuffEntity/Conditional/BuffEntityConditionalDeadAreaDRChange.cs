public class BuffEntityConditionalDeadAreaDRChange : BuffEntityConditionalDead
{
    #region 死亡范围护甲变化
    /// <summary>
    /// 死亡时按死者的护甲上限结算范围内友军的护甲变化。
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        if (!base.TriggerBuffConditional(buffEntityData)) return false;
        var source = GetFightCreatureEntityForTarget();
        if (source == null) return false;

        float sourceMax = source.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.DR);
        return BuffEntityBaseDRChangeArea.DRChangeArea(buffEntityData, source, sourceMax: sourceMax);
    }
    #endregion
}
