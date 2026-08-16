using System.Collections.Generic;

/// <summary>
/// 装备套装配置扩展：槽位解析与整套可装备校验
/// </summary>
public partial class EquipSuitInfoBean
{
    //槽位-道具映射缓存(槽位值为0=空槽不收录)
    protected Dictionary<ItemTypeEnum, long> dicSuitItems;

    #region 槽位解析
    /// <summary>
    /// 获取套装全部槽位道具(key=槽位类型, value=道具ID; 配置0=空槽不收录)，只解析一次并缓存
    /// </summary>
    public Dictionary<ItemTypeEnum, long> GetSuitItems()
    {
        if (dicSuitItems == null)
        {
            dicSuitItems = new Dictionary<ItemTypeEnum, long>();
            AddSuitItem(ItemTypeEnum.Hat, hat);
            AddSuitItem(ItemTypeEnum.Clothes, clothes);
            AddSuitItem(ItemTypeEnum.Pants, pants);
            AddSuitItem(ItemTypeEnum.Shoe, shoe);
            AddSuitItem(ItemTypeEnum.NoseRing, nose_ring);
            AddSuitItem(ItemTypeEnum.FingerRing, finger_ring);
            AddSuitItem(ItemTypeEnum.Weapon, weapon);
        }
        return dicSuitItems;
    }

    /// <summary>
    /// 收录一个槽位道具(道具ID>0才收录)
    /// </summary>
    protected void AddSuitItem(ItemTypeEnum itemType, long itemId)
    {
        if (itemId > 0)
        {
            dicSuitItems[itemType] = itemId;
        }
    }
    #endregion

    #region 可装备校验
    /// <summary>
    /// 该套装是否可被指定生物整套装备：物种匹配(creature_model_id==0=通用) 且 套内每件均通过 CanEquipItem 校验(槽位许可/种族模组/武器类型)；任一件穿不上则整套不可用
    /// </summary>
    /// <param name="creatureInfo">生物配置</param>
    public bool CanEquipFor(CreatureInfoBean creatureInfo)
    {
        if (creatureInfo == null)
        {
            return false;
        }
        //物种匹配(0=通用套装)
        if (creature_model_id != 0 && creature_model_id != creatureInfo.model_id)
        {
            return false;
        }
        //套内每件均可装备(悬空ID视为不可用, 防配置错误)
        foreach (var suitItem in GetSuitItems())
        {
            var itemInfo = ItemsInfoCfg.GetItemData(suitItem.Value);
            if (itemInfo == null || !creatureInfo.CanEquipItem(itemInfo))
            {
                return false;
            }
        }
        return true;
    }
    #endregion
}

public partial class EquipSuitInfoCfg
{
}
