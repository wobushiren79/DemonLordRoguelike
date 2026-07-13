

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UICreatureManager : BaseUIComponent
{
    //当前选择的生物卡片下标
    public int selectCreatureIndex = 0;

    //献祭升级按钮不可升级时的压暗染色:图标是纯白剪影(颜色来自特效材质渐变),灰度化对纯白无效,只能靠压暗;白图标×深灰=平整深灰,明显
    private readonly Color colorSacrificeButtonGray = new Color(0.35f, 0.35f, 0.35f, 1f);

    //记录按钮各 Image 置灰前的原始材质(特效材质),恢复时还原回去而非滞空
    private readonly Dictionary<Image, Material> dicSacrificeOriginMat = new Dictionary<Image, Material>();

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
        var listBackpackCreature = GetSortedBackpackCreature(userData);
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
    /// 获取阵容管理列表的默认排序副本:魔王(selfCreature)恒置顶第一位 → 其余按稀有度降序(高→低) → 等级降序(高→低) → 同类聚合(creatureId 升序,同一种魔物相邻,与筛选排序 Class 键口径一致)。
    /// <para>魔王独立于背包存储,排序后插入到列表首部;返回新列表,不改动底层存档的 listBackpackCreature 原始顺序;该顺序会作为筛选排序弹窗的稳定基序(次级 tiebreaker)。</para>
    /// </summary>
    /// <param name="userData">用户数据</param>
    /// <returns>按默认规则排序后的生物列表副本(魔王在首位)</returns>
    private List<CreatureBean> GetSortedBackpackCreature(UserDataBean userData)
    {
        var listSource = userData.GetUserBackpackCreatureData().listBackpackCreature;
        var listSorted = new List<CreatureBean>(listSource);
        listSorted.Sort((a, b) =>
        {
            //稀有度降序(高稀有度置前)
            int rarityCompare = b.GetRarityValue().CompareTo(a.GetRarityValue());
            if (rarityCompare != 0) return rarityCompare;
            //等级降序(高等级置前)
            int levelCompare = b.level.CompareTo(a.level);
            if (levelCompare != 0) return levelCompare;
            //同类聚合:按 creatureId 升序使同一种魔物相邻
            return a.creatureId.CompareTo(b.creatureId);
        });
        //魔王恒置顶显示在第一位(始终第一,不受筛选排序影响见 UIViewCreatureCardList.OrderListCreature)
        if (userData.selfCreature != null)
            listSorted.Insert(0, userData.selfCreature);
        return listSorted;
    }

    /// <summary>
    /// 刷新献祭升级按钮: 解锁祭坛且未满级时显示; 经验达标则正常, 经验未达标则置灰但仍可点击(点击时提示经验未达标)。未解锁祭坛/无选中生物/已满级时隐藏。
    /// </summary>
    /// <param name="creatureData">当前选中的生物;为空则隐藏</param>
    public void RefreshSacrificeButton(CreatureBean creatureData)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //解锁祭坛且有选中生物且未满级才显示按钮(满级隐藏,不再置灰);魔王隐藏等级且不吃经验,不可献祭升级故隐藏按钮
        bool isShow = creatureData != null
            && !creatureData.IsDemonLord()
            && userUnlock.CheckIsUnlock(UnlockEnum.Altar)
            && !creatureData.IsMaxLevel();
        ui_BtnLevelUpSacrifice_Button.gameObject.SetActive(isShow);
        if (!isShow)
            return;
        //未满级下:经验达标则正常,经验未达标则置灰(按钮始终可点击,点击时再提示)
        SetSacrificeButtonGray(!creatureData.CanUpLevel());
    }

    /// <summary>
    /// 设置献祭升级按钮的置灰表现: 图标是纯白剪影且颜色来自特效材质,故置灰时给 Image 移除特效材质(走默认UI着色,去掉渐变/流光)并压暗染深灰;恢复时还原原始特效材质并转白。非 Image 仅染色。不改变可点击性。
    /// </summary>
    /// <param name="isGray">true 置灰, false 恢复正常</param>
    private void SetSacrificeButtonGray(bool isGray)
    {
        var graphics = ui_BtnLevelUpSacrifice_Button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var graphic = graphics[i];
            if (graphic == null)
                continue;
            //Image:置灰移除特效材质(纯白剪影去掉渐变才能被压暗成灰),恢复还原原始特效材质
            if (graphic is Image image)
            {
                if (isGray)
                {
                    //首次置灰前记录原始材质(特效材质)
                    if (!dicSacrificeOriginMat.ContainsKey(image))
                        dicSacrificeOriginMat[image] = image.material;
                    image.material = null;
                }
                else if (dicSacrificeOriginMat.TryGetValue(image, out var originMat))
                {
                    //还原原始特效材质,未记录过说明从未置灰则保持不动
                    image.material = originMat;
                }
            }
            //统一压暗染色:置灰深灰(白图标×深灰=平整深灰)/恢复白色
            graphic.color = isGray ? colorSacrificeButtonGray : Color.white;
        }
    }

    /// <summary>
    /// 初始化背包道具数据
    /// </summary>
    public void InitBackpackItemsData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        // 获取当前选中的生物数据
        var creatureData = ui_UIViewCreatureCardEquipDetails.creatureData;
        var listBackpackItem = GetSortedBackpackItem(userData);
        ui_UIViewItemBackpackList.SetData(listBackpackItem, OnCellChangeForBackpackItem, creatureData);
    }

    /// <summary>
    /// 获取背包道具列表的默认排序副本:稀有度降序(高→低) → 同类聚合(道具类型 ItemTypeEnum 升序,同一类道具相邻)。
    /// <para>返回新列表,不改动底层存档的 listBackpackItems 原始顺序。</para>
    /// </summary>
    /// <param name="userData">用户数据</param>
    /// <returns>按默认规则排序后的道具列表副本</returns>
    private List<ItemBean> GetSortedBackpackItem(UserDataBean userData)
    {
        var listSource = userData.GetUserBackpackItemsData().listBackpackItems;
        var listSorted = new List<ItemBean>(listSource);
        listSorted.Sort((a, b) =>
        {
            //稀有度降序(高稀有度置前)
            int rarityCompare = b.rarity.CompareTo(a.rarity);
            if (rarityCompare != 0) return rarityCompare;
            //同类聚合:按道具类型升序使同一类道具相邻
            return ((int)a.GetItemType()).CompareTo((int)b.GetItemType());
        });
        return listSorted;
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
        //魔王不可献祭升级(隐藏等级且不吃经验),按钮已隐藏,此处再兜底拦截
        if (itemCreatureData.IsDemonLord())
            return;
        //经验未达标(或已满级)不能升级: 提示并拦截,不进入献祭流程
        if (!itemCreatureData.CanUpLevel())
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61009), 0);
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