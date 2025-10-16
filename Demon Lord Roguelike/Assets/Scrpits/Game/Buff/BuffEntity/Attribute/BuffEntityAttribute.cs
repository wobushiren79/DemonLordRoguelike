using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEditor;
using UnityEngine;

public class BuffEntityAttribute : BuffBaseEntity
{
    public CreatureAttributeTypeEnum attributeType = CreatureAttributeTypeEnum.None;

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
            attributeType = classEntityData.GetEnum<CreatureAttributeTypeEnum>();
        }
    }

    public virtual float GetChangeData(CreatureAttributeTypeEnum attributeTypeEnum)
    {
        if (attributeType == attributeTypeEnum && CheckIsPre(buffEntityData))
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
        if (attributeType == attributeTypeEnum && CheckIsPre(buffEntityData))
        {
            return buffEntityData.buffInfo.trigger_value_rate;
        }
        else
        {
            return 0;
        }
    }

    public float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if(targetAttributeType != attributeType)
        {
            return targetData;
        }
        switch (targetAttributeType)
        {
            case CreatureAttributeTypeEnum.HP:
            case CreatureAttributeTypeEnum.DR:
            case CreatureAttributeTypeEnum.ATK:
            case CreatureAttributeTypeEnum.ASPD:
            case CreatureAttributeTypeEnum.MSPD:
            case CreatureAttributeTypeEnum.RCD:
                targetData += GetChangeData(targetAttributeType);
                //小于0特殊处理
                if (targetData < 0) targetData = 0;
                targetData *= 1 + GetChangeRateData(targetAttributeType);
                break;
            case CreatureAttributeTypeEnum.CRT:
            case CreatureAttributeTypeEnum.EVA:
                targetData += GetChangeRateData(targetAttributeType);
                break;
            default:
                break;
        }
        if (targetData < 0)
        {   
            targetData = 0;
        }
        return targetData;
    }
}