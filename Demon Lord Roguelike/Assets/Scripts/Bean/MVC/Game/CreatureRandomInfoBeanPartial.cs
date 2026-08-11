using System;
using System.Collections.Generic;
public partial class CreatureRandomInfoBean
{
    protected Dictionary<CreatureSkinTypeEnum, List<long>> dicRandomData;

    /// <summary>
    /// 获取全部随机皮肤池(按部位分组，带缓存；注意填充必须只在首次执行，否则每次调用会往缓存列表重复追加同一批id)
    /// </summary>
    public Dictionary<CreatureSkinTypeEnum, List<long>> GetAllRandomData()
    {
        if (dicRandomData == null)
        {
            dicRandomData = new Dictionary<CreatureSkinTypeEnum, List<long>>();
            List<long> listRandomData = skin_random_data.SplitForListLong(',', '-');
            for (int i = 0; i < listRandomData.Count; i++)
            {
                var itemId = listRandomData[i];
                var itemInfo = CreatureModelInfoCfg.GetItemData(itemId);
                if (itemInfo == null)
                {
                    continue;
                }
                if (dicRandomData.TryGetValue(itemInfo.GetPartType(), out var itemList))
                {
                    itemList.Add(itemId);
                }
                else
                {
                    dicRandomData.Add(itemInfo.GetPartType(), new List<long>() { itemId });
                }
            }
        }
        return dicRandomData;
    }

    /// <summary>
    /// 从随机皮肤池抽一套皮肤(每个部位分组等概率抽1个)，可排除指定部位
    /// </summary>
    /// <param name="excludePartType">要排除的部位类型(如固有皮肤已占用的部位)</param>
    public List<long> GetRandomData(List<CreatureSkinTypeEnum> excludePartType = null)
    {
        List<long> listSkinRandom = new List<long>();
        var allRandomData = GetAllRandomData();
        foreach (var item in allRandomData)
        {
            //是否要排除固定类型
            if (!excludePartType.IsNull() && excludePartType.Contains(item.Key))
            {
                continue;
            }
            List<long> listSkin = item.Value;
            int targetSkinRandomIndex = UnityEngine.Random.Range(0, listSkin.Count);
            long targetSkinRandom = listSkin[targetSkinRandomIndex];
            listSkinRandom.Add(targetSkinRandom);
        }
        return listSkinRandom;
    }
}
public partial class CreatureRandomInfoCfg
{
}
