using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEditor;
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
    public float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
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
            case CreatureAttributeTypeEnum.HP:
            case CreatureAttributeTypeEnum.DR:
            case CreatureAttributeTypeEnum.ATK:
            case CreatureAttributeTypeEnum.ASPD:
            case CreatureAttributeTypeEnum.MSPD:
            case CreatureAttributeTypeEnum.RCD:
                targetData += buffData.trigger_value;
                //小于0特殊处理
                if (targetData < 0) targetData = 0;
                targetData *= 1 + buffData.trigger_value_rate;
                break;
            case CreatureAttributeTypeEnum.CRT:
            case CreatureAttributeTypeEnum.EVA:
                targetData += buffData.trigger_value_rate;
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