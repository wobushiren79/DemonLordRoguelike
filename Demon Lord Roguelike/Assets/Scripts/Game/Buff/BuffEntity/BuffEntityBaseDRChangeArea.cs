public class BuffEntityBaseDRChangeArea : BuffEntityBaseDRChange
{
    #region 范围护甲变化
    /// <summary>
    /// 执行通用触发判定并对范围内友军应用护甲变化。
    /// </summary>
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        if (!InvokeBaseTriggerBuff(buffEntityData)) return false;
        return DRChangeArea(buffEntityData, GetFightCreatureEntityForTarget());
    }

    /// <summary>
    /// 对范围内友军应用护甲变化。死亡效果使用死者的护甲上限，常规效果使用受益者上限。
    /// </summary>
    public static bool DRChangeArea(BuffEntityBean buffEntityData, FightCreatureEntity source, bool isChangeSelf = false, float? sourceMax = null)
    {
        return BuffEntityAreaChange.Apply(buffEntityData, source, isChangeSelf, target => DRChange(buffEntityData, target, sourceMax));
    }
    #endregion
}
