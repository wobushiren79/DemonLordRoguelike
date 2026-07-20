using DG.Tweening;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent, IRadioGroupCallBack
{
    //阵容预制体
    public UIViewCreatureCardItem modelLineupItem;
    //阵容标签
    public List<RadioButtonView> listLineupTag = new List<RadioButtonView>();

    // 缓存池里的阵容卡片
    public Queue<UIViewCreatureCardItem> queuePoolCardLineup = new Queue<UIViewCreatureCardItem>();
    // 展示中的阵容卡片
    public List<UIViewCreatureCardItem> listShowCardLineup = new List<UIViewCreatureCardItem>();

    //阵容动画卡片移动时间
    protected float timeForLineupCardMove = 0.3f;
    //阵容动画卡片移动时间(初始化)
    protected float timeForLineupCardMoveInit = 0.5f;
    //卡片动画错开间隔
    protected float timeForLineupCardStagger = 0.04f;
    //当前阵容的序号
    protected int currentLineupIndex = 1;
    public override void Awake()
    {
        base.Awake();
        ui_LineupIndexTitle.SetCallBack(this);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_LineupIndexBtn.gameObject.SetActive(false);

        //默认打开第一套阵容
        currentLineupIndex = 1;
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnBeginDrag, EventForCardBeginDrag);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnEndDrag, EventForCardEndDrag);
        InitLineupTag();
        InitCreatureData();
        InitLineupData();
        //初始化设置标题
        ui_LineupIndexTitle.SetPosition(0, false);
        //刷新UI数据
        RefreshUIData();
    }

    public override void CloseUI()
    {
        ui_UIViewCreatureCardList.CloseUI();
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
        //清除所有title
        ui_LineupIndexTitle.DestroyAllChild();
        //保存一下用户数据
        GameDataHandler.Instance.manager.SaveUserData();
        base.CloseUI();
    }

    #region  初始化
    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitCreatureData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var listBackpackCreature = GetSortedBackpackCreature(userData);
        ui_UIViewCreatureCardList.SetData(listBackpackCreature, CardUseStateEnum.LineupBackpack, OnCellChangeForBackpackCreature);
    }

    /// <summary>
    /// 获取阵容管理背包列表的默认排序副本:当前阵容内优先(按当前阵容槽位序号升序,阵容外排后) → 稀有度降序(高→低) → 等级降序(高→低)。
    /// <para>返回新列表,不改动底层存档的 listBackpackCreature 原始顺序;该顺序会作为筛选排序弹窗的稳定基序(次级 tiebreaker)。</para>
    /// </summary>
    /// <param name="userData">用户数据</param>
    /// <returns>按默认规则排序后的生物列表副本</returns>
    private List<CreatureBean> GetSortedBackpackCreature(UserDataBean userData)
    {
        var listSource = userData.GetUserBackpackCreatureData().listBackpackCreature;
        var listSorted = new List<CreatureBean>(listSource);
        //当前阵容槽位序号:在当前阵容内取其槽位序号(>=0),否则置最大值排到阵容外
        int LineupOrder(CreatureBean itemData)
        {
            int posIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemData.creatureUUId);
            return posIndex >= 0 ? posIndex : int.MaxValue;
        }
        listSorted.Sort((a, b) =>
        {
            //当前阵容内优先:按槽位序号升序(阵容外统一为最大值排后)
            int lineupCompare = LineupOrder(a).CompareTo(LineupOrder(b));
            if (lineupCompare != 0) return lineupCompare;
            //稀有度降序(高稀有度置前)
            int rarityCompare = b.GetRarityValue().CompareTo(a.GetRarityValue());
            if (rarityCompare != 0) return rarityCompare;
            //等级降序(高等级置前)
            return b.level.CompareTo(a.level);
        });
        return listSorted;
    }

    /// <summary>
    /// 初始化阵容内容
    /// </summary>
    public void InitLineupData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        List<CreatureBean> listLineupCreature = userData.GetLineupCreature(currentLineupIndex);
        RectTransform rtfLineupIndexTitle = (RectTransform)ui_LineupIndexTitle.transform;
        for (int i = 0; i < listLineupCreature.Count; i++)
        {
            var creatureData = listLineupCreature[i];
            AddLineupCard(creatureData, new Vector3(rtfLineupIndexTitle.sizeDelta.x / 2f + 120, 0, 0), 1);
        }
    }

    /// <summary>
    /// 初始化阵容标签
    /// </summary>
    public void InitLineupTag()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //获取阵容解锁数量
        int unlockLineupNum = userUnlock.GetUnlockLineupNum();
        //清除所有title
        ui_LineupIndexTitle.DestroyAllChild();
        listLineupTag.Clear();
        ui_LineupIndexTitle.listButton.Clear();
        for (int i = 0; i < unlockLineupNum; i++)
        {
            GameObject objItemTitle = Instantiate(ui_LineupIndexTitle.gameObject, ui_LineupIndexBtn.gameObject);
            RadioButtonView radioButton = objItemTitle.GetComponent<RadioButtonView>();
            SetLineupTagText(radioButton, i + 1);
            listLineupTag.Add(radioButton);
            ui_LineupIndexTitle.AddRadioButton(radioButton);
        }
    }

    /// <summary>
    /// 设置阵容选择标签文本
    /// </summary>
    public void SetLineupTagText(RadioButtonView radioButtonView, int index)
    {
        var titleTex = radioButtonView.GetComponentInChildren<TextMeshProUGUI>();
        titleTex.text = string.Format(TextHandler.Instance.GetTextById(30005), index);
    }
    #endregion

    #region  其他
    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        int creatureNumMax = userUnlock.GetUnlockLineupCreatureNum();

        var listCreatureIds = userData.GetLineupCreatureIds(currentLineupIndex);
        //阵容魔物数量文本：满员时数量显示为通用警示红
        string countStr = ColorUtil.WrapLimitFull($"{listCreatureIds.Count}/{creatureNumMax}", listCreatureIds.Count >= creatureNumMax);
        ui_LineupHint.text = string.Format(TextHandler.Instance.GetTextById(30007), countStr);
    }

    /// <summary>
    /// 改变队伍序号
    /// </summary>
    public void ChangeLineupIndex(int indexLineup)
    {
        currentLineupIndex = indexLineup;
        //移除所有展示中的阵容卡片
        RemoveLineupCardShow();
        //初始化阵容卡片
        InitLineupData();
        //重排并刷新背包卡片(默认排序以当前阵容为准,切换阵容后需重排)
        InitCreatureData();
        //刷新UI数据
        RefreshUIData();
    }

    /// <summary>
    /// 获取阵容卡片位置
    /// </summary>
    public Vector3 GetLineupPostion(int maxLineupNum, int lineupPosIndex)
    {
        float wView = ui_LineupContent.rect.width;
        float itemW = wView / maxLineupNum;
        if (itemW > modelLineupItem.rectTransform.rect.width)
        {
            itemW = modelLineupItem.rectTransform.rect.width;
        }
        return new Vector3(itemW * lineupPosIndex - wView / 2f + itemW / 2f, 0, 0);
    }

    /// <summary>
    /// 由拖拽落点的横坐标反解目标槽位序号(GetLineupPostion 的逆运算)，夹回 [0, 总数-1]
    /// </summary>
    public int GetLineupDropPosIndex(float localX, int maxLineupNum)
    {
        float wView = ui_LineupContent.rect.width;
        float itemW = wView / maxLineupNum;
        if (itemW > modelLineupItem.rectTransform.rect.width)
        {
            itemW = modelLineupItem.rectTransform.rect.width;
        }
        int posIndex = Mathf.RoundToInt((localX + wView / 2f - itemW / 2f) / itemW);
        return Mathf.Clamp(posIndex, 0, maxLineupNum - 1);
    }

        /// <summary>
    /// 背包列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreature(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        //设置选中和未选中状态
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData.CheckIsLineup(currentLineupIndex, itemData.creatureUUId))
        {
            itemView.SetCardState(CardStateEnum.LineupSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.LineupNoSelect);
        }
    }
    
    #endregion
    #region 动画相关
    /// <summary>
    /// 播放所有lineup卡片重置位置动画
    /// </summary>
    /// <param name="animType">0默认 1初始化</param>
    /// <param name="actionForComplete"></param>
    public void AnimForAllLineupCardPosReset(int animType, Action actionForComplete)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        int completeNum = 0;
        int totalCard = listShowCardLineup.Count;
        //通知卡片移动位置
        for (int i = 0; i < totalCard; i++)
        {
            var itemCardView = listShowCardLineup[i];
            itemCardView.transform.SetAsLastSibling();
            var itemLineupPosIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemCardView.cardData.creatureData.creatureUUId);
            Vector3 itemLineupPos = GetLineupPostion(totalCard, itemLineupPosIndex);
            //错开延迟：初始化(animType==1)左到右级联；单卡增删(animType==0)让刚操作的卡(列表末位)零延迟先动，其余依次跟上，避免点击后等待
            float delay = (animType == 1 ? i : (totalCard - 1 - i)) * timeForLineupCardStagger;
            //播放动画
            itemCardView.transform.DOKill();
            if (animType == 1)
            {
                //初始化动画：从下方弹入带回弹(与战斗发牌统一走 AnimForCardShow,落位时播放卡片音效)
                itemCardView.AnimForCardShow(itemLineupPos, i, timeForLineupCardStagger, timeForLineupCardMoveInit, Ease.OutBack, () =>
                {
                    completeNum++;
                    if (completeNum == totalCard)
                    {
                        actionForComplete?.Invoke();
                    }
                });
            }
            else
            {
                //默认动画：带轻微缩放弹跳和错开效果
                float timeForMove = timeForLineupCardMove;
                Sequence cardSequence = DOTween.Sequence();
                cardSequence
                    .AppendInterval(delay)
                    .Append(itemCardView.transform.DOLocalMove(itemLineupPos, timeForMove).SetEase(Ease.OutBack))
                    .Join(itemCardView.transform.DOScale(1.08f, timeForMove * 0.5f).SetEase(Ease.OutQuad))
                    .Append(itemCardView.transform.DOScale(1f, timeForMove * 0.5f).SetEase(Ease.InOutQuad))
                    .OnComplete(() =>
                    {
                        completeNum++;
                        if (completeNum == totalCard)
                        {
                            actionForComplete?.Invoke();
                        }
                    });
            }
        }
        //如果没有卡片，直接回调
        if (totalCard == 0)
        {
            actionForComplete?.Invoke();
        }
    }
    #endregion
    #region 阵容里卡片相关处理
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
            GameObject ObjNewCard = Instantiate(ui_LineupContent.gameObject, modelLineupItem.gameObject);
            lineupView = ObjNewCard.GetComponent<UIViewCreatureCardItem>();
        }
        lineupView.SetData(creatureData, CardUseStateEnum.Lineup);
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

    /// <summary>
    /// 移除所有展示的阵容卡片
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
    #endregion
    #region  点击相关

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
    #endregion

    #region 回调相关
    /// <summary>
    /// 事件-焦点选中卡片
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// 事件-焦点离开
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// 事件-点击
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {
        //点击阵容卡片时播放卡片音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_card_1);
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (targetView.cardData.cardUseState == CardUseStateEnum.LineupBackpack && targetView.cardData.cardState == CardStateEnum.LineupNoSelect)
        {
            //如果已经超过阵容生物上限
            var userUnlock = userData.GetUserUnlockData();
            int creatureNumMax = userUnlock.GetUnlockLineupCreatureNum();
            var listLineupCreatureId = userData.GetLineupCreature(currentLineupIndex);
            if (listLineupCreatureId.Count >= creatureNumMax)
            {       
                //弹出提示
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(30006));
                return;
            }
            userData.AddLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureUUId);
            //刷新背包里的卡片
            ui_UIViewCreatureCardList.RefreshCardByIndex(targetView.cardData.indexList);
            //增加阵容卡
            Vector3 posStart = UGUIUtil.GetRootPos(ui_LineupContent.transform, targetView.transform);
            AddLineupCard(targetView.cardData.creatureData, posStart);
            //刷新UI数据
            RefreshUIData();
        }
        else if (targetView.cardData.cardUseState == CardUseStateEnum.Lineup)
        {
            userData.RemoveLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureUUId);
            //刷新背包里的卡片
            ui_UIViewCreatureCardList.RefreshCardByCreatureUUId(targetView.cardData.creatureData.creatureUUId);
            //移除阵容卡
            RemoveLineupCard(targetView);
            //刷新UI数据
            RefreshUIData();
        }
    }

    /// <summary>
    /// 事件-开始拖拽阵容卡片(卡片自身已置顶跟手，此处无需额外处理)
    /// </summary>
    public void EventForCardBeginDrag(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// 事件-结束拖拽阵容卡片：按落点横坐标反解目标槽位换位，随后全部卡片吸附归位(夹回)
    /// </summary>
    public void EventForCardEndDrag(UIViewCreatureCardItem targetView)
    {
        int totalCard = listShowCardLineup.Count;
        if (totalCard > 1)
        {
            int targetPosIndex = GetLineupDropPosIndex(targetView.transform.localPosition.x, totalCard);
            var userData = GameDataHandler.Instance.manager.GetUserData();
            userData.MoveLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureUUId, targetPosIndex);
        }
        //所有卡吸附到新槽位(含夹回)
        AnimForAllLineupCardPosReset(0, null);
    }

    public void RadioButtonSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {
        for (int i = 0; i < listLineupTag.Count; i++)
        {
            var itemLineupTag = listLineupTag[i];
            if (rbview == itemLineupTag)
            {
                ChangeLineupIndex(position + 1);
            }
        }
    }

    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }
    #endregion
}
