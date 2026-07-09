using System;
using UnityEngine.UI;

/// <summary>
/// 排序筛选弹窗 - 单个筛选项。支持三种模式:
/// 排序键模式(SetData):点击回传 OrderFilterTypeEnum;稀有度筛选模式(SetDataForRarity):点击回传 RarityEnum;道具类型筛选模式(SetDataForItemType):点击回传 ItemTypeEnum。
/// 点击切换选中状态,选中时显示 CheckBoxSubmit;名称通过 NameItem 内联显示(无 POPUP)。
/// </summary>
public partial class UIViewDialogOrderFilterItem : BaseUIView
{
    #region 数据
    //该项当前所处的模式(决定点击回传哪种类型)
    protected FilterItemMode itemMode;
    //该项代表的排序键(排序键模式)
    protected OrderFilterTypeEnum filterType;
    //该项代表的稀有度(稀有度筛选模式)
    protected RarityEnum rarityType;
    //该项代表的道具类型(道具类型筛选模式)
    protected ItemTypeEnum itemTypeType;
    //排序键点击回调
    protected Action<OrderFilterTypeEnum> actionForClick;
    //稀有度点击回调
    protected Action<RarityEnum> actionForClickRarity;
    //道具类型点击回调
    protected Action<ItemTypeEnum> actionForClickItemType;

    /// <summary>
    /// 筛选项模式:排序键 / 稀有度 / 道具类型
    /// </summary>
    protected enum FilterItemMode
    {
        SortKey,
        Rarity,
        ItemType,
    }
    #endregion

    #region 公有方法
    /// <summary>
    /// 设置数据(排序键项):NameItem 显示该排序类型的多语言名称,点击回传 filterType。
    /// </summary>
    /// <param name="filterType">该项代表的排序键</param>
    /// <param name="isSelect">是否选中</param>
    /// <param name="actionForClick">点击回调</param>
    public void SetData(OrderFilterTypeEnum filterType, bool isSelect, Action<OrderFilterTypeEnum> actionForClick)
    {
        itemMode = FilterItemMode.SortKey;
        this.filterType = filterType;
        this.actionForClick = actionForClick;
        this.actionForClickRarity = null;
        this.actionForClickItemType = null;
        SetNameItem(GetFilterName(filterType));
        SetSelect(isSelect);
    }

    /// <summary>
    /// 设置数据(稀有度筛选项):NameItem 显示稀有度名称,点击回传 rarityType(多选)。
    /// </summary>
    /// <param name="rarity">该项代表的稀有度</param>
    /// <param name="rarityName">稀有度多语言名称</param>
    /// <param name="isSelect">是否选中</param>
    /// <param name="actionForClickRarity">点击回调</param>
    public void SetDataForRarity(RarityEnum rarity, string rarityName, bool isSelect, Action<RarityEnum> actionForClickRarity)
    {
        itemMode = FilterItemMode.Rarity;
        this.rarityType = rarity;
        this.actionForClickRarity = actionForClickRarity;
        this.actionForClick = null;
        this.actionForClickItemType = null;
        SetNameItem(rarityName);
        SetSelect(isSelect);
    }

    /// <summary>
    /// 设置数据(道具类型筛选项):NameItem 显示道具类型名称,点击回传 itemType(多选)。
    /// </summary>
    /// <param name="itemType">该项代表的道具类型</param>
    /// <param name="itemTypeName">道具类型多语言名称</param>
    /// <param name="isSelect">是否选中</param>
    /// <param name="actionForClickItemType">点击回调</param>
    public void SetDataForItemType(ItemTypeEnum itemType, string itemTypeName, bool isSelect, Action<ItemTypeEnum> actionForClickItemType)
    {
        itemMode = FilterItemMode.ItemType;
        this.itemTypeType = itemType;
        this.actionForClickItemType = actionForClickItemType;
        this.actionForClick = null;
        this.actionForClickRarity = null;
        SetNameItem(itemTypeName);
        SetSelect(isSelect);
    }

    /// <summary>
    /// 设置名称文本
    /// </summary>
    /// <param name="content">名称内容</param>
    public void SetNameItem(string content)
    {
        if (ui_NameItem != null)
            ui_NameItem.text = content;
    }

    /// <summary>
    /// 设置选中状态(选中显示对勾 CheckBoxSubmit)
    /// </summary>
    /// <param name="isSelect">是否选中</param>
    public void SetSelect(bool isSelect)
    {
        if (ui_CheckBoxSubmit != null)
            ui_CheckBoxSubmit.gameObject.SetActive(isSelect);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 获取排序键对应的多语言名称
    /// </summary>
    /// <param name="filterType">排序键</param>
    /// <returns>名称文本</returns>
    protected string GetFilterName(OrderFilterTypeEnum filterType)
    {
        switch (filterType)
        {
            case OrderFilterTypeEnum.Rarity:
                return TextHandler.Instance.GetTextById(2000004);
            case OrderFilterTypeEnum.Level:
                return TextHandler.Instance.GetTextById(2000005);
            case OrderFilterTypeEnum.Lineup:
                return TextHandler.Instance.GetTextById(2000006);
            case OrderFilterTypeEnum.Name:
                return TextHandler.Instance.GetTextById(2000007);
            case OrderFilterTypeEnum.Class:
                return TextHandler.Instance.GetTextById(2000011);
            case OrderFilterTypeEnum.Damage:
                return TextHandler.Instance.GetTextById(50001);
            case OrderFilterTypeEnum.Kill:
                return TextHandler.Instance.GetTextById(50002);
            case OrderFilterTypeEnum.DamageReceived:
                return TextHandler.Instance.GetTextById(50004);
            case OrderFilterTypeEnum.Exp:
                return TextHandler.Instance.GetTextById(50003);
        }
        return "";
    }
    #endregion

    #region 按钮点击
    /// <summary>
    /// 按钮点击:按模式把自身排序键/稀有度回传给弹窗(回调为空则点击无响应)
    /// </summary>
    /// <param name="viewButton">被点击的按钮</param>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        switch (itemMode)
        {
            case FilterItemMode.Rarity:
                actionForClickRarity?.Invoke(rarityType);
                break;
            case FilterItemMode.ItemType:
                actionForClickItemType?.Invoke(itemTypeType);
                break;
            default:
                actionForClick?.Invoke(filterType);
                break;
        }
    }
    #endregion
}
