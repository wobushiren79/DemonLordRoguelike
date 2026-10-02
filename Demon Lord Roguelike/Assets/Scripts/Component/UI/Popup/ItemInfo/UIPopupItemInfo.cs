

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.UI;
using Spine;

public partial class UIPopupItemInfo : PopupShowCommonView
{
    //属性视图缓存：key=属性类型, value=对应的属性视图
    protected Dictionary<CreatureAttributeTypeEnum, UIViewPopupItemAttribute> dicAttributeView = new Dictionary<CreatureAttributeTypeEnum, UIViewPopupItemAttribute>();

    public override void SetData(object data)
    {
        ItemBean itemData = (ItemBean)data;
        var itemInfo = ItemsInfoCfg.GetItemData(itemData.itemId);
        //配置缺失(如所属Mod未开启)时名字兜底显示道具ID,其余各Set方法内部均已判空容错
        string itemName = itemInfo != null ? itemInfo.name_language : $"ID:{itemData.itemId}";
        SetIcon(itemData.itemId);
        SetName(itemName);
        SetRarity(itemData.rarity);
        SetNum(itemData, itemInfo);
        SetType(itemData, itemInfo);
        SetAttributes(itemData);
        SetJuiceExp(itemData, itemInfo);
        SetTransformPreview(itemInfo);
    }

    #region 幻化药形象预览
    /// <summary>
    /// 设置幻化药形象预览:仅幻化药(TransformPotion 且 other_data 非空)显示预览区,
    /// 展示该药的 show(战斗/小卡形象)与 ui_show(详情高清形象)两段 spine,两段按配置键有无独立显隐
    /// </summary>
    public void SetTransformPreview(ItemsInfoBean itemInfo)
    {
        //prefab 未配置预览区时容错跳过(字段经 AutoLink 绑定)
        if (ui_TransformPreviewContent == null)
            return;
        bool isTransformPotion = itemInfo != null && itemInfo.GetItemType() == ItemTypeEnum.TransformPotion && !itemInfo.other_data.IsNull();
        ui_TransformPreviewContent.gameObject.SetActive(isTransformPotion);
        if (!isTransformPotion)
            return;
        TransformOtherData transformData = CreatureBean.ParseTransformOtherData(itemInfo.other_data);
        SetTransformPreviewForShow(transformData);
        SetTransformPreviewForUIShow(transformData);
    }

    /// <summary>
    /// 设置 show 段预览(战斗/小卡形象):有 show_res 键才显示(仅详情UI幻化道具缺省该键,本区隐藏)
    /// </summary>
    protected void SetTransformPreviewForShow(TransformOtherData transformData)
    {
        bool hasShow = !transformData.showRes.IsNull();
        ui_ShowArea.gameObject.SetActive(hasShow);
        if (!hasShow)
            return;
        ui_ShowLabel.text = TextHandler.Instance.GetTextById(61022);
        SpineHandler.Instance.SetSkeletonDataAsset(ui_ShowSpine, transformData.showRes);
        //待机动画:idle_anim 键缺省时传 null,框架按该骨架实际动画列表候选解析
        SpineHandler.Instance.PlayAnim(ui_ShowSpine, SpineAnimationStateEnum.Idle, true, animNameAppoint: transformData.idleAnim);
        ApplyTransformPreviewSize(ui_ShowSpine, transformData.showData, GameUIUtil.cardContentHeightForS);
    }

    /// <summary>
    /// 设置 ui_show 段预览(详情高清形象):有 ui_show_res 键才显示;支持 ui_show_skin 换肤(「|」分隔多皮肤叠加)
    /// </summary>
    protected void SetTransformPreviewForUIShow(TransformOtherData transformData)
    {
        bool hasUIShow = !transformData.uiShowRes.IsNull();
        ui_UIShowArea.gameObject.SetActive(hasUIShow);
        if (!hasUIShow)
            return;
        ui_UIShowLabel.text = TextHandler.Instance.GetTextById(61023);
        SpineHandler.Instance.SetSkeletonDataAsset(ui_UIShowSpine, transformData.uiShowRes);
        //ui_show_skin 换肤:单皮肤整皮替换,多皮肤叠加(同 CreatureHandler.SetCreatureData 处理)
        if (!transformData.uiShowSkin.IsNull())
        {
            string[] uiShowSkins = transformData.uiShowSkin.Split('|');
            if (uiShowSkins.Length > 1)
                SpineHandler.Instance.ChangeSkeletonSkin(ui_UIShowSpine.Skeleton, uiShowSkins);
            else
                SpineHandler.Instance.ChangeSkeletonSkin(ui_UIShowSpine.Skeleton, transformData.uiShowSkin);
        }
        else if (ui_UIShowSpine.Skeleton != null && ui_UIShowSpine.Skeleton.Skin != null)
        {
            //无皮肤键但当前挂着指定皮肤时重置回骨架默认外观,防 popup 复用残留上一预览药的皮肤(同骨架异药场景)
            ui_UIShowSpine.Skeleton.SetSkin((Skin)null);
            ui_UIShowSpine.Skeleton.SetupPoseSlots();
        }
        //待机动画:ui_show_idle_anim 键缺省时传 null,框架按该骨架实际动画列表候选解析
        SpineHandler.Instance.PlayAnim(ui_UIShowSpine, SpineAnimationStateEnum.Idle, true, animNameAppoint: transformData.uiShowIdleAnim);
        ApplyTransformPreviewSize(ui_UIShowSpine, transformData.uiShowData, GameUIUtil.cardContentHeightForB);
    }

    /// <summary>
    /// 应用预览尺寸(走 GameUIUtil.ApplyCardIconSizeFit 卡片图标尺寸等比适配):预览 spine 节点与大/小卡 ui_Icon 同 pivot(0.5,0)+父容器中心锚定,
    /// 尺寸键 scale;pos 直接套用并同乘「预览框高/卡片容器高」系数,即还原小卡(SetCreatureUIForSimple)/大卡(SetCreatureUIForDetails)的显示效果;
    /// Skeleton.X/Y 清零防历史方案残留;pos 落 anchoredPosition 需 spine 节点不在 LayoutGroup 控制下(现挂 ShowRectContent 绝对定位)。
    /// </summary>
    protected void ApplyTransformPreviewSize(SkeletonGraphicExtend spineGraphic, string sizeData, float cardContentHeight)
    {
        GameUIUtil.ApplyCardIconSizeFit(spineGraphic.rectTransform, sizeData, cardContentHeight);
        if (spineGraphic.Skeleton != null)
        {
            spineGraphic.Skeleton.X = 0;
            spineGraphic.Skeleton.Y = 0;
        }
    }
    #endregion

    /// <summary>
    /// 设置魔汁经验行:仅魔汁(ItemTypeEnum.Juice)显示「经验+X」,其余道具隐藏(魔汁无属性,属性区自动隐藏互斥)
    /// </summary>
    public void SetJuiceExp(ItemBean itemData, ItemsInfoBean itemInfo)
    {
        //prefab 未配置该元素时容错跳过(字段经 AutoLink 绑定)
        if (ui_JuiceExpText == null)
            return;
        bool isJuice = itemInfo != null && itemInfo.GetItemType() == ItemTypeEnum.Juice;
        ui_JuiceExpText.gameObject.SetActive(isJuice);
        if (isJuice)
            ui_JuiceExpText.text = string.Format(TextHandler.Instance.GetTextById(61017), itemData.juicerExp);
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
        ui_NameText.text = name;
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
        // 设置稀有度文本颜色（用道具专用单色 ui_board_color_item，非渐变对 ui_board_color——后者含逗号无法直接解析）
        if (rarityInfo != null && !string.IsNullOrEmpty(rarityInfo.ui_board_color_item))
        {
            Color textColor = ColorUtil.ParseHtmlString(rarityInfo.ui_board_color_item);
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