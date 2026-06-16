using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就管理器
/// 负责缓存成就配置数据与提供查询接口
/// </summary>
public partial class AchievementManager : BaseManager
{
    /// <summary>
    /// 是否已经初始化(Handler 完成事件注册)
    /// </summary>
    public bool isInited;

    /// <summary>
    /// 排序好的成就列表(每个可升级成就一行, 即UI卡片数据源; 缓存)
    /// </summary>
    private List<AchievementInfoBean> _cachedSortedList;

    public virtual void Awake()
    {
    }

    /// <summary>
    /// 获取排序后的成就列表(每个可升级成就一行, UI 卡片直接用此列表)
    /// </summary>
    public List<AchievementInfoBean> GetAllAchievementsSorted()
    {
        if (_cachedSortedList == null)
        {
            _cachedSortedList = AchievementInfoCfg.GetAllListSorted();
        }
        return _cachedSortedList;
    }

    /// <summary>
    /// 清理缓存
    /// </summary>
    public void ClearCache()
    {
        _cachedSortedList = null;
    }
}
