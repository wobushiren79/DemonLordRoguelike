

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.UI;

public partial class UIPopupItemInfo : PopupShowCommonView
{
    //属性视图缓存：key=属性类型, value=对应的属性视图
    protected Dictionary<CreatureAttributeTypeEnum, UIViewPopupItemAttribute> dicAttributeView = new Dictionary<CreatureAttributeTypeEnum, UIViewPopupItemAttribute>();

    public override void SetData(object data)
    {
        ItemBean itemData = (ItemBean)data;
        var itemInfo = ItemsInfoCfg.GetItemData(itemData.itemId);
        string itemName = itemInfo.name_language;
        SetIcon(itemData.itemId);
        SetName(itemName);
        SetRarity(itemData.rarity);
        SetNum(itemData, itemInfo);
        SetType(itemData, itemInfo);
        SetAttributes(itemData);
    }

    /// <summary>
    /// 设置属性列表
    /// </summary>
    public void SetAttributes(ItemBean itemData)
    {
        //如果没有属性，直接返回
        if (itemData.dicAttribute == null || itemData.dicAttribute.Count == 0)
        {
            ui_AttributeContent.gameObject.SetActive(false);
            return;
        }

        ui_AttributeContent.gameObject.SetActive(true);

        //先隐藏所有缓存的视图
        foreach (var viewPair in dicAttributeView)
        {
            viewPair.Value.gameObject.SetActive(false);
        }

        //遍历属性数据，显示对应的属性视图
        foreach (var attributePair in itemData.dicAttribute)
        {
            CreatureAttributeTypeEnum attributeType = attributePair.Key;
            float attributeValue = attributePair.Value;

            //跳过值为0的属性
            if (attributeValue == 0)
                continue;

            UIViewPopupItemAttribute attributeView;
            if (dicAttributeView.TryGetValue(attributeType, out attributeView))
            {
                //使用缓存的视图
                attributeView.gameObject.SetActive(true);
                attributeView.SetData(attributeType, attributeValue);
            }
            else
            {
                //创建新的属性视图
                attributeView = CreateAttributeView(attributeType, attributeValue);
                if (attributeView != null)
                {
                    dicAttributeView.Add(attributeType, attributeView);
                }
            }
        }
        UGUIUtil.RefreshUISize(ui_AttributeContent);
    }

    /// <summary>
    /// 创建属性视图
    /// </summary>
    protected UIViewPopupItemAttribute CreateAttributeView(CreatureAttributeTypeEnum attributeType, float attributeValue)
    {
        if (ui_UIViewPopupItemAttribute == null)
            return null;

        GameObject newObj = Instantiate(ui_UIViewPopupItemAttribute.gameObject, ui_AttributeContent);
        newObj.gameObject.SetActive(true);
        UIViewPopupItemAttribute newView = newObj.GetComponent<UIViewPopupItemAttribute>();
        if (newView != null)
        {
            newView.SetData(attributeType, attributeValue);
        }
        return newView;
    }

    /// <summary>
    /// 设置数量
    /// </summary>
    public void SetNum(ItemBean itemData, ItemsInfoBean itemInfo)
    {
        // 如果道具上限为1，不显示数量
        if (itemInfo != null && itemInfo.num_max == 1)
        {
            ui_ItemNum.gameObject.SetActive(false);
        }
        else
        {
            ui_ItemNum.gameObject.SetActive(true);
            ui_ItemNum.text = $"{itemData.itemNum}";
        }
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon(long itemId)
    {
        IconHandler.Instance.SetItemIcon(itemId, ui_Icon);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        string itemName = TextHandler.Instance.GetTextById(2000010);
        ui_NameText.text = string.Format(itemName, name);
    }

    /// <summary>
    /// 设置稀有度
    /// </summary>
    public void SetRarity(int rarity)
    {
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        string rarityName = rarityInfo != null ? rarityInfo.name_language : "";
        string rarityText = TextHandler.Instance.GetTextById(2000008);
        ui_RarityText.text = string.Format(rarityText, rarityName);
        // 设置稀有度文本颜色
        if (rarityInfo != null && !string.IsNullOrEmpty(rarityInfo.ui_board_color))
        {
            Color textColor = ColorUtil.ParseHtmlString(rarityInfo.ui_board_color);
            ui_RarityText.color = textColor;
            ui_IconBG.color = textColor;
        }
    }

    /// <summary>
    /// 设置所属种族
    /// </summary>
    public void SetType(ItemBean itemData, ItemsInfoBean itemInfo)
    {
        string raceName;
        if (itemInfo == null || itemInfo.creature_model_id == 0)
        {
            // 使用通用文本
            raceName = TextHandler.Instance.GetTextById(90001);
        }
        else
        {
            // 查询CreatureModel获取种族名称
            var creatureModel = CreatureModelCfg.GetItemData(itemInfo.creature_model_id);
            raceName = creatureModel != null ? creatureModel.name_language : TextHandler.Instance.GetTextById(90001);
        }
        // 根据userType读取多语言文本并追加
        var userTypeEnum = itemData.GetUserTypeEnum();
        string userTypeText = userTypeEnum.GetLanguageText();
        if (!string.IsNullOrEmpty(userTypeText))
        {
            raceName = $"{raceName}{userTypeText}";
        }
        string typeText = TextHandler.Instance.GetTextById(2000009);
        ui_TypeText.text = string.Format(typeText, raceName);
    }
}