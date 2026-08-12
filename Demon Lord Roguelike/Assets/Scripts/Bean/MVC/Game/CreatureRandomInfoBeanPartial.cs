using System;
using System.Collections.Generic;
public partial class CreatureRandomInfoBean
{
    protected Dictionary<CreatureSkinTypeEnum, List<long>> dicRandomData;
    protected Dictionary<ItemTypeEnum, List<long>> dicRandomEquipData;

    /// <summary>
    /// 获取随机池类型(0=皮肤池 1=装备池)
    /// </summary>
    public CreatureRandomTypeEnum GetRandomType()
    {
        return (CreatureRandomTypeEnum)random_type;
    }

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

    /// <summary>
    /// 获取全部随机装备池(按道具类型分组，带缓存；解析 equip_random_data，仅装备池类型有效)
    /// </summary>
    public Dictionary<ItemTypeEnum, List<long>> GetAllRandomEquipData()
    {
        if (dicRandomEquipData == null)
        {
            dicRandomEquipData = new Dictionary<ItemTypeEnum, List<long>>();
            List<long> listItemIds = equip_random_data.SplitForListLong(',', '-');
            for (int i = 0; i < listItemIds.Count; i++)
            {
                var itemInfo = ItemsInfoCfg.GetItemData(listItemIds[i]);
                if (itemInfo == null)
                {
                    continue;
                }
                var itemType = itemInfo.GetItemType();
                if (dicRandomEquipData.TryGetValue(itemType, out var itemList))
                {
                    itemList.Add(listItemIds[i]);
                }
                else
                {
                    dicRandomEquipData.Add(itemType, new List<long>() { listItemIds[i] });
                }
            }
        }
        return dicRandomEquipData;
    }

    /// <summary>
    /// 从随机装备池为指定生物抽一套装备：每个槽位在「空 + 池内该生物可装备的道具」中等概率抽1个
    /// (如池内该槽位有3件可装备，则裸体概率=1/4)；可装备性按 部位类型/种族模组/武器类型 过滤
    /// </summary>
    /// <param name="creatureInfo">生物配置</param>
    /// <returns>抽中的装备道具配置列表(允许缺槽/全空)</returns>
    public List<ItemsInfoBean> GetRandomEquipItemInfos(CreatureInfoBean creatureInfo)
    {
        List<ItemsInfoBean> listData = new List<ItemsInfoBean>();
        if (creatureInfo == null)
        {
            return listData;
        }
        var allEquipData = GetAllRandomEquipData();
        foreach (var item in allEquipData)
        {
            //过滤出该生物可装备的道具
            List<ItemsInfoBean> listCanEquip = new List<ItemsInfoBean>();
            List<long> listItemIds = item.Value;
            for (int i = 0; i < listItemIds.Count; i++)
            {
                var itemInfo = ItemsInfoCfg.GetItemData(listItemIds[i]);
                if (itemInfo != null && creatureInfo.CanEquipItem(itemInfo))
                {
                    listCanEquip.Add(itemInfo);
                }
            }
            //空槽参与等概率随机：随机下标==可装备数量 时该槽位留空(裸体)
            int randomIndex = UnityEngine.Random.Range(0, listCanEquip.Count + 1);
            if (randomIndex < listCanEquip.Count)
            {
                listData.Add(listCanEquip[randomIndex]);
            }
        }
        return listData;
    }
}
public partial class CreatureRandomInfoCfg
{
}
