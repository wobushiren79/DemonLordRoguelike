using System.Collections.Generic;

/// <summary>
/// 条件属性BUFF：满足前置条件时才把 modifier 注入管线
/// </summary>
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

    /// <summary>
    /// 条件未满足时不产出 modifier
    /// </summary>
    public override void CollectModifiers(List<AttributeModifier> sink)
    {
        if (buffEntityData == null || !buffEntityData.isValid) return;
        if (!CheckIsPre(buffEntityData)) return;
        EmitModifiers(sink, buffEntityData.buffData, attributeType, buffEntityData.stackCount, this);
    }

    /// <summary>
    /// 兼容层：单BUFF应用（CreatureBean.cs 的预览/RCD 路径仍走此方法），条件不满足时不应用
    /// </summary>
    public override float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (!CheckIsPre(buffEntityData)) return targetData;
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
