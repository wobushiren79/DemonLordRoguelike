using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 排序筛选弹窗(UIDialogOrderFilter)数据。
/// </summary>
public class DialogOrderFilterBean : DialogBean
{
    //触发弹窗的按钮(用于把弹窗内容定位到该按钮处)
    public RectTransform targetButton;
    //可供选择的筛选类型(为空或不传则显示全部)
    public List<OrderFilterTypeEnum> listFilterType;
    //默认已选中的筛选类型(按优先级从高到低排序,index0=最高优先级;为空则默认无选中)
    public List<OrderFilterTypeEnum> selectFilterTypes;
    //确认回调:参数1=已选筛选类型(可多选,按选择/优先级顺序排列,index0=最高优先级),参数2=是否正序(true正序/false倒序),由调用方据此排序
    public Action<List<OrderFilterTypeEnum>, bool> actionForConfirm;
}
