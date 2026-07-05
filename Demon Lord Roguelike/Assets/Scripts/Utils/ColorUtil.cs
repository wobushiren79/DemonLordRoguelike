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

    #region 通用达上限警示红
    /// <summary>达到数量上限时的警示红色HTML串(用于 TMP 富文本 &lt;color&gt; 包裹)</summary>
    public const string LimitFullHtml = "#FF4D4D";
    /// <summary>达到数量上限时的警示红(用于直接给 Text/Image.color 赋值)</summary>
    public static readonly Color LimitFull = ParseHtmlString(LimitFullHtml);

    /// <summary>
    /// 数量文本达上限着色：达到/超过上限时用通用警示红富文本包裹，否则原样返回。
    /// 阵容满员、进阶素材选满等「当前/上限」展示统一复用。
    /// </summary>
    /// <param name="content">原始文本(通常为 当前/上限 格式)</param>
    /// <param name="isFull">是否已达上限</param>
    /// <returns>着色后的富文本(满)或原文本(未满)</returns>
    public static string WrapLimitFull(string content, bool isFull)
    {
        return isFull ? $"<color={LimitFullHtml}>{content}</color>" : content;
    }
    #endregion
}
