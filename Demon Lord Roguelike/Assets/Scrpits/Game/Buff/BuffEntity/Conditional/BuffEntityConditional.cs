public class BuffEntityConditional : BuffBaseEntity
{
    public override void UpdateBuffTime(float buffTime)
    {
        buffEntityData.timeUpdateTotal += buffTime;
    }

}