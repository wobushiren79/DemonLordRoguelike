public class BuffEntityInstant : BuffBaseEntity
{
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        TriggerBuff(buffEntityData);
        //即刻触发 触发之后移除
        BuffHandler.Instance.manager.RemoveBuffEntity(this);
        BuffHandler.Instance.manager.RemoveBuffEntityBean(buffEntityData);
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override void TriggerBuff(BuffEntityBean buffEntityData)
    {
        base.TriggerBuff(buffEntityData);
        
    }
}