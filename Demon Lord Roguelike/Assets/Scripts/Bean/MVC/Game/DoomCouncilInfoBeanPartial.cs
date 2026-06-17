using System;
using System.Collections.Generic;
public partial class DoomCouncilInfoBean
{
    /// <summary>
    /// 获取议会人数区间(解析 council_num 的 "min,max" 格式; 解析失败时默认 1,1)
    /// 注意: council_num 字段由 Excel(excel_doom_council_info) 导出生成, 需先运行配置导出工具
    /// </summary>
    /// <param name="min">最小议员人数</param>
    /// <param name="max">最大议员人数</param>
    public void GetCouncilNumRange(out int min, out int max)
    {
        min = 1;
        max = 1;
        if (string.IsNullOrEmpty(council_num))
        {
            return;
        }
        var arr = council_num.Split(',');
        if (arr.Length >= 1 && int.TryParse(arr[0].Trim(), out int parseMin))
        {
            min = parseMin;
        }
        if (arr.Length >= 2 && int.TryParse(arr[1].Trim(), out int parseMax))
        {
            max = parseMax;
        }
        else
        {
            max = min;
        }
        if (min < 1)
        {
            min = 1;
        }
        if (max < min)
        {
            max = min;
        }
    }

    /// <summary>
    /// 在议会人数区间内随机一个本场议会的议员人数(含上下限)
    /// </summary>
    /// <returns>本场议会的议员人数</returns>
    public int GetRandomCouncilNum()
    {
        GetCouncilNumRange(out int min, out int max);
        return UnityEngine.Random.Range(min, max + 1);
    }
}
public partial class DoomCouncilInfoCfg
{
}
