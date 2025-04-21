using System;
using System.Collections.Generic;
public partial class StoreInfoBean
{
    public StoreInfoTypeEnum GetStoreType()
    {
        return (StoreInfoTypeEnum)store_type;
    }
}
public partial class StoreInfoCfg
{
   public static Dictionary<StoreInfoTypeEnum, List<StoreInfoBean>> dicStoreInfoByType;

    /// <summary>
    /// 按类型获取数据
    /// </summary>
    /// <param name="storeInfoType"></param>
    /// <returns></returns>
    public static List<StoreInfoBean> GetStoreInfoByType(StoreInfoTypeEnum storeInfoType)
    {
        if (dicStoreInfoByType == null)
        {
            dicStoreInfoByType = new Dictionary<StoreInfoTypeEnum, List<StoreInfoBean>>();
            var allData = GetAllData();
            allData.ForEach((key, value) =>
            {
                var storeType = value.GetStoreType();
                if (dicStoreInfoByType.TryGetValue(storeType, out var listData))
                {
                    listData.Add(value);
                }
                else
                {
                    dicStoreInfoByType.Add(storeType, new List<StoreInfoBean>() { value });
                }
            });
        }
        if (dicStoreInfoByType.TryGetValue(storeInfoType, out List<StoreInfoBean> listData))
        {
            return listData;
        }
        else
        {
            return null;
        }
    }
}
