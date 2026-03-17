public class BuffEntityConditionalAttribute : BuffEntityAttribute
{
    public bool isPre = false;

    public override void ClearData()
    {
        base.ClearData();
        isPre = false;
    }

    public override void UpdateBuffTime(float buffTime)
    {
        buffEntityData.timeUpdateTotal += buffTime;
    }

    public override float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (!CheckIsPre(buffEntityData))
        {
            return targetData;
        }
        return base.ChangeData(targetAttributeType, targetData);
    }

    /// <summary>
    /// 处理检测
    /// </summary>
    public override void HandleForEvent()
    {
        base.HandleForEvent();
        if (!isPre && CheckIsPre(buffEntityData))
        {
            isPre = true;
            //通知刷新属性
            var fightCreatureEntity = GetFightCreatureEntityForTarget();
            if (fightCreatureEntity != null && !fightCreatureEntity.IsDead())
            {
                fightCreatureEntity.fightCreatureData.RefreshBaseAttribute();
            }
        }
    }
}