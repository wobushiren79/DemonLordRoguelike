using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent
{
    // 缓存池里的阵容卡片
    public Queue<UIViewCreatureCardItem> queuePoolCardLineup = new Queue<UIViewCreatureCardItem>();
    // 展示中的阵容卡片
    public List<UIViewCreatureCardItem> listShowCardLineup = new List<UIViewCreatureCardItem>();


    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();
    //当前阵容的序号
    public int currentLineupIndex = 1;
    //阵容动画卡片移动时间
    protected float timeForLineupCardMove = 0.2f;
    //阵容动画卡片移动时间(初始化)
    protected float timeForLineupCardMoveInit = 0.5f;
    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForBackpack);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);
        InitBackpackData();
        InitLineupData();
    }


    public override void CloseUI()
    {
        ui_BackpackContent.SetCellCount(0);
        ui_BackpackContent.ClearAllCell();
        while (queuePoolCardLineup.Count > 0)
        {
            var targetView = queuePoolCardLineup.Dequeue();
            DestroyImmediate(targetView.gameObject);
        }
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            var targetView = listShowCardLineup[i];
            DestroyImmediate(targetView.gameObject);
        }
        listShowCardLineup.Clear();
        base.CloseUI();
    }


    /// <summary>
    /// item滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForBackpack(ScrollGridCell itemCell)
    {
        var itemData = listBackpackCreature[itemCell.index];
        var itemView = itemCell.GetComponent<UIViewCreatureCardItem>();
        itemView.cardData.indexList = itemCell.index;
        itemView.SetData(itemData, CardUseState.LineupBackpack);

        //设置选中和未选中状态
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData.CheckIsLineup(currentLineupIndex, itemData.creatureId))
        {
            itemView.SetCardState(CardStateEnum.LineupSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.LineupNoSelect);
        }
    }

    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitBackpackData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        listBackpackCreature.Clear();
        listBackpackCreature.AddRange(userData.listBackpackCreature);
        //设置数量
        ui_BackpackContent.SetCellCount(userData.listBackpackCreature.Count);
    }

    /// <summary>
    /// 初始化阵容内容
    /// </summary>
    public void InitLineupData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        List<string> listLineupCreature = userData.GetLineupCreature(currentLineupIndex);
        for (int i = 0; i < listLineupCreature.Count; i++)
        {
            string creatureId = listLineupCreature[i];
            var creatureData = userData.GetBackpackCreature(creatureId);
            if (creatureData == null)
                continue;
            AddLineupCard(creatureData, new Vector3(Screen.width / 2f + 120, 0, 0), 1);
        }
    }

    /// <summary>
    /// 获取阵容卡片位置
    /// </summary>
    public Vector3 GetLineupPostion(int maxLineupNum, int lineupPosIndex)
    {
        float wView = ui_LineupContent.rect.width;
        float itemW = wView / maxLineupNum;
        if (itemW > ui_ViewCreatureCardItem.rectTransform.rect.width)
        {
            itemW = ui_ViewCreatureCardItem.rectTransform.rect.width;
        }
        return new Vector3(itemW * lineupPosIndex - wView / 2f + itemW / 2f, 0, 0);
    }

    /// <summary>
    /// 播放所有lineup卡片重置位置动画
    /// </summary>
    /// <param name="animType">0默认 1初始化</param>
    /// <param name="actionForComplete"></param>
    public void AnimForAllLineupCardPosReset(int animType, Action actionForComplete)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        int completeNum = 0;
        //通知卡片移动位置
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            var itemCardView = listShowCardLineup[i];
            itemCardView.transform.SetAsLastSibling();
            var itemLineupPosIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemCardView.cardData.creatureData.creatureId);
            Vector3 itemLineupPos = GetLineupPostion(listShowCardLineup.Count, itemLineupPosIndex);
            //播放动画
            float timeForMove = timeForLineupCardMove;
            Ease animEase = Ease.OutBack;
            if (animType == 1)
            {
                timeForMove = timeForLineupCardMoveInit;
                animEase = Ease.OutQuad;
            }
            itemCardView.transform.DOKill();
            itemCardView.transform
                .DOLocalMove(itemLineupPos, timeForMove)
                .SetEase(animEase)
                .OnComplete(() =>
                {
                    completeNum++;
                    if (completeNum == listShowCardLineup.Count)
                    {
                        actionForComplete?.Invoke();
                    }
                });
        }
    }

    /// <summary>
    /// 增加阵容里面的卡片
    /// </summary>
    public void AddLineupCard(CreatureBean creatureData, Vector3 startPos, int animType = 0)
    {
        //UIHandler.Instance.ShowScreenLock();
        UIViewCreatureCardItem lineupView;
        if (queuePoolCardLineup.Count > 0)
        {
            lineupView = queuePoolCardLineup.Dequeue();
        }
        else
        {
            GameObject ObjNewCard = Instantiate(ui_LineupContent.gameObject, ui_ViewCreatureCardItem.gameObject);
            lineupView = ObjNewCard.GetComponent<UIViewCreatureCardItem>();
        }
        lineupView.SetData(creatureData, CardUseState.Lineup);
        lineupView.transform.localPosition = startPos;
        listShowCardLineup.Add(lineupView);
        lineupView.gameObject.SetActive(true);
        //播放动画
        AnimForAllLineupCardPosReset(animType, () =>
        {
            //UIHandler.Instance.HideScreenLock();
        });
    }

    /// <summary>
    /// 移除阵容里的卡片
    /// </summary>
    /// <param name="targetView"></param>
    public void RemoveLineupCard(UIViewCreatureCardItem targetView, int animType = 0)
    {
        //UIHandler.Instance.ShowScreenLock();
        targetView.gameObject.SetActive(false);
        queuePoolCardLineup.Enqueue(targetView);
        listShowCardLineup.Remove(targetView);

        AnimForAllLineupCardPosReset(animType, () =>
        {
            //UIHandler.Instance.HideScreenLock();
        });
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 事件-焦点选中卡片
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {
        ui_ViewCreatureCardDetails.gameObject.SetActive(true);
        ui_ViewCreatureCardDetails.SetData(targetView.cardData.creatureData);
    }

    /// <summary>
    /// 事件-焦点离开
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {
        //ui_ViewCreatureCardDetails.gameObject.SetActive(false);
    }

    /// <summary>
    /// 事件-点击
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (targetView.cardData.cardUseState == CardUseState.LineupBackpack && targetView.cardData.cardState == CardStateEnum.LineupNoSelect)
        {
            userData.AddLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureId);
            ui_BackpackContent.RefreshCell(targetView.cardData.indexList);

            //增加阵容卡
            Vector3 posStart = UGUIUtil.GetUIRootPos(ui_LineupContent.transform, targetView.transform);
            AddLineupCard(targetView.cardData.creatureData, posStart);
        }
        else if (targetView.cardData.cardUseState == CardUseState.Lineup)
        {
            userData.RemoveLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureId);

            //刷新
            var allCell = ui_BackpackContent.GetAllCell();
            for (int i = 0; i < allCell.Count; i++)
            {
                var itemCell = allCell[i];
                var itemCardView = itemCell.GetComponent<UIViewCreatureCardItem>();
                if (itemCardView.cardData.creatureData.creatureId.Equals(targetView.cardData.creatureData.creatureId))
                {
                    ui_BackpackContent.RefreshCell(i);
                    break;
                }
            }
            //移除阵容卡
            RemoveLineupCard(targetView);
        }
    }
}
