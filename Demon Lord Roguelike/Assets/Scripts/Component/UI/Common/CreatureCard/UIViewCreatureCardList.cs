

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public partial class UIViewCreatureCardList : BaseUIView
{
    //生物数据
    protected List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //卡片的使用地方
    protected CardUseStateEnum cardUseState;
    //卡片变化回调
    protected Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange;
    public override void Awake()
    {
        base.Awake();
        ui_CreatureListContent.AddCellListener(OnCellChangeForCreatrue);
        ui_OrderBtn_Rarity_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000004),PopupEnum.Text);
        ui_OrderBtn_Level_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000005),PopupEnum.Text);
        ui_OrderBtn_Lineup_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000006),PopupEnum.Text);
        ui_OrderBtn_Name_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000007),PopupEnum.Text);
    }

    public override void OpenUI()
    {
        base.OpenUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_CreatureListContent.SetCellCount(0);
        ui_CreatureListContent.ClearAllCell();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_OrderBtn_Rarity_Button)
        {
            OrderListCreature(1);
        }
        else if (viewButton == ui_OrderBtn_Level_Button)
        {
            OrderListCreature(2);
        }
        else if (viewButton == ui_OrderBtn_Lineup_Button)
        {
            OrderListCreature(3);
        }
        else if (viewButton == ui_OrderBtn_Name_Button)
        {
            OrderListCreature(4);
        }
    }

    /// <summary>
    /// 刷新指定卡片
    /// </summary>
    public void RefreshCardByIndex(int index)
    {
        ui_CreatureListContent.RefreshCell(index);
    }

    /// <summary>
    /// 刷新指定卡片
    /// </summary>
    public void RefreshCardByCreatureUUId(string creatureUUId)
    {
        listCreatureData.ForEach((index, itemData) =>
        {
            if (creatureUUId.Equals(itemData.creatureUUId))
            {
                RefreshCardByIndex(index);
            }
        });
    }

    /// <summary>
    /// 刷新所有卡片
    /// </summary>
    public void RefreshAllCard()
    {
        ui_CreatureListContent.RefreshAllCells();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(List<CreatureBean> listData, CardUseStateEnum cardUseState, Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange = null)
    {
        gameObject.SetActive(true);
        this.cardUseState = cardUseState;
        this.actionForOnCellChange = actionForOnCellChange;
        listCreatureData.Clear();
        listCreatureData.AddRange(listData);
        //初始化排序
        OrderListCreature(1, false);
        //设置数量
        ui_CreatureListContent.SetCellCount(listCreatureData.Count);
    }

    /// <summary>
    /// 获取列表单个数据
    /// </summary>
    public CreatureBean GetItemData(int index)
    {
        if (index >= listCreatureData.Count)
        {
            LogUtil.LogError($"获取单个生物数据失败 超过下标 index_{index} Count_{listCreatureData.Count}");
            return null;
        }
        return listCreatureData[index];
    }

    /// <summary>
    /// item滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForCreatrue(ScrollGridCell itemCell)
    {
        var itemData = listCreatureData[itemCell.index];
        var itemView = itemCell.GetComponent<UIViewCreatureCardItem>();
        itemView.cardData.indexList = itemCell.index;
        itemView.SetData(itemData, cardUseState);
        actionForOnCellChange?.Invoke(itemCell.index, itemView, itemData);
    }

    /// <summary>
    /// 排序背包里的生物
    /// </summary>
    /// <param name="orderType"></param>
    public void OrderListCreature(int orderType, bool isRefreshUI = true)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        // 统一处理阵容索引排序逻辑
        Func<CreatureBean, int> lineupOrder = itemData =>
        {
            int lineupIndex = userData.GetLinupIndex(itemData.creatureUUId);
            return lineupIndex > 0 ? lineupIndex : int.MaxValue;
        };

        switch (orderType)
        {
            case 1://按稀有度排序
                listCreatureData = listCreatureData
                    .OrderByDescending((itemData) => itemData.rarity)
                    .ThenByDescending((itemData) => itemData.level)
                    .ThenBy((itemData) => lineupOrder)
                    .ThenBy((itemData) => itemData.creatureName)
                    .ToList();
                break;
            case 2:
                //按等级排序
                listCreatureData = listCreatureData
                    .OrderByDescending((itemData) => itemData.level)
                    .ThenByDescending((itemData) => itemData.rarity)
                    .ThenBy((itemData) => lineupOrder)
                    .ThenBy((itemData) => itemData.creatureName)
                    .ToList();
                break;
            case 3:
                //按选中排序
                listCreatureData = listCreatureData
                    .OrderBy((itemData) => lineupOrder)
                    .ThenByDescending((itemData) => itemData.rarity)
                    .ThenByDescending((itemData) => itemData.level)
                    .ThenBy((itemData) => itemData.creatureName)
                    .ToList();
                break;
            case 4://名字排序
                listCreatureData = listCreatureData
                    .OrderBy((itemData) => itemData.creatureName)
                    .ThenByDescending((itemData) => itemData.rarity)
                    .ThenBy((itemData) => lineupOrder)
                    .ThenByDescending((itemData) => itemData.level)
                    .ToList();
                break;
        }
        if (isRefreshUI)
            RefreshAllCard();
    }
}