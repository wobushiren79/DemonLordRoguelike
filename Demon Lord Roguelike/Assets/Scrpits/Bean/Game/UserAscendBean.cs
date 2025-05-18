using System;
using System.Collections.Generic;

[Serializable]
public class UserAscendBean
{
    public Dictionary<int, UserAscendDetailsBean> dicAscendData = new Dictionary<int, UserAscendDetailsBean>();

    /// <summary>
    /// 检测是否右进阶数据
    /// </summary>
    public bool CheckHasAscend(int index)
    {
        return dicAscendData.ContainsKey(index);
    }

    /// <summary>
    /// 获取进阶数据
    /// </summary>
    public UserAscendDetailsBean GetAscendData(int index)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            return targetData;
        }
        return null;
    }
}

[Serializable]
public class UserAscendDetailsBean
{
    public int index;
    public float progress;
    public string creatureId;
}