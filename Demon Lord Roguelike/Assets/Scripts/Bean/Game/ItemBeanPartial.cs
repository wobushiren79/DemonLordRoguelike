using System;
using UnityEngine;

public partial class ItemBean
{
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected ItemsInfoBean _itemsInfo;

    [Newtonsoft.Json.JsonIgnore]
    public ItemsInfoBean itemsInfo
    {
        get
        {
            if(_itemsInfo == null)
            {
                _itemsInfo = ItemsInfoCfg.GetItemData(itemId);
                if(_itemsInfo == null)
                {
                    LogUtil.LogError($"获取道具数据失败 id_{itemId}");
                }
            }
            return _itemsInfo;
        }
    }
}
