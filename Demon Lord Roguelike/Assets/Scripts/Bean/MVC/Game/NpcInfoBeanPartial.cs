using System;
using System.Collections.Generic;
public partial class NpcInfoBean
{
    protected List<long> equipItems;
    protected List<long> listSkin;
    protected List<CreatureSkinTypeEnum> listSkinType;
    protected List<long> listTitle;

    /// <summary>
    /// 获取议员评级
    /// </summary>
    /// <returns></returns>
    public int GetCouncilorRatings()
    {
        int rating = 1;
        if (councilor_ratings != 0)
        {
            rating = councilor_ratings;
        }
        return rating;
    }

    /// <summary>
    /// 获取称号
    /// </summary>
    public List<long> GetTitles()
    {
        if (listTitle.IsNull())
        {
            listTitle = new List<long>();
            if (!title_data.IsNull())
            {
                listTitle = title_data.SplitForListLong('&');
            }
        }
        return listTitle;
    }

    /// <summary>
    /// 获取皮肤
    /// </summary>
    /// <param name="hasRandomData">是否包含随机皮肤</param>
    /// <returns></returns>
    public List<long> GetSkins(bool hasRandomData = true)
    {
        List<long> listData = new List<long>();
        //先添加固有皮肤
        if (listSkin.IsNull())
        {
            listSkin = skin_data.SplitForListLong('&');
            listSkinType = new List<CreatureSkinTypeEnum>();
            for ( int i = 0; i < listSkin.Count; i++)
            {
                var skinId = listSkin[i];
                var modelInfo = CreatureModelInfoCfg.GetItemData(skinId);
                listSkinType.Add(modelInfo.GetPartType());
            }
        }
        if (!listSkin.IsNull())
        {
           listData.AddRange(listSkin);   
        }
        //再添加随机皮肤
        if (hasRandomData && creature_random_id != 0)
        {
            var creatureInfoRandomBean = CreatureRandomInfoCfg.GetItemData(creature_random_id);
            List<long> listRandomSkin = creatureInfoRandomBean.GetRandomData(listSkinType);
            if (!listRandomSkin.IsNull())
            {
                listData.AddRange(listRandomSkin);
            }
        }
        return listData;
    }

    /// <summary>
    /// 获取装备
    /// </summary>
    /// <returns></returns>
    public List<long> GetEquipItems()
    {
        if (equipItems.IsNull())
        {
            equipItems = equip_item_ids.SplitForListLong('&');
        }
        return equipItems;
    }

    /// <summary>
    /// 获取装备
    /// </summary>
    /// <returns></returns>
    public List<ItemsInfoBean> GetEquipItemsInfo()
    {
        List<ItemsInfoBean> listData = new List<ItemsInfoBean>();
        var equipItems = GetEquipItems();
        for (int i = 0; i < equipItems.Count; i++)
        {
            var itemId = equipItems[i];
            var itemData = ItemsInfoCfg.GetItemData(itemId);
            listData.Add(itemData);
        }
        return listData;
    }

    /// <summary>
    /// 获取NPC类型
    /// </summary>
    /// <returns></returns>
    public NpcTypeEnum GetNpcType()
    {
        return (NpcTypeEnum)npc_type;
    }
}

public partial class NpcInfoCfg
{
    /// <summary>
    /// 通过类型获取NPC数据
    /// </summary>
    /// <param name="npcType"></param>
    /// <returns></returns>
    public  static List<NpcInfoBean> GetNpcInfosByType(NpcTypeEnum npcType)
    {
        List<NpcInfoBean> listData = new List<NpcInfoBean>();
        var allData = GetAllArrayData();
        for (int i = 0; i < allData.Length; i++)
        {
            var itemData = allData[i];
            if (itemData.GetNpcType() == npcType)
            {
                listData.Add(itemData);
            }
        }
        return listData;
    }

    //议会随机议员评级出现权重: 评级1~5 对应 50/30/15/10/5 (合计110, 抽取时按权重归一化)
    private static readonly int[] councilorRatingWeights = { 50, 30, 15, 10, 5 };

    /// <summary>
    /// 按权重随机一个议会随机议员的评级(1~5)
    /// 权重: 1级50 2级30 3级15 4级10 5级5 (合计110, 归一化抽取)
    /// </summary>
    /// <returns>评级(1~5)</returns>
    public static int GetRandomCouncilorRating()
    {
        int total = 0;
        for (int i = 0; i < councilorRatingWeights.Length; i++)
        {
            total += councilorRatingWeights[i];
        }
        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < councilorRatingWeights.Length; i++)
        {
            acc += councilorRatingWeights[i];
            if (roll < acc)
            {
                return i + 1;
            }
        }
        return councilorRatingWeights.Length;
    }

    /// <summary>
    /// 随机抽取一个【议会随机NPC】: 随机一种生物 + 按权重随机评级(1~5), 取对应的随机议员配置
    /// </summary>
    /// <returns>随机议员的 NpcInfoBean; 没有可用数据时返回 null</returns>
    public static NpcInfoBean GetRandomCouncilorNpc()
    {
        var listRandomCouncilor = GetNpcInfosByType(NpcTypeEnum.CouncilorRandom);
        if (listRandomCouncilor.IsNull())
        {
            return null;
        }
        //收集所有出现过的生物id
        List<long> listCreatureId = new List<long>();
        for (int i = 0; i < listRandomCouncilor.Count; i++)
        {
            long creatureId = listRandomCouncilor[i].creature_id;
            if (!listCreatureId.Contains(creatureId))
            {
                listCreatureId.Add(creatureId);
            }
        }
        if (listCreatureId.Count == 0)
        {
            return null;
        }
        //随机一种生物
        long targetCreatureId = listCreatureId[UnityEngine.Random.Range(0, listCreatureId.Count)];
        //按权重随机评级
        int targetRating = GetRandomCouncilorRating();
        //取对应(生物+评级)的议员配置; 找不到精确评级时退化为该生物任意一条
        NpcInfoBean fallback = null;
        for (int i = 0; i < listRandomCouncilor.Count; i++)
        {
            var itemInfo = listRandomCouncilor[i];
            if (itemInfo.creature_id != targetCreatureId)
            {
                continue;
            }
            fallback = itemInfo;
            if (itemInfo.GetCouncilorRatings() == targetRating)
            {
                return itemInfo;
            }
        }
        return fallback;
    }

    /// <summary>
    /// 随机抽取一个【议会固定NPC】, 没有时返回 null
    /// </summary>
    /// <returns>固定议员的 NpcInfoBean; 没有可用数据时返回 null</returns>
    public static NpcInfoBean GetRandomFixedCouncilorNpc()
    {
        var listFixed = GetNpcInfosByType(NpcTypeEnum.Councilor);
        if (listFixed.IsNull() || listFixed.Count == 0)
        {
            return null;
        }
        return listFixed[UnityEngine.Random.Range(0, listFixed.Count)];
    }
}
