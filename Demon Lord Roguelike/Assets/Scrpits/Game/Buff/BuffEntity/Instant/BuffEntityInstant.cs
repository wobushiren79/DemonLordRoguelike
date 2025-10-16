public class BuffEntityInstant : BuffBaseEntity
{
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        TriggerBuff(buffEntityData);

        buffEntityData.isValid = false;
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuff(buffEntityData);
        return isTriggerSuccess;
    }
}