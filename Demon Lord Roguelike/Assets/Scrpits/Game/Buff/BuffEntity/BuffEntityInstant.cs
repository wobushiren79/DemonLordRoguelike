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
    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);
        
    }
}