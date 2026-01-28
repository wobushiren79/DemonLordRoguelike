public class BuffEntityInstant : BuffBaseEntity
{
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        TriggerBuffInstant(buffEntityData);
        buffEntityData.isValid = false;
    }

    public override void UpdateBuffTime(float buffTime)
    {
        buffEntityData.timeUpdateTotal += buffTime;
    }

}