

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public partial class UIViewCreatureCardList : BaseUIView
{
    //生物数据
    protected List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //卡片的使用地方
    protected CardUseState cardUseState;
    //卡片变化回调
    protected Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange;
    public override void Awake()
    {
        base.Awake();
        ui_CreatureListContent.AddCellListener(OnCellChangeForCreatrue);
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
        if (viewButton == ui_OrderBtn_Rarity)
        {
            OrderBackpackCreature(1);
        }
        else if (viewButton == ui_OrderBtn_Level)
        {
            OrderBackpackCreature(2);
        }
        else if (viewButton == ui_OrderBtn_Lineup)
        {
            OrderBackpackCreature(3);
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
    public void RefreshCardByCreatureId(string creatureId)
    {
        listCreatureData.ForEach((index, itemData) =>
        {
            if (creatureId.Equals(itemData.creatureId))
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
    public void SetData(List<CreatureBean> listData, CardUseState cardUseState, Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange = null)
    {
        OpenUI();
        this.cardUseState = cardUseState;
        this.actionForOnCellChange = actionForOnCellChange;
        listCreatureData.Clear();
        listCreatureData.AddRange(listData);
        //初始化排序
        OrderBackpackCreature(1, false);
        //设置数量
        ui_CreatureListContent.SetCellCount(listCreatureData.Count);
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
    public void OrderBackpackCreature(int orderType, bool isRefreshUI = true)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();

        switch (orderType)
        {
            case 1://按稀有度排序
                listCreatureData = listCreatureData
                    .OrderByDescending((itemData) =>
                    {
                        return itemData.rarity;
                    })
                    .ThenByDescending((itemData) =>
                    {
                        return itemData.level;
                    })
                    .ToList();
                break;
            case 2:
                //按等级排序
                listCreatureData = listCreatureData
                    .OrderByDescending((itemData) =>
                    {
                        return itemData.level;
                    })
                    .ThenByDescending((itemData) =>
                    {
                        return itemData.rarity;
                    })
                    .ToList();
                break;
            case 3:
                // //按选中排序
                // listCreatureData = listCreatureData
                //     .OrderBy((itemData) =>
                //     {
                //         if (userData.CheckIsLineup(currentLineupIndex, itemData.creatureId))
                //         {
                //             return 0;
                //         }
                //         else
                //         {
                //             return 1;
                //         }
                //         return 1;
                //     })
                //     .ThenByDescending((itemData) =>
                //     {
                //         return itemData.rarity;
                //     })
                //     .ThenByDescending((itemData) =>
                //     {
                //         return itemData.level;
                //     })
                //     .ToList();
                break;
        }
        if (isRefreshUI)
            ui_CreatureListContent.RefreshAllCells();
    }
}