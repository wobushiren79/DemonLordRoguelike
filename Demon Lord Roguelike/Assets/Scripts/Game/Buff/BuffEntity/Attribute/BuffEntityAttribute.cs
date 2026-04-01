using UnityEngine;

public class BuffEntityAttribute : BuffBaseEntity
{
    public CreatureAttributeTypeEnum attributeType = CreatureAttributeTypeEnum.None;

    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        string classEntityData = buffInfo.class_entity_data;
        if (classEntityData.IsNull())
        {
            LogUtil.LogError($"初始化BuffEntityAttribute失败，class_entity_data为空 buffID:{buffEntityData.buffId}");
        }
        else
        {
            attributeType = classEntityData.GetEnum<CreatureAttributeTypeEnum>();
        }
    }

    /// <summary>
    /// 改变数据-内部
    /// </summary>
    public virtual float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (targetAttributeType != attributeType)
        {
            return targetData;
        }
        return ChangeData(buffEntityData.buffData, targetAttributeType, targetData);
    }

    /// <summary>
    /// 改变数据-全局
    /// </summary>
    public static float ChangeData(BuffBean buffData, CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        switch (targetAttributeType)
        {
            //特殊属性 从0%开始叠加
            case CreatureAttributeTypeEnum.CRT:
            case CreatureAttributeTypeEnum.EVA:
                targetData += buffData.trigger_value_rate;
                break;
            default:
                targetData += buffData.trigger_value;
                //小于0特殊处理
                if (targetData < 0) targetData = 0;
                targetData *= 1 + buffData.trigger_value_rate;
                break;
        }
        if (targetData < 0)
        {
            targetData = 0;
        }
        return targetData;
    }
}