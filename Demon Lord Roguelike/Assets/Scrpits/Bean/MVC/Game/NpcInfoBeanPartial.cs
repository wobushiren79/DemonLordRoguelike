using System;
using System.Collections.Generic;
public partial class NpcInfoBean
{
    protected List<long> equipItems;
    protected List<long> listSkin;
    protected List<CreatureSkinTypeEnum> listSkinType;

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
        if (hasRandomData && skin_random_id != 0)
        {
            var creatureInfoRandomBean = CreatureInfoRandomCfg.GetItemData(skin_random_id);
            List<long> listRandomSkin = creatureInfoRandomBean.GetRandomData(listSkinType);
            if (!listRandomSkin.IsNull())
            {
                listData.AddRange(listRandomSkin);
            }
        }
        return listData;
    }

    public List<long> GetEquipItems()
    {
        if (equipItems.IsNull())
        {
            equipItems = equip_item_ids.SplitForListLong('&');
        }
        return equipItems;
    }

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
}
