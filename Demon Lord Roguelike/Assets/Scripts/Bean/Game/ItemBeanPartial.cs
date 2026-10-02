using System;
using UnityEngine;

public partial class ItemBean
{
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected ItemsInfoBean _itemsInfo;

    /// <summary>
    /// 配置是否已查询过(含失败):失败同样标记,避免排序/遍历等高频访问反复查表并刷错误日志。
    /// 配置缺失通常源于所属Mod未开启,运行期内开关不变(重开Mod需重启游戏),故缓存失败结果是安全的
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected bool _isItemsInfoQueried;

    [Newtonsoft.Json.JsonIgnore]
    public ItemsInfoBean itemsInfo
    {
        get
        {
            if (!_isItemsInfoQueried)
            {
                _isItemsInfoQueried = true;
                _itemsInfo = ItemsInfoCfg.GetItemData(itemId);
                if (_itemsInfo == null)
                {
                    LogUtil.LogError($"获取道具数据失败 id_{itemId}(可能来自未开启/已删除的Mod)");
                }
            }
            return _itemsInfo;
        }
    }
}
