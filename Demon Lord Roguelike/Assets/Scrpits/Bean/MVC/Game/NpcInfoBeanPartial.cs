using System;
using System.Collections.Generic;
public partial class NpcInfoBean
{
    public List<long> equipItems;

    public List<long> GetEquipItems()
    {
        if (equipItems.IsNull())
        {
            equipItems = new List<long>();
            equipItems = equip_item_ids.SplitForListLong('&');
        }
        return equipItems;
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
