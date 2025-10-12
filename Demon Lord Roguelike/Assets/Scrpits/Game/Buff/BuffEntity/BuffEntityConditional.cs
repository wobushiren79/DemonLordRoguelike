public class BuffEntityConditional : BuffBaseEntity
{

    public override void AddBuffTime(float buffTime)
    {
        //条件型触发BUFF 不增加触发时间
    }

    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);

    }
}