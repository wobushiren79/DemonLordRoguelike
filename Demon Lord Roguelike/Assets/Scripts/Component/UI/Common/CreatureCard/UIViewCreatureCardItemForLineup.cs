using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

//阵容卡片特殊设置
public partial class UIViewCreatureCardItemForLineup : UIViewCreatureCardItem, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //是否正在拖拽(用于屏蔽拖拽结束瞬间误触发的点击移除)
    protected bool isDragging = false;
    //按下时指针与卡片中心的横向偏移(避免拖拽瞬间卡片跳到指针处)
    protected float dragOffsetX = 0f;

    #region 重写
    /// <summary>
    /// 刷新状态
    /// </summary>
    public override void RefreshCardState(CardStateEnum cardState)
    {
        base.RefreshCardState(cardState);
        switch (cardState)
        {
            case CardStateEnum.LineupSelect:
                ui_Mask.gameObject.SetActive(true);
                break;
            case CardStateEnum.LineupNoSelect:
                break;
        }
    }

    /// <summary>
    /// 点击选择：拖拽结束瞬间不触发点击(否则会被当成点击移除)
    /// </summary>
    public override void OnClickSelect()
    {
        if (isDragging)
            return;
        base.OnClickSelect();
    }
    #endregion

    #region 拖拽换位
    /// <summary>
    /// 开始拖拽：置顶显示并记录抓取偏移，派发开始拖拽事件
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        //仅阵容卡片可拖拽换位
        if (cardData.cardUseState != CardUseStateEnum.Lineup)
            return;
        isDragging = true;
        transform.DOKill();
        transform.SetAsLastSibling();
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            dragOffsetX = transform.localPosition.x - localPoint.x;
        }
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnBeginDrag, (UIViewCreatureCardItem)this);
    }

    /// <summary>
    /// 拖拽中：卡片横向跟随指针(纵向锁定在阵容行上)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Vector3 pos = transform.localPosition;
            pos.x = localPoint.x + dragOffsetX;
            transform.localPosition = pos;
        }
    }

    /// <summary>
    /// 结束拖拽：派发结束拖拽事件由管理器换位夹回，清除拖拽标记
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnEndDrag, (UIViewCreatureCardItem)this);
        isDragging = false;
    }
    #endregion
}
