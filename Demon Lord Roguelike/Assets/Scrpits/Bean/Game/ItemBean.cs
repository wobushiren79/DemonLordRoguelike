using UnityEngine;

public partial class ItemBean
{
    //道具ID
    public long itemId;
    //道具数量
    public int itemNum;
    //道具品质
    public int rarity;

    public ItemBean()
    {

    }

    public ItemBean(long itemId, int itemNum = 1, int rarity = 1)
    {
        this.itemId = itemId;
        this.itemNum = itemNum;
        this.rarity = rarity;
    }

    /// <summary>
    /// 获取道具类型
    /// </summary>
    public ItemTypeEnum GetItemType()
    {
        return itemsInfo.GetItemType();
    }
}
