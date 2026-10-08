#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// 幻化药 UI/世界 尺寸/位置测试覆盖层（仅编辑器编译，打包无此类）：
/// 幻化药测试面板(TestTransformPotionGUI)调参时写入，CreatureBeanPartial.GetTransformUIShowData/GetTransformShowData/GetTransformWorldData/GetTransformShowBrightness
/// 查询时优先于道具 other_data 配置生效，使所有走真实显示链的 UI/世界显示立即反映调参结果；保存写回 Mod 项目或重置时清除。
/// 数据段对应 other_data 键：show_data=默认展示(小卡)尺寸、ui_show_data=详情UI尺寸、world_data=世界显示尺寸/偏移、show_brightness=场景亮度系数(1=原亮度,<1调暗,>1调亮,域(0,2])。
/// </summary>
public static class TransformPotionUITestOverride
{
    #region 数据字段

    /// <summary>单条覆盖数据：详情UI/默认展示(小卡)/世界显示 各自的「scale;x,y」覆盖值 + 场景亮度系数(null=该段不覆盖)</summary>
    private class OverrideData
    {
        public string uiShowData;
        public string showData;
        public string worldData;
        /// <summary>场景亮度系数 show_brightness 覆盖值（「0.58」/「1.50」形式，null=不覆盖）</summary>
        public string showBrightness;
    }

    /// <summary>覆盖表：key=幻化药道具完整id</summary>
    private static readonly Dictionary<long, OverrideData> dicOverride = new Dictionary<long, OverrideData>();

    #endregion

    #region 写入/清除

    /// <summary>
    /// 设置指定幻化药的详情UI尺寸覆盖值(ui_show_data 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    /// <param name="data">「scale;x,y」覆盖值</param>
    public static void SetUiShowData(long itemId, string data)
    {
        GetOrAdd(itemId).uiShowData = data;
    }

    /// <summary>
    /// 设置指定幻化药的默认展示(小卡)尺寸覆盖值(show_data 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    /// <param name="data">「scale;x,y」覆盖值</param>
    public static void SetShowData(long itemId, string data)
    {
        GetOrAdd(itemId).showData = data;
    }

    /// <summary>
    /// 设置指定幻化药的世界显示尺寸/偏移覆盖值(world_data 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    /// <param name="data">「scale;x,y」覆盖值(x=横向偏移,y=竖向抬升)</param>
    public static void SetWorldData(long itemId, string data)
    {
        GetOrAdd(itemId).worldData = data;
    }

    /// <summary>
    /// 设置指定幻化药的场景亮度系数覆盖值(show_brightness 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    /// <param name="data">「0.58」/「1.50」形式的系数覆盖值（1=原亮度，&lt;1调暗，&gt;1调亮）</param>
    public static void SetShowBrightness(long itemId, string data)
    {
        GetOrAdd(itemId).showBrightness = data;
    }

    /// <summary>
    /// 清除指定幻化药的全部覆盖(保存写回后/重置为配置值时调用)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    public static void Clear(long itemId)
    {
        dicOverride.Remove(itemId);
    }

    /// <summary>
    /// 只清除指定幻化药的详情UI尺寸覆盖(列表项「还原」单段恢复用)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    public static void ClearUiShowData(long itemId)
    {
        if (dicOverride.TryGetValue(itemId, out OverrideData od)) od.uiShowData = null;
    }

    /// <summary>
    /// 只清除指定幻化药的默认展示(小卡)尺寸覆盖(列表项「还原」单段恢复用)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    public static void ClearShowData(long itemId)
    {
        if (dicOverride.TryGetValue(itemId, out OverrideData od)) od.showData = null;
    }

    /// <summary>
    /// 只清除指定幻化药的世界显示尺寸/偏移覆盖(列表项「还原」单段恢复用)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    public static void ClearWorldData(long itemId)
    {
        if (dicOverride.TryGetValue(itemId, out OverrideData od)) od.worldData = null;
    }

    /// <summary>
    /// 只清除指定幻化药的场景亮度系数覆盖(列表项「还原」恢复配置值用)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    public static void ClearShowBrightness(long itemId)
    {
        if (dicOverride.TryGetValue(itemId, out OverrideData od)) od.showBrightness = null;
    }

    /// <summary>
    /// 清空全部覆盖(列表页「清空全部未保存修改」用)
    /// </summary>
    public static void ClearAll()
    {
        dicOverride.Clear();
    }

    /// <summary>
    /// 是否存在指定幻化药的任一覆盖(面板显示「已修改未保存」标记用)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id</param>
    public static bool HasOverride(long itemId)
    {
        return dicOverride.TryGetValue(itemId, out OverrideData data)
            && (data.uiShowData != null || data.showData != null || data.worldData != null || data.showBrightness != null);
    }

    /// <summary>
    /// 获取全部有覆盖的幻化药完整id(按id排序, 批量保存用)
    /// </summary>
    public static List<long> GetAllDirtyIds()
    {
        List<long> list = new List<long>();
        foreach (var kv in dicOverride)
        {
            if (kv.Value.uiShowData != null || kv.Value.showData != null || kv.Value.worldData != null || kv.Value.showBrightness != null)
                list.Add(kv.Key);
        }
        list.Sort();
        return list;
    }

    #endregion

    #region 查询(供 CreatureBeanPartial 三个 Get 方法优先消费)

    /// <summary>
    /// 尝试获取指定幻化药的详情UI尺寸覆盖值(ui_show_data 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id(0=无幻化, 恒返回false)</param>
    public static bool TryGetUiShowData(long itemId, out string data)
    {
        data = null;
        if (itemId != 0 && dicOverride.TryGetValue(itemId, out OverrideData od) && od.uiShowData != null)
        {
            data = od.uiShowData;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 尝试获取指定幻化药的默认展示(小卡)尺寸覆盖值(show_data 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id(0=无幻化, 恒返回false)</param>
    public static bool TryGetShowData(long itemId, out string data)
    {
        data = null;
        if (itemId != 0 && dicOverride.TryGetValue(itemId, out OverrideData od) && od.showData != null)
        {
            data = od.showData;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 尝试获取指定幻化药的世界显示尺寸/偏移覆盖值(world_data 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id(0=无幻化, 恒返回false)</param>
    public static bool TryGetWorldData(long itemId, out string data)
    {
        data = null;
        if (itemId != 0 && dicOverride.TryGetValue(itemId, out OverrideData od) && od.worldData != null)
        {
            data = od.worldData;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 尝试获取指定幻化药的场景亮度系数覆盖值(show_brightness 键)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id(0=无幻化, 恒返回false)</param>
    /// <returns>有覆盖返回 true 且 data 为系数字符串</returns>
    public static bool TryGetShowBrightness(long itemId, out string data)
    {
        data = null;
        if (itemId != 0 && dicOverride.TryGetValue(itemId, out OverrideData od) && od.showBrightness != null)
        {
            data = od.showBrightness;
            return true;
        }
        return false;
    }

    #endregion

    #region 私有

    /// <summary>
    /// 获取或新建指定幻化药的覆盖数据
    /// </summary>
    private static OverrideData GetOrAdd(long itemId)
    {
        if (!dicOverride.TryGetValue(itemId, out OverrideData data))
        {
            data = new OverrideData();
            dicOverride.Add(itemId, data);
        }
        return data;
    }

    #endregion
}
#endif
