/// <summary>
/// 条件性触发 不受时间影响
/// </summary>
public class BuffEntityConditional : BuffBaseEntity
{    

    public override void UpdateBuffTime(float buffTime)
    {
        buffEntityData.timeUpdateTotal += buffTime;
    }

}