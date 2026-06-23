

using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UICreatureManager : BaseUIComponent
{
    //当前选择的生物卡片下标
    public int selectCreatureIndex = 0;

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue,true);

        InitCreaturekData();
        InitBackpackItemsData();
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        this.RegisterEvent<UIViewItemBackpack>(EventsInfo.UIViewItemBackpack_OnClickSelect, EventForItemBackpackClickSelect);
        this.RegisterEvent<UIViewItemEquip>(EventsInfo.UIViewItemEquip_OnClickSelect, EventForItemEquipClickSelect);

        ui_BtnLevelUpSacrifice_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(60000), PopupEnum.Text);
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
        else if (viewButton == ui_BtnLevelUpSacrifice_Button)
        {
            OnClickForCreatureSacrifice();
        }
    }

    /// <summary>
    /// 输入响应: 按下 ESC 时退出生物管理界面
    /// </summary>
    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
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
        var listBackpackCreature = userData.GetUserBackpackCreatureData().listBackpackCreature;
        ui_UIViewCreatureCardList.SetData(listBackpackCreature, CardUseStateEnum.CreatureManager, OnCellChangeForBackpackCreature);
        //钳制选中下标:献祭等操作会使生物数量减少,上次选中的下标可能越界,夹到有效范围避免取数据越界报错
        int creatureCount = listBackpackCreature != null ? listBackpackCreature.Count : 0;
        if (selectCreatureIndex >= creatureCount)
            selectCreatureIndex = creatureCount > 0 ? creatureCount - 1 : 0;
        //初始化卡片详情(无生物时不取数据,避免越界报错)
        var itemCreatureData = creatureCount > 0 ? ui_UIViewCreatureCardList.GetItemData(selectCreatureIndex) : null;
        ui_UIViewCreatureCardEquipDetails.SetData(itemCreatureData);
        //刷新献祭升级按钮显隐(返回界面/升级后保持正确状态)
        RefreshSacrificeButton(itemCreatureData);
    }

    /// <summary>
    /// 刷新献祭升级按钮显隐: 默认隐藏,仅在已解锁祭坛 且 当前经验达到下一级所需(未满级)时显示。
    /// </summary>
    /// <param name="creatureData">当前选中的生物;为空则隐藏</param>
    public void RefreshSacrificeButton(CreatureBean creatureData)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        bool canSacrificeLevelUp = creatureData != null
            && userUnlock.CheckIsUnlock(UnlockEnum.Altar)
            && creatureData.CanUpLevel();
        ui_BtnLevelUpSacrifice_Button.gameObject.SetActive(canSacrificeLevelUp);
    }

    /// <summary>
    /// 初始化背包道具数据
    /// </summary>
    public void InitBackpackItemsData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        // 获取当前选中的生物数据
        var creatureData = ui_UIViewCreatureCardEquipDetails.creatureData;
        ui_UIViewItemBackpackList.SetData(userData.GetUserBackpackItemsData().listBackpackItems, OnCellChangeForBackpackItem, creatureData);
    }

    /// <summary>
    /// 设置选中第几张卡片
    /// </summary>
    public void SetCreatureCard(int indexSelect, CreatureBean creatureData)
    {
        this.selectCreatureIndex = indexSelect;
        ui_UIViewCreatureCardEquipDetails.SetData(creatureData);
        ui_UIViewCreatureCardList.RefreshAllCard();

        //刷新献祭升级按钮显隐
        RefreshSacrificeButton(creatureData);

        // 刷新背包道具显示（根据新选中的生物判断可装备性）
        InitBackpackItemsData();
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

        // 判断生物是否可以装备该道具
        CreatureInfoBean creatureInfo = creatureData.creatureInfo;
        if (!creatureInfo.CanEquipItem(itemInfo))
        {
            LogUtil.Log($"生物{creatureData.creatureName}无法装备该道具");
            return;
        }

        ItemTypeEnum itemType = itemInfo.GetItemType();
        //从背包里移除
        var userData = GameDataHandler.Instance.manager.GetUserData();
        userData.RemoveBackpackItem(itemData);
        //添加到角色装备里
        creatureData.ChangeEquip(itemType, itemData, out var beforeItem);
        //把换下来得装备添加到背包里
        if (beforeItem != null)
            userData.AddBackpackItem(beforeItem);

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
            userData.AddBackpackItem(beforeItem);

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

    /// <summary>
    /// 点击献祭
    /// </summary>
    public void OnClickForCreatureSacrifice()
    {
        var itemCreatureData = ui_UIViewCreatureCardList.GetItemData(selectCreatureIndex);
        if (itemCreatureData == null)
        {
            LogUtil.LogError("没有献祭生物");
            return;
        }
        CreatureSacrificeBean creatureSacrificeData = new CreatureSacrificeBean();
        creatureSacrificeData.targetCreature = itemCreatureData;
        GameHandler.Instance.StartCreatureSacrifice(creatureSacrificeData);
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