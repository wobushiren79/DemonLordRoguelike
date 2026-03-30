

using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
using UnityEngine.UI;
using static ExcelUtil;

public partial class UITestNpcCreate : BaseUIComponent
{
    public CreatureBean creatureData;
    public bool isShowEquip = true;//是否展示装备

    public List<long> listCreatureSkinData;
    public List<long> listCreatureEquipItemIds;

    public Color colorHair = Color.white;//头发颜色
    public override void Awake()
    {
        base.Awake();

        //测试标准模型
        CreatureBean creatureNormalTest = new CreatureBean(2001);
        creatureNormalTest.AddSkinForBase();

        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_NormalModel, creatureNormalTest);
        SpineHandler.Instance.PlayAnim(ui_NormalModel, SpineAnimationStateEnum.Idle, creatureNormalTest, true);

        ui_LoadInput.text = "2000000001";

        ui_UIViewColorSelect_Hair.SetData("头发颜色", colorHair, ActionForHairColorChange);
    }

    /// <summary>
    /// 初始化角色身体
    /// </summary>
    public void InitCreatureBodyData()
    {
        List<long> listSkinId = listCreatureSkinData;

        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureData.creatureInfo.model_id);
        var creatureModelData = CreatureModelCfg.GetItemData(creatureData.creatureInfo.model_id);

        //获取该生物所有的皮肤类型
        List<CreatureSkinTypeEnum> listSkinType = new List<CreatureSkinTypeEnum>();
        foreach (var item in dicAllSkins)
        {
            listSkinType.Add(item.Key);
        }

        bool isShowHairColorSelect = false;
        for (int i = 0; i < ui_UIDataBody.childCount; i++)
        {
            var itemView = ui_UIDataBody.GetChild(i).GetComponent<UIViewTestIconShow>();
            if (i < listSkinType.Count)
            {
                var skinType = listSkinType[i];
                CreatureModelInfoBean targetItemData = null;
                for (int j = 0; j < listSkinId.Count; j++)
                {
                    var itemSkinId = listSkinId[j];
                    var itemData = CreatureModelInfoCfg.GetItemData(itemSkinId);
                    if (itemData.GetPartType() == skinType)
                    {
                        targetItemData = itemData;
                        if (skinType == CreatureSkinTypeEnum.Hair && itemData.color_state != 0)
                        {
                            isShowHairColorSelect = true;
                        }
                    }
                }
                string iconRes = null;
                long targetItemId = 0;
                if (targetItemData != null)
                {
                    iconRes = $"{creatureModelData.mark_name}_Atlas_{targetItemData.res_name.Replace("/", "_")}";
                    targetItemId = targetItemData.id;
                }
                itemView.gameObject.SetActive(true);
                itemView.SetDataForSkin((int)skinType,targetItemId, iconRes, skinType.GetEnumName());
                itemView.actionForOnClick = ActionForCreatureBodyOnClick;
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        }
        //颜色选择
        ui_UIViewColorSelect_Hair.gameObject.SetActive(isShowHairColorSelect ? true : false);
    }

    /// <summary>
    /// 初始化角色状态
    /// </summary>
    public void InitCreatureEquipData()
    {
        List<ItemTypeEnum> listEquipType = creatureData.creatureInfo.GetEquipItemsType();
        List<long> listEquipItems = listCreatureEquipItemIds;

        for (int i = 0; i < ui_UIDataEquipItem.childCount; i++)
        {
            var itemView = ui_UIDataEquipItem.GetChild(i).GetComponent<UIViewTestIconShow>();
            if (i < listEquipType.Count)
            {
                var equipItemType = listEquipType[i];
                ItemsInfoBean targetItemData = null;
                for (int j = 0; j < listEquipItems.Count; j++)
                {
                    var itemDataId = listEquipItems[j];
                    var itemInfo = ItemsInfoCfg.GetItemData(itemDataId);
                    if (itemInfo.GetItemType() == equipItemType)
                    {
                        targetItemData = itemInfo;
                    }
                }
                string iconRes = null;
                long targetItemId = 0;
                if (targetItemData != null)
                {
                    iconRes = targetItemData.icon_res;
                    targetItemId = targetItemData.id;
                }
                itemView.gameObject.SetActive(true);
                itemView.SetDataForItem((int)equipItemType, targetItemId, iconRes, equipItemType.GetEnumName());
                itemView.actionForOnClick = ActionForCreatureEquipOnClick;
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 刷新展示角色
    /// </summary>
    public void RefreshCreature()
    {
        //使用自定义皮肤
        creatureData.InitSkin(listCreatureSkinData);
        //使用自定义装备
        creatureData.InitEquip(listCreatureEquipItemIds);
        //修改头发颜色
        creatureData.ChangeSkinColor(CreatureSkinTypeEnum.Hair, colorHair);

        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_TargetModel, creatureData, isNeedEquip: isShowEquip);
        //播放待机动画
        SpineHandler.Instance.PlayAnim(ui_TargetModel, SpineAnimationStateEnum.Idle, creatureData, true);
        //设置生物名字
        ui_UIDataNameInput.text = creatureData.creatureName;
        //设置UI显示数据
        ui_CreatureCardItem.SetData(creatureData, CardUseStateEnum.Show);
        ui_ViewCreatureCardDetails.SetData(creatureData);
        //设置生物装备
        InitCreatureEquipData();
        //设置生物皮肤
        InitCreatureBodyData();
    }

    #region  按钮点击
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_LoadBtn)
        {
            OnClickForLoadNpc();
        }
        else if (viewButton == ui_SaveBtn)
        {
            OnClickForSaveCreature();
        }
        else if (viewButton == ui_ListSelectBG_Button)
        {
            OnClickForHideSelect();
        }
        else if (viewButton == ui_ShowEquipBtn)
        {
            OnClickForChangeShowEquip();
        }
    }

    /// <summary>
    /// 点击切换显隐装备
    /// </summary>
    public void OnClickForChangeShowEquip()
    {
        isShowEquip = !isShowEquip;
        RefreshCreature();
    }

    /// <summary>
    /// 点击保存数据
    /// </summary>
    public void OnClickForSaveCreature()
    {
        if (creatureData == null)
        {
            LogUtil.LogError("没有生物数据");
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = $"是否要保存生物数据 npcId:{creatureData.creatureNpcData.npcId}";
        dialogData.actionSubmit = ((view, data) =>
        {
            string creatureSkinData = "";
            string creatureEquipItemIds = "";
            foreach (var item in listCreatureSkinData)
            {
                creatureSkinData += $"{item}&";
            }
            foreach (var item in listCreatureEquipItemIds)
            {
                creatureEquipItemIds += $"{item}&";
            }
#if UNITY_EDITOR
            var creatureNpcData = creatureData.GetCreatureNpcData();
            long npciD = creatureNpcData.npcId;
            List<ExcelChangeData> listData = new List<ExcelChangeData>()
            {
                new ExcelChangeData(npciD,"skin_data",creatureSkinData),
                new ExcelChangeData(npciD,"equip_item_ids",creatureEquipItemIds),
            };
            ExcelUtil.SetExcelData("Assets/Data/Excel/excel_npc_info[NPC信息].xlsx", "NpcInfo", listData);
#endif
        });
        dialogData.actionCancel = ((view, data) =>  
        {
            
        });
        UIHandler.Instance.ShowDialogNormal(dialogData);

    }

    /// <summary>
    /// 点击加载NPC
    /// </summary>
    public void OnClickForLoadNpc()
    {
        int npcId = int.Parse(ui_LoadInput.text);
        NpcInfoBean npcInfoData = NpcInfoCfg.GetItemData(npcId);
        creatureData = new CreatureBean(npcInfoData);
        var creatureNpcData = creatureData.GetCreatureNpcData();
        listCreatureSkinData = creatureNpcData.npcInfo.skin_data.SplitForListLong('&');
        listCreatureEquipItemIds = creatureNpcData.npcInfo.equip_item_ids.SplitForListLong('&');
        RefreshCreature();
    }

    /// <summary>
    /// 点击关闭选项
    /// </summary>
    public void OnClickForHideSelect()
    {
        ui_ListSelectBG_RectTransform.gameObject.SetActive(false);
        ui_ListSelectContent.DestroyAllChild();
    }
    #endregion

    #region  事件回调
    public void ActionForHairColorChange(UIViewColorShow viewColorShow, Color changeColor)
    {
        if (viewColorShow == ui_UIViewColorSelect_Hair)
        {
            colorHair = changeColor;
            RefreshCreature();
        }
    }

    public void ActionForCreatureBodyOnClick(int showType, long showId)
    {
        ui_ListSelectBG_RectTransform.gameObject.SetActive(true);
        CreatureSkinTypeEnum creatureSkinType = (CreatureSkinTypeEnum)showType;
        ui_ListSelectContent.DestroyAllChild();
        var creatureModelData = CreatureModelCfg.GetItemData(creatureData.creatureInfo.model_id);
        
        //添加一个空白的显示 用于取消不放置
        GameObject itemShowObjNull = Instantiate(ui_ListSelectContent.gameObject, ui_UIViewTestIconShow.gameObject);
        itemShowObjNull.gameObject.SetActive(true);
        var itemShowViewNull = itemShowObjNull.GetComponent<UIViewTestIconShow>();
        itemShowViewNull.SetDataForSkin(showType, 0, null, "取消");
        itemShowViewNull.actionForOnClick = ActionForSelectItemForCreatureBody;

        //加载改角色所有的皮肤类型
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureData.creatureInfo.model_id);
        if (dicAllSkins.TryGetValue(creatureSkinType, out var listSkinData))
        {
            for (int i = 0; i < listSkinData.Count; i++)
            {
                var creatureModelInfo = listSkinData[i];
                GameObject itemShowObj = Instantiate(ui_ListSelectContent.gameObject, ui_UIViewTestIconShow.gameObject);
                itemShowObj.gameObject.SetActive(true);
                var itemShowView = itemShowObj.GetComponent<UIViewTestIconShow>();
                string iconRes = null;
                if (creatureModelInfo != null)
                {
                    iconRes = $"{creatureModelData.mark_name}_Atlas_{creatureModelInfo.res_name.Replace("/", "_")}";
                }
                itemShowView.SetDataForSkin(1, creatureModelInfo.id, iconRes, creatureSkinType.GetEnumName());
                itemShowView.actionForOnClick = ActionForSelectItemForCreatureBody;
            }
        }
        
    }

    public void ActionForCreatureEquipOnClick(int showType, long showId)
    {
        ui_ListSelectBG_RectTransform.gameObject.SetActive(true);
        ItemTypeEnum itemType = (ItemTypeEnum)showType;
        ui_ListSelectContent.DestroyAllChild();

        //添加一个空白的显示 用于取消不放置
        GameObject itemShowObjNull = Instantiate(ui_ListSelectContent.gameObject, ui_UIViewTestIconShow.gameObject);
        itemShowObjNull.gameObject.SetActive(true);
        var itemShowViewNull = itemShowObjNull.GetComponent<UIViewTestIconShow>();
        itemShowViewNull.SetDataForItem(showType, 0, null, "取消");
        itemShowViewNull.actionForOnClick = ActionForSelectItemForCreatureEquip;

        var creatureModelData = CreatureModelCfg.GetItemData(creatureData.creatureInfo.model_id);
        var listItemInfo = ItemsInfoCfg.GetDataByCreatureModelId(creatureModelData.id);
        for (int i = 0; i < listItemInfo.Count; i++)
        {
            var itemInfo = listItemInfo[i];
            if (itemInfo.GetItemType() == itemType)
            {
                GameObject itemShowObj = Instantiate(ui_ListSelectContent.gameObject, ui_UIViewTestIconShow.gameObject);
                itemShowObj.gameObject.SetActive(true);
                var itemShowView = itemShowObj.GetComponent<UIViewTestIconShow>();
                itemShowView.SetDataForItem(showType, itemInfo.id, itemInfo.icon_res, itemType.GetEnumName());
                itemShowView.actionForOnClick = ActionForSelectItemForCreatureEquip;
            }
        }
    }

    /// <summary>
    /// 选择身体部件
    /// </summary>
    public void ActionForSelectItemForCreatureBody(int showType, long showId)
    {
        OnClickForHideSelect();
        var creatureModelInfo = CreatureModelInfoCfg.GetItemData(showId);
        var partType = creatureModelInfo.GetPartType();
        for (int i = 0; i < listCreatureSkinData.Count; i++)
        {
            var itemDataId = listCreatureSkinData[i];
            var itemCreatureModelInfo = CreatureModelInfoCfg.GetItemData(itemDataId);
            //如果是同一个部位的 去掉
            if (itemCreatureModelInfo.GetPartType() == partType)
            {
                listCreatureSkinData.Remove(itemDataId);
                i--;
            }
        }
        //添加该部位
        if (creatureModelInfo != null)
            listCreatureSkinData.Add(creatureModelInfo.id);
        //刷新
        RefreshCreature();
    }

    /// <summary>
    /// 选择装备
    /// </summary>
    public void ActionForSelectItemForCreatureEquip(int showType, long showId)
    {
        OnClickForHideSelect();
        var targetItemsInfo = ItemsInfoCfg.GetItemData(showId);
        var itemType = (ItemTypeEnum)showType;
        for (int i = 0; i < listCreatureEquipItemIds.Count; i++)
        {
            var itemDataId = listCreatureEquipItemIds[i];
            var itemsInfo = ItemsInfoCfg.GetItemData(itemDataId);
           //如果是同一个部位的 去掉
            if(itemsInfo.GetItemType() == itemType)
            {
                listCreatureEquipItemIds.Remove(itemDataId);
                i--;
            }
        }
        if (showId != 0)
            listCreatureEquipItemIds.Add(showId);
        RefreshCreature();
    }
    #endregion;
}


