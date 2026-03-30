using UnityEngine;
public partial class CreatureAttributeTypeInfoBean
{
}
public partial class CreatureAttributeTypeInfoCfg
{
        
    /// <summary>
    /// 通过枚举获取属性类型多语言名称
    /// </summary>
    public static string GetAttributeTypeNameByEnum(CreatureAttributeTypeEnum attributeType)
    {
        if (attributeType == CreatureAttributeTypeEnum.None)
        {
            return "???";
        }
        CreatureAttributeTypeInfoBean bean = GetItemData((long)attributeType);
        if (bean == null)
        {
            return "???";
        }
        return bean.name_language;
    }
    /// <summary>
    /// 通过枚举获取属性类型文本颜色
    /// </summary>
    public static Color GetAttributeTypeColorByEnum(CreatureAttributeTypeEnum attributeType)
    {
        if (attributeType == CreatureAttributeTypeEnum.None)
        {
            return Color.black;
        }
        CreatureAttributeTypeInfoBean bean = GetItemData((long)attributeType);
        if (bean == null)
        {
            return Color.black;
        }
        return ColorUtil.ParseHtmlString(bean.color_text);
    }
}
