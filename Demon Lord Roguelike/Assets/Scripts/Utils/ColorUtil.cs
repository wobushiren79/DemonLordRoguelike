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

    #region 进度分段配色
    /// <summary>进度分段颜色 0%~20% (红)</summary>
    static readonly Color ProgressColorVeryLow = ParseHtmlString("#C0392B");
    /// <summary>进度分段颜色 20%~40% (橙)</summary>
    static readonly Color ProgressColorLow = ParseHtmlString("#E67E22");
    /// <summary>进度分段颜色 40%~60% (黄)</summary>
    static readonly Color ProgressColorMedium = ParseHtmlString("#F1C40F");
    /// <summary>进度分段颜色 60%~80% (浅绿)</summary>
    static readonly Color ProgressColorHigh = ParseHtmlString("#2ECC71");
    /// <summary>进度分段颜色 80%~100% (蓝)</summary>
    static readonly Color ProgressColorVeryHigh = ParseHtmlString("#3498DB");

    /// <summary>
    /// 按进度百分比取分段颜色(0~1 分5段:0-0.2红 0.2-0.4橙 0.4-0.6黄 0.6-0.8浅绿 0.8-1蓝)。
    /// 献祭成功率进度条与孵化缸进阶BUFF概率统一复用此配色。
    /// </summary>
    /// <param name="rate01">0~1 的进度/百分比</param>
    /// <returns>该区间对应的颜色</returns>
    public static Color GetProgressColor(float rate01)
    {
        if (rate01 < 0.2f)
            return ProgressColorVeryLow;
        if (rate01 < 0.4f)
            return ProgressColorLow;
        if (rate01 < 0.6f)
            return ProgressColorMedium;
        if (rate01 < 0.8f)
            return ProgressColorHigh;
        return ProgressColorVeryHigh;
    }
    #endregion
}
