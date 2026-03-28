using UnityEngine;

public static class ColorUtil
{
    /// <summary>
    /// 将HTML颜色字符串解析为Color
    /// </summary>
    /// <param name="htmlString">HTML颜色字符串，如 "#FF0000" 或 "#FF0000FF"</param>
    /// <returns>解析后的Color，如果解析失败则返回Color.white</returns>
    public static Color ParseHtmlString(string htmlString)
    {
        if (ColorUtility.TryParseHtmlString(htmlString, out Color color))
        {
            return color;
        }
        return Color.white;
    }

    /// <summary>
    /// 尝试将HTML颜色字符串解析为Color
    /// </summary>
    /// <param name="htmlString">HTML颜色字符串</param>
    /// <param name="defaultColor">解析失败时返回的默认颜色</param>
    /// <returns>解析后的Color</returns>
    public static Color ParseHtmlString(string htmlString, Color defaultColor)
    {
        if (ColorUtility.TryParseHtmlString(htmlString, out Color color))
        {
            return color;
        }
        return defaultColor;
    }
}
