using UnityEditor;
using UnityEngine;

public class BuffEntityAttribute : BuffBaseEntity
{
    public CreatureAttributeTypeEnum targetAttributeType = CreatureAttributeTypeEnum.None;

    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        string classEntityData = buffEntityData.buffInfo.class_entity_data;
        if (classEntityData.IsNull())
        {
            LogUtil.LogError($"初始化BuffEntityAttribute失败，class_entity_data为空 buffID:{buffEntityData.buffInfo.id}");
        }
        else
        {
            targetAttributeType = classEntityData.GetEnum<CreatureAttributeTypeEnum>();
        }
    }

    public virtual float GetChangeData(CreatureAttributeTypeEnum attributeTypeEnum)
    {
        if (targetAttributeType == attributeTypeEnum && CheckIsPre(buffEntityData))
        {
            return buffEntityData.buffInfo.trigger_value;
        }
        else
        {
            return 0;
        }
    }

    public virtual float GetChangeRateData(CreatureAttributeTypeEnum attributeTypeEnum)
    {
        if (targetAttributeType == attributeTypeEnum && CheckIsPre(buffEntityData))
        {
            return buffEntityData.buffInfo.trigger_value_rate;
        }
        else
        {
            return 0;
        }
    }
}