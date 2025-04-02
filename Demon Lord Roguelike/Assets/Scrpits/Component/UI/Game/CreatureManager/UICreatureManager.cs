

using System.Collections.Generic;
using UnityEngine.UI;

public partial class UICreatureManager : BaseUIComponent
{
    //当前选择的生物卡片下标
    public int selectCreatureIndex = 0;

    public override void OpenUI()
    {
        base.OpenUI();
        InitCreaturekData();
        InitBackpackItemsData();
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        this.RegisterEvent<UIViewItemBackpack>(EventsInfo.UIViewItemBackpack_OnClickSelect, EventForItemBackpackClickSelect);
        this.RegisterEvent<UIViewItemEquip>(EventsInfo.UIViewItemEquip_OnClickSelect, EventForItemEquipClickSelect);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewCreatureCardList.CloseUI();
        ui_UIViewItemBackpackList.CloseUI();
        ui_UIViewCreatureCardEquipDetails.CloseUI();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 选择指定的生物卡片
    /// </summary>
    public void SelectCreatureCard(int index)
    {

    }

    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitCreaturekData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        ui_UIViewCreatureCardList.SetData(userData.listBackpackCreature, CardUseState.CreatureManager, OnCellChangeForBackpackCreature);
        //初始化卡片详情
        var itemCreatureData = ui_UIViewCreatureCardList.GetItemData(selectCreatureIndex);
        ui_UIViewCreatureCardEquipDetails.SetData(itemCreatureData);

    }

    /// <summary>
    /// 初始化背包道具数据
    /// </summary>
    public void InitBackpackItemsData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        ui_UIViewItemBackpackList.SetData(userData.listBackpackItems, OnCellChangeForBackpackItem);
    }

    /// <summary>
    /// 设置选中第几张卡片
    /// </summary>
    public void SetCreatureCard(int indexSelect, CreatureBean creatureData)
    {
        this.selectCreatureIndex = indexSelect;
        ui_UIViewCreatureCardEquipDetails.SetData(creatureData);
        ui_UIViewCreatureCardList.RefreshAllCard();
    }

    /// <summary>
    /// 设置装备
    /// </summary>
    public void SetCreatureEquip(ItemBean itemData)
    {
        if (itemData == null || itemData.itemId <= 0)
        {
            return;
        }
        var itemInfo = ItemsInfoCfg.GetItemData(itemData.itemId);
        if (itemInfo == null)
        {
            LogUtil.LogError($"设置角色装备失败 没有找到itemId_{itemData.itemId}的数据");
            return;
        }
        CreatureBean creatureData = ui_UIViewCreatureCardEquipDetails.creatureData;
        if (creatureData == null)
        {
            LogUtil.LogError($"设置角色装备失败 没有生物数据");
            return;
        }
        ItemTypeEnum itemType = itemInfo.GetItemType();
        //从背包里移除
        var userData = GameDataHandler.Instance.manager.GetUserData();
        userData.RemoveItem(itemData);
        //添加到角色装备里
        creatureData.ChangeEquip(itemType, itemData, out var beforeItem);
        //把换下来得装备添加到背包里
        if (beforeItem != null)
            userData.AddItem(beforeItem);

        //刷新相关UI
        ui_UIViewCreatureCardEquipDetails.RefreshShowEquip();
        InitBackpackItemsData();
    }

    /// <summary>
    /// 卸载角色装备
    /// </summary>
    public void UnloadCreatureEquip(ItemTypeEnum itemType)
    {
        CreatureBean creatureData = ui_UIViewCreatureCardEquipDetails.creatureData;
        if (creatureData == null)
        {
            LogUtil.LogError($"卸载角色装备失败 没有生物数据");
            return;
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //卸载装备
        creatureData.ChangeEquip(itemType, null, out var beforeItem);
        //把换下来得装备添加到背包里
        if (beforeItem != null)
            userData.AddItem(beforeItem);

        //刷新相关UI
        ui_UIViewCreatureCardEquipDetails.RefreshShowEquip();
        InitBackpackItemsData();
    }

    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreature(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (index == selectCreatureIndex)
        {
            itemView.SetCardState(CardStateEnum.CreatureManagerSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureManagerNoSelect);
        }
    }

    /// <summary>
    /// 背包道具变化
    /// </summary>
    public void OnCellChangeForBackpackItem(int index, UIViewItemBackpack itemView, ItemBean itemData)
    {

    }

    #region 点击事件
    /// <summary>
    /// 点击返回
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }
    #endregion

    #region  回调事件
    /// <summary>
    /// 卡片点击
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem targetView)
    {
        SetCreatureCard(targetView.cardData.indexList, targetView.cardData.creatureData);
    }

    /// <summary>
    /// 背包道具点击
    /// </summary>
    public void EventForItemBackpackClickSelect(UIViewItemBackpack targetView)
    {
        SetCreatureEquip(targetView.itemData);
    }

    /// <summary>
    /// 装备道具点击
    /// </summary>
    public void EventForItemEquipClickSelect(UIViewItemEquip targetView)
    {
        UnloadCreatureEquip(targetView.itemTypeEnum);
    }
    #endregion
}