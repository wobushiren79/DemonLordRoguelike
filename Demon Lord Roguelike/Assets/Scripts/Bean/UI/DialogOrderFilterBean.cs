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
    //开放哪些维度(决定各区段显隐:名字/等级/稀有度/战斗(数据)/其它;为空或不传则全部显示)
    public List<OrderFilterTypeEnum> listFilterType;
    //默认已选中的排序键(按优先级从高到低,index0=最高;来自战斗/其它区;为空则默认无选中)
    public List<OrderFilterTypeEnum> selectFilterTypes;
    //默认名字筛选(回填名字输入框)
    public string defaultNameFilter;
    //默认等级下限(回填左输入框;0=不限下限,输入框留空)
    public int defaultLevelMin = 0;
    //默认等级上限(回填右输入框;int.MaxValue=不限上限,输入框留空)
    public int defaultLevelMax = int.MaxValue;
    //默认选中的稀有度(回填稀有度多选;为空则默认全不选=不按稀有度筛选)
    public List<RarityEnum> defaultRarities;
    //道具类型区可选项(按上下文动态提供,如当前魔物的可装备类型;按顺序填充预留的道具类型项,多余项隐藏。为空/不含 ItemType 维度则该区隐藏)
    public List<ItemTypeEnum> itemTypes;
    //默认选中的道具类型(回填道具类型多选;为空则默认全不选=不按道具类型筛选)
    public List<ItemTypeEnum> defaultItemTypes;
    //确认回调:回传 OrderFilterResultBean(排序键 + 名字 + 等级区间 + 稀有度 + 道具类型),由调用方据此先过滤再排序
    public Action<OrderFilterResultBean> actionForConfirm;
}
