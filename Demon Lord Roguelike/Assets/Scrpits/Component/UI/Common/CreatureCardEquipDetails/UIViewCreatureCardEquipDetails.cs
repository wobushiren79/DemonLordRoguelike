

using System.Collections.Generic;

public partial class UIViewCreatureCardEquipDetails : BaseUIView
{
    //展示的装备UI
    protected Dictionary<ItemTypeEnum, UIViewItemEquip> dicShowEquipView = new Dictionary<ItemTypeEnum, UIViewItemEquip>();
    //生物数据
    public CreatureBean creatureData;

    public override void OpenUI()
    {
        base.OpenUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData)
    {
        this.creatureData = creatureData;
        gameObject.SetActive(true);
        SetCardDetails(creatureData);
        ShowEquipUI(creatureData);
        ShowEquipForCreature(creatureData);
    }

    /// <summary>
    /// 设置卡片详情
    /// </summary>
    public void SetCardDetails(CreatureBean creatureData)
    {
        ui_UIViewCreatureCardDetails.SetData(creatureData);
    }

    /// <summary>
    /// 设置装备UI
    /// </summary>
    public void ShowEquipUI(CreatureBean creatureData)
    {
        dicShowEquipView.Clear();
        var itemInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        List<ItemTypeEnum> listEquipType = new List<ItemTypeEnum>();
        if (itemInfo != null)
        {
            listEquipType = itemInfo.GetEquipItemsType();
        }
        if (listEquipType.IsNull())
        {
            ui_EquipList.gameObject.SetActive(false);
            return;
        }
        ui_EquipList.gameObject.SetActive(true);
        for (int i = 0; i < ui_EquipList.childCount; i++)
        {
            var childTF = ui_EquipList.GetChild(i);
            if (i >= listEquipType.Count)
            {
                childTF.gameObject.SetActive(false);
                continue;
            }
            var itemType = listEquipType[i];
            childTF.gameObject.SetActive(true);
            UIViewItemEquip itemEquipView = childTF.GetComponent<UIViewItemEquip>();
            itemEquipView.SetData(itemType);
            dicShowEquipView.Add(itemType, itemEquipView);
        }
    }

    /// <summary>
    /// 设置装备
    /// </summary>
    public void ShowEquipForCreature(CreatureBean creatureData)
    {
        var equipData = creatureData.dicEquipItemData;
        //先清空原来得装备栏
        foreach(var itemData in dicShowEquipView)
        {
            itemData.Value.SetData(null);
        }
        //再设置已有装备
        foreach (var itemData in equipData)
        {
            ShowEquip(itemData.Key, itemData.Value);
        }
    }

    /// <summary>
    /// 设置道具
    /// </summary>
    public void ShowEquip(ItemTypeEnum itemTypeEnum, ItemBean itemData)
    {
        if (dicShowEquipView.TryGetValue(itemTypeEnum, out UIViewItemEquip itemView))
        {
            itemView.SetData(itemData);
        }
    }

    /// <summary>
    /// 刷新装备显示
    /// </summary>
    public void RefreshShowEquip()
    {
        ShowEquipForCreature(this.creatureData);
        ui_UIViewCreatureCardDetails.RefreshCard();
    }
}