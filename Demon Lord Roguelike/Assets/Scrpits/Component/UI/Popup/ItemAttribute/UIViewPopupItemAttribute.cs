

using UnityEngine;

public partial class UIViewPopupItemAttribute : BaseUIView
{
    /// <summary>
    /// 设置属性数据
    /// </summary>
    /// <param name="attributeType">属性类型</param>
    /// <param name="attributeValue">属性值</param>
    public void SetData(CreatureAttributeTypeEnum attributeType, float attributeValue)
    {
        //获取属性名称的多语言文本
        string attributeName = CreatureAttributeTypeInfoCfg.GetAttributeTypeNameByEnum(attributeType);
        Color attributeColor = CreatureAttributeTypeInfoCfg.GetAttributeTypeColorByEnum(attributeType);
        string showText = $"{attributeName}: +{attributeValue}";
        ui_AttributeText.text = showText;
        ui_AttributeText.color = attributeColor;
    }
}