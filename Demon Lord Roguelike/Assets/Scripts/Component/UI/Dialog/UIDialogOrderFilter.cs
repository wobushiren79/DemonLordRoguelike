using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 排序筛选弹窗。
/// 在指定按钮处弹出,可多选筛选类型(按选择顺序决定优先级,index0=最高优先级),点击「正序/倒序」按钮确认,
/// 通过回调把(已选筛选类型列表, 是否正序)回传给调用方,由调用方负责排序。点击背景关闭。
/// </summary>
public partial class UIDialogOrderFilter : DialogView
{
    #region 数据
    //当前已选中的筛选类型(按选择/优先级顺序排列,index0=最高优先级)
    protected List<OrderFilterTypeEnum> selectFilterTypes = new List<OrderFilterTypeEnum>();
    //确认回调:参数1=已选筛选类型(按优先级排序),参数2=是否正序
    protected Action<List<OrderFilterTypeEnum>, bool> actionForConfirm;
    //筛选类型 -> 对应的筛选项视图
    protected Dictionary<OrderFilterTypeEnum, UIViewDialogOrderFilterItem> dicItem = new Dictionary<OrderFilterTypeEnum, UIViewDialogOrderFilterItem>();
    #endregion

    #region 数据初始化
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="dialogData">弹窗数据(需为 DialogOrderFilterBean)</param>
    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        DialogOrderFilterBean filterData = dialogData as DialogOrderFilterBean;
        if (filterData == null)
            return;
        actionForConfirm = filterData.actionForConfirm;
        //拷贝默认选中(按优先级顺序),不显示的项稍后在 InitItems 中剔除
        selectFilterTypes.Clear();
        if (filterData.selectFilterTypes != null)
            selectFilterTypes.AddRange(filterData.selectFilterTypes);
        //初始化正序/倒序按钮文本(多语言)
        InitSubmitText();
        //初始化筛选项
        InitItems(filterData.listFilterType);
        //根据点击的按钮调整弹窗内容位置
        RefreshDialogContentPosition(filterData.targetButton);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 初始化正序/倒序按钮的多语言文本
    /// </summary>
    protected void InitSubmitText()
    {
        if (ui_TextAscendingSubmit != null)
            ui_TextAscendingSubmit.text = TextHandler.Instance.GetTextById(2000012);
        if (ui_TextDescendingSubmit != null)
            ui_TextDescendingSubmit.text = TextHandler.Instance.GetTextById(2000013);
    }

    /// <summary>
    /// 初始化筛选项:按可选列表显隐,并设置(多选)选中态与点击回调。
    /// 不显示的默认选中项会被剔除(无法选中隐藏的筛选)。
    /// </summary>
    /// <param name="listFilterType">可供选择的筛选类型(为空显示全部)</param>
    protected void InitItems(List<OrderFilterTypeEnum> listFilterType)
    {
        dicItem.Clear();
        //序列化引用为同节点上的 PopupButtonCommonView,转取实际的筛选项视图
        RegisterItem(OrderFilterTypeEnum.Name, ui_UIViewDialogOrderFilterItem_Name);
        RegisterItem(OrderFilterTypeEnum.Level, ui_UIViewDialogOrderFilterItem_Level);
        RegisterItem(OrderFilterTypeEnum.Rarity, ui_UIViewDialogOrderFilterItem_Rarity);
        RegisterItem(OrderFilterTypeEnum.Lineup, ui_UIViewDialogOrderFilterItem_Lineup);
        RegisterItem(OrderFilterTypeEnum.Class, ui_UIViewDialogOrderFilterItem_Class);
        RegisterItem(OrderFilterTypeEnum.Damage, ui_UIViewDialogOrderFilterItem_Damage);
        RegisterItem(OrderFilterTypeEnum.Kill, ui_UIViewDialogOrderFilterItem_Kill);
        RegisterItem(OrderFilterTypeEnum.DamageReceived, ui_UIViewDialogOrderFilterItem_DamageReceived);
        RegisterItem(OrderFilterTypeEnum.Exp, ui_UIViewDialogOrderFilterItem_Exp);

        foreach (var itemKV in dicItem)
        {
            OrderFilterTypeEnum itemType = itemKV.Key;
            UIViewDialogOrderFilterItem itemView = itemKV.Value;
            bool isShow = listFilterType == null || listFilterType.Count == 0 || listFilterType.Contains(itemType);
            itemView.gameObject.SetActive(isShow);
            if (!isShow)
            {
                //隐藏的筛选不能保留在选中列表里
                selectFilterTypes.Remove(itemType);
                continue;
            }
            itemView.SetData(itemType, selectFilterTypes.Contains(itemType), OnClickForItem);
        }
        //刷新栅格布局大小
        UGUIUtil.RefreshUISize(ui_ContentShow);
        UGUIUtil.RefreshUISize(ui_DialogContent);
    }

    /// <summary>
    /// 把序列化的 PopupButtonCommonView 引用转为同节点上的筛选项视图并登记
    /// </summary>
    /// <param name="filterType">筛选类型</param>
    /// <param name="refView">序列化引用(与筛选项视图同节点)</param>
    protected void RegisterItem(OrderFilterTypeEnum filterType, PopupButtonCommonView refView)
    {
        if (refView == null)
            return;
        UIViewDialogOrderFilterItem itemView = refView.GetComponent<UIViewDialogOrderFilterItem>();
        if (itemView != null)
            dicItem[filterType] = itemView;
    }

    /// <summary>
    /// 根据触发按钮调整弹窗内容(DialogContent)的位置与轴心。
    /// 轴心按鼠标所在屏幕象限取值:左下(0,0)/右下(1,0)/左上(0,1)/右上(1,1),
    /// 使内容朝屏幕内侧展开;位置对齐到触发按钮处。
    /// </summary>
    /// <param name="targetButton">触发弹窗的按钮</param>
    protected void RefreshDialogContentPosition(RectTransform targetButton)
    {
        if (ui_DialogContent == null)
            return;
        //轴心:依据鼠标所在屏幕象限
        float pivotX = Input.mousePosition.x <= Screen.width / 2f ? 0 : 1;
        float pivotY = Input.mousePosition.y <= Screen.height / 2f ? 0 : 1;
        ui_DialogContent.pivot = new Vector2(pivotX, pivotY);
        //位置:对齐到触发按钮(转换为内容父级——弹窗根——的本地坐标)
        if (targetButton != null)
        {
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetButton.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 localPoint))
                ui_DialogContent.anchoredPosition = localPoint;
        }
    }

    /// <summary>
    /// 确认:回传(已选筛选类型按优先级排序的副本, 是否正序)并关闭弹窗
    /// </summary>
    /// <param name="isAscending">是否正序</param>
    protected void Confirm(bool isAscending)
    {
        actionForConfirm?.Invoke(new List<OrderFilterTypeEnum>(selectFilterTypes), isAscending);
        DestroyDialog();
    }
    #endregion

    #region 点击回调
    /// <summary>
    /// 筛选项点击:多选切换。未选中则按选择顺序追加(成为当前最低优先级),已选中则移除;随后刷新所有项选中态。
    /// </summary>
    /// <param name="filterType">被点击项的筛选类型</param>
    protected void OnClickForItem(OrderFilterTypeEnum filterType)
    {
        if (selectFilterTypes.Contains(filterType))
            selectFilterTypes.Remove(filterType);
        else
            selectFilterTypes.Add(filterType);
        foreach (var itemKV in dicItem)
            itemKV.Value.SetSelect(selectFilterTypes.Contains(itemKV.Key));
    }

    /// <summary>
    /// 按钮点击:正序/倒序即确认
    /// </summary>
    /// <param name="viewButton">被点击的按钮</param>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_AscendingSubmit)
            Confirm(true);
        else if (viewButton == ui_DescendingSubmit)
            Confirm(false);
    }
    #endregion
}
