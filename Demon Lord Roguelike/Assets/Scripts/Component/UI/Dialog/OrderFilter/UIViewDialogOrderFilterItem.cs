using System;
using UnityEngine.UI;

/// <summary>
/// 排序筛选弹窗 - 单个筛选项。
/// 点击切换选中状态;选中时显示 CheckBoxSubmit;鼠标悬浮显示对应筛选的详情气泡。
/// </summary>
public partial class UIViewDialogOrderFilterItem : BaseUIView
{
    #region 数据
    //该项代表的筛选类型
    protected OrderFilterTypeEnum filterType;
    //点击回调(把自身筛选类型回传给弹窗,由弹窗统一处理多选)
    protected Action<OrderFilterTypeEnum> actionForClick;
    #endregion

    #region 生命周期
    /// <summary>
    /// 初始化:该预制体的 Button 与 PopupButtonCommonView 与本组件挂在同一节点上,
    /// 未在序列化中赋值,运行时按需补齐引用。
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        if (ui_UIViewDialogOrderFilterItem_Button == null)
            ui_UIViewDialogOrderFilterItem_Button = GetComponent<Button>();
        if (ui_UIViewDialogOrderFilterItem_PopupButtonCommonView == null)
            ui_UIViewDialogOrderFilterItem_PopupButtonCommonView = GetComponent<PopupButtonCommonView>();
    }
    #endregion

    #region 公有方法
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="filterType">该项代表的筛选类型</param>
    /// <param name="isSelect">是否选中</param>
    /// <param name="actionForClick">点击回调</param>
    public void SetData(OrderFilterTypeEnum filterType, bool isSelect, Action<OrderFilterTypeEnum> actionForClick)
    {
        this.filterType = filterType;
        this.actionForClick = actionForClick;
        //悬浮详情气泡(复用排序标签多语言文本)
        if (ui_UIViewDialogOrderFilterItem_PopupButtonCommonView != null)
            ui_UIViewDialogOrderFilterItem_PopupButtonCommonView.SetData(GetFilterDetail(filterType), PopupEnum.Text);
        SetSelect(isSelect);
    }

    /// <summary>
    /// 设置选中状态(选中显示对勾 CheckBoxSubmit)
    /// </summary>
    /// <param name="isSelect">是否选中</param>
    public void SetSelect(bool isSelect)
    {
        if (ui_CheckBoxSubmit != null)
            ui_CheckBoxSubmit.gameObject.SetActive(isSelect);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 获取筛选类型对应的悬浮详情多语言文本(复用排序标签文本ID)
    /// </summary>
    /// <param name="filterType">筛选类型</param>
    /// <returns>详情文本</returns>
    protected string GetFilterDetail(OrderFilterTypeEnum filterType)
    {
        switch (filterType)
        {
            case OrderFilterTypeEnum.Rarity:
                return TextHandler.Instance.GetTextById(2000004);
            case OrderFilterTypeEnum.Level:
                return TextHandler.Instance.GetTextById(2000005);
            case OrderFilterTypeEnum.Lineup:
                return TextHandler.Instance.GetTextById(2000006);
            case OrderFilterTypeEnum.Name:
                return TextHandler.Instance.GetTextById(2000007);
            case OrderFilterTypeEnum.Class:
                return TextHandler.Instance.GetTextById(2000011);
            case OrderFilterTypeEnum.Damage:
                return TextHandler.Instance.GetTextById(50001);
            case OrderFilterTypeEnum.Kill:
                return TextHandler.Instance.GetTextById(50002);
            case OrderFilterTypeEnum.DamageReceived:
                return TextHandler.Instance.GetTextById(50004);
            case OrderFilterTypeEnum.Exp:
                return TextHandler.Instance.GetTextById(50003);
        }
        return "";
    }
    #endregion

    #region 按钮点击
    /// <summary>
    /// 按钮点击:该项仅有一个按钮,点击即把自身筛选类型回传给弹窗
    /// </summary>
    /// <param name="viewButton">被点击的按钮</param>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        actionForClick?.Invoke(filterType);
    }
    #endregion
}
