using DG.Tweening;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent, IRadioGroupCallBack
{
    // �����������ݿ�Ƭ
    public Queue<UIViewCreatureCardItem> queuePoolCardLineup = new Queue<UIViewCreatureCardItem>();
    // չʾ�е����ݿ�Ƭ
    public List<UIViewCreatureCardItem> listShowCardLineup = new List<UIViewCreatureCardItem>();


    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();
    //��ǰ���ݵ����
    public int currentLineupIndex = 1;
    //���ݶ�����Ƭ�ƶ�ʱ��
    protected float timeForLineupCardMove = 0.2f;
    //���ݶ�����Ƭ�ƶ�ʱ��(��ʼ��)
    protected float timeForLineupCardMoveInit = 0.4f;
    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForBackpack);
        ui_LineupIndexTitle.SetCallBack(this);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        currentLineupIndex = 1;
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);
        InitBackpackData();
        InitLineupData();
        //��ʼ�����ñ���
        ui_LineupIndexTitle.SetPosition(0,false);
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
    /// item�����仯
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForBackpack(ScrollGridCell itemCell)
    {
        var itemData = listBackpackCreature[itemCell.index];
        var itemView = itemCell.GetComponent<UIViewCreatureCardItem>();
        itemView.cardData.indexList = itemCell.index;
        itemView.SetData(itemData, CardUseState.LineupBackpack);

        //����ѡ�к�δѡ��״̬
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
    /// ��ʼ��������Ƭ����
    /// </summary>
    public void InitBackpackData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        listBackpackCreature.Clear();
        listBackpackCreature.AddRange(userData.listBackpackCreature);
        //��ʼ������
        OrderBackpackCreature(3, false);
        //��������
        ui_BackpackContent.SetCellCount(userData.listBackpackCreature.Count);
    }

    /// <summary>
    /// ��ʼ����������
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
    /// ��ȡ���ݿ�Ƭλ��
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
    /// ��������lineup��Ƭ����λ�ö���
    /// </summary>
    /// <param name="animType">0Ĭ�� 1��ʼ��</param>
    /// <param name="actionForComplete"></param>
    public void AnimForAllLineupCardPosReset(int animType, Action actionForComplete)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        int completeNum = 0;
        //֪ͨ��Ƭ�ƶ�λ��
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            var itemCardView = listShowCardLineup[i];
            itemCardView.transform.SetAsLastSibling();
            var itemLineupPosIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemCardView.cardData.creatureData.creatureId);
            Vector3 itemLineupPos = GetLineupPostion(listShowCardLineup.Count, itemLineupPosIndex);
            //���Ŷ���
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
    /// ������������Ŀ�Ƭ
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
        //���Ŷ���
        AnimForAllLineupCardPosReset(animType, () =>
        {
            //UIHandler.Instance.HideScreenLock();
        });
    }

    /// <summary>
    /// �Ƴ�������Ŀ�Ƭ
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

    /// <summary>
    /// �Ƴ�����չʾ�����ݿ�Ƭ
    /// </summary>
    public void RemoveLineupCardShow()
    {
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            var targetView = listShowCardLineup[i];
            targetView.gameObject.SetActive(false);
            queuePoolCardLineup.Enqueue(targetView);
        }
        listShowCardLineup.Clear();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_OrderBtn_Rarity)
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

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// �ı�������
    /// </summary>
    public void ChangeLineupIndex(int indexLineup)
    {
        currentLineupIndex = indexLineup;
        //�Ƴ�����չʾ�е����ݿ�Ƭ
        RemoveLineupCardShow();
        //��ʼ�����ݿ�Ƭ
        InitLineupData();
        //ˢ�±�����Ƭ
        ui_BackpackContent.RefreshAllCells();
    }

    /// <summary>
    /// ���򱳰��������
    /// </summary>
    /// <param name="orderType"></param>
    public void OrderBackpackCreature(int orderType, bool isRefreshUI = true)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();

        switch (orderType)
        {
            case 1://��ϡ�ж�����
                listBackpackCreature = listBackpackCreature
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
                //���ȼ�����
                listBackpackCreature = listBackpackCreature
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
                //��ѡ������
                listBackpackCreature = listBackpackCreature
                    .OrderBy((itemData) =>
                    {
                        if (userData.CheckIsLineup(currentLineupIndex, itemData.creatureId))
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    })
                    .ThenByDescending((itemData) =>
                    {
                        return itemData.rarity;
                    })
                    .ThenByDescending((itemData) =>
                    {
                        return itemData.level;
                    })
                    .ToList();
                break;
        }
        if (isRefreshUI)
            ui_BackpackContent.RefreshAllCells();
    }

    /// <summary>
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// �¼�-����ѡ�п�Ƭ
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {
        ui_ViewCreatureCardDetails.gameObject.SetActive(true);
        ui_ViewCreatureCardDetails.SetData(targetView.cardData.creatureData);
    }

    /// <summary>
    /// �¼�-�����뿪
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {
        //ui_ViewCreatureCardDetails.gameObject.SetActive(false);
    }

    /// <summary>
    /// �¼�-���
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (targetView.cardData.cardUseState == CardUseState.LineupBackpack && targetView.cardData.cardState == CardStateEnum.LineupNoSelect)
        {
            userData.AddLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureId);
            ui_BackpackContent.RefreshCell(targetView.cardData.indexList);

            //�������ݿ�
            Vector3 posStart = UGUIUtil.GetUIRootPos(ui_LineupContent.transform, targetView.transform);
            AddLineupCard(targetView.cardData.creatureData, posStart);
        }
        else if (targetView.cardData.cardUseState == CardUseState.Lineup)
        {
            userData.RemoveLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureId);

            //ˢ��
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
            //�Ƴ����ݿ�
            RemoveLineupCard(targetView);
        }
    }

    public void RadioButtonSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {
        if (rbview == ui_LineupIndexBtn_1)
        {
            ChangeLineupIndex(1);
        }
        else if (rbview == ui_LineupIndexBtn_2)
        {
            ChangeLineupIndex(2);
        }
        else if (rbview == ui_LineupIndexBtn_3)
        {
            ChangeLineupIndex(3);
        }
    }

    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }
}
