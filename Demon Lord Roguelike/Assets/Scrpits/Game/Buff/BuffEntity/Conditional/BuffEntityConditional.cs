public class BuffEntityConditional : BuffBaseEntity
{

    public override void AddBuffTime(float buffTime)
    {
        //条件型触发BUFF 不增加触发时间
    }

    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuff(buffEntityData);
        return isTriggerSuccess;
    }
}