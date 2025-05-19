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

    /// <summary>
    /// 添加进阶数据
    /// </summary>
    public UserAscendDetailsBean AddAscendData(int index, CreatureBean creatureData)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            LogUtil.LogError($"添加进阶数据错误,已经存在index_{index}的数据 progress_{targetData.progress} creatureId_{targetData.creatureId}");
        }
        UserAscendDetailsBean newData = new UserAscendDetailsBean();
        newData.progress = 0;
        newData.index = index;
        newData.creatureId = creatureData.creatureId;
        dicAscendData[index] = newData;
        return newData;
    }

    /// <summary>
    /// 移除进阶数据
    /// </summary>
    public void RemoveAscendData(int index)
    {
        if (dicAscendData.ContainsKey(index))
        {
            dicAscendData.Remove(index);
        }
        else
        {
            LogUtil.LogError($"溢出进阶数据错误,没有数据index_{index}");
        }
    }
}

[Serializable]
public class UserAscendDetailsBean
{
    public int index;
    public float progress;
    public string creatureId;
}