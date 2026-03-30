using System;

public static class ItemsUtil
{
    /// <summary>
    /// 获取多语言显示文本
    /// </summary>
    /// <param name="userType">道具使用者类型</param>
    /// <returns>多语言文本</returns>
    public static string GetLanguageText(this ItemUserTypeEnum userType)
    {
        switch (userType)
        {
            case ItemUserTypeEnum.Default:
                return "";
            case ItemUserTypeEnum.DemonLord:
                return TextHandler.Instance.GetTextById(90002);
            default:
                return "";
        }
    }
}
