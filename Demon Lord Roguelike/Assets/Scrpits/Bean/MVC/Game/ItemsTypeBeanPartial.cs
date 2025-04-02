using System;
using System.Collections.Generic;
public partial class ItemsTypeBean
{
}
public partial class ItemsTypeCfg
{
    public static ItemsTypeBean GetItemData(ItemTypeEnum itemType)
    {
        return GetItemData((int)itemType);
    }
}
