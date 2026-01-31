using System;
using System.Collections.Generic;
public partial class RarityInfoBean
{
    public RarityEnum GetRarityEnum()
    {
        return (RarityEnum)id;
    }
}
public partial class RarityInfoCfg
{
    public static RarityInfoBean GetItemData(RarityEnum key)
    {
        return GetItemData((long)key);
    }
}
