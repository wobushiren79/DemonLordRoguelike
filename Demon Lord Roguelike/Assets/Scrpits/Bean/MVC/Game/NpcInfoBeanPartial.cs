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
}
public partial class NpcInfoCfg
{
}
