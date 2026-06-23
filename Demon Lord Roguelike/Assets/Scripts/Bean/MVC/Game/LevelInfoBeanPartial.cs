using System;
using System.Collections.Generic;
using UnityEngine;

public partial class LevelInfoBean
{

}
public partial class LevelInfoCfg
{
    #region 等级颜色
    /// <summary>
    /// 按等级获取等级字体颜色。
    /// <para>0 级(及无配置/颜色为空)返回白色; 1-10 级取 `excel_level_info` 配置的 `level_color`(具渐进感的 10 种颜色)。</para>
    /// </summary>
    /// <param name="level">生物等级(0~10)</param>
    /// <returns>对应的等级字体颜色, 解析失败时回退为白色</returns>
    public static Color GetLevelColor(int level)
    {
        //0 级及以下: 无对应配置, 统一白色
        if (level <= 0)
            return Color.white;
        var levelInfo = GetItemData(level);
        if (levelInfo == null || string.IsNullOrEmpty(levelInfo.level_color))
            return Color.white;
        if (ColorUtility.TryParseHtmlString(levelInfo.level_color, out Color color))
            return color;
        return Color.white;
    }
    #endregion
}
