

using System.Collections.Generic;
using System.Linq;

public partial class UIViewCreatureCardEquipDetails : BaseUIView
{
    //展示的装备UI
    protected Dictionary<ItemTypeEnum, UIViewItemEquip> dicShowEquipView = new Dictionary<ItemTypeEnum, UIViewItemEquip>();

    public void SetData(CreatureBean creatureData)
    {
        SetCardDetails(creatureData);
        SetEquipUI(creatureData);
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
    public void SetEquipUI(CreatureBean creatureData)
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
            var itemType = listEquipType[i];
            if (i >= listEquipType.Count)
            {
                childTF.gameObject.SetActive(false);
                continue;
            }
            childTF.gameObject.SetActive(true);
            UIViewItemEquip itemEquipView = childTF.GetComponent<UIViewItemEquip>();
            itemEquipView.SetData(itemType);
            dicShowEquipView.Add(itemType, itemEquipView);
        }
    }
}