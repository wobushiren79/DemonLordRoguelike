using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 排序筛选弹窗。统一承载多种条件,分区段展示:
/// 名字区(ContentName,模糊查询输入框)、等级区(ContentLevel,左/右两个整数输入构成区间)、
/// 稀有度区(ContentRarity,多选)、战斗区(ContentData,伤害/击杀/承伤/经验,排序键多选)、其它区(ContentOther,阵容/同类,排序键多选)。
/// 各区段显隐由 listFilterType 推导;排序键按选择顺序决定优先级(index0=主键),战斗区选中项上移到容器最前以表达优先级、其它区保持原始顺序;
/// 点击「确认」回传 OrderFilterResultBean(排序键+名字+等级区间+稀有度)。点击背景关闭。
/// 约定:名字/等级/稀有度为「命中即置顶」条件——调用方不删行、全部展示,只把命中项排到前面,再按排序键次级排序。
/// </summary>
public partial class UIDialogOrderFilter : DialogView
{
    #region 数据
    //战斗(数据)区的排序维度
    protected static readonly OrderFilterTypeEnum[] dataTypes = { OrderFilterTypeEnum.Damage, OrderFilterTypeEnum.Kill, OrderFilterTypeEnum.DamageReceived, OrderFilterTypeEnum.Exp };
    //其它区的排序维度
    protected static readonly OrderFilterTypeEnum[] otherTypes = { OrderFilterTypeEnum.Lineup, OrderFilterTypeEnum.Class };

    //当前已选中的排序键(按选择/优先级顺序排列,index0=最高优先级)
    protected List<OrderFilterTypeEnum> selectFilterTypes = new List<OrderFilterTypeEnum>();
    //当前已选中的稀有度(多选筛选)
    protected List<RarityEnum> selectRarities = new List<RarityEnum>();
    //确认回调
    protected Action<OrderFilterResultBean> actionForConfirm;
    //排序键 -> 排序项视图(仅含战斗/其它区已显示的项)
    protected Dictionary<OrderFilterTypeEnum, UIViewDialogOrderFilterItem> dicItem = new Dictionary<OrderFilterTypeEnum, UIViewDialogOrderFilterItem>();
    //稀有度 -> 稀有度项视图
    protected Dictionary<RarityEnum, UIViewDialogOrderFilterItem> dicRarityItem = new Dictionary<RarityEnum, UIViewDialogOrderFilterItem>();
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
        //拷贝默认选中(排序键按优先级顺序、稀有度多选)
        selectFilterTypes.Clear();
        if (filterData.selectFilterTypes != null)
            selectFilterTypes.AddRange(filterData.selectFilterTypes);
        selectRarities.Clear();
        if (filterData.defaultRarities != null)
            selectRarities.AddRange(filterData.defaultRarities);

        List<OrderFilterTypeEnum> listFilterType = filterData.listFilterType;
        //各区段初始化(显隐 + 数据 + 自身布局刷新)
        InitStaticText();
        InitNameSection(listFilterType, filterData.defaultNameFilter);
        InitLevelSection(listFilterType, filterData.defaultLevelMin, filterData.defaultLevelMax);
        InitRaritySection(listFilterType);
        InitSortSections(listFilterType);
        //内层排布完成后再刷新外层容器,最后按触发按钮定位
        UGUIUtil.RefreshUISize(ui_ContentShow);
        UGUIUtil.RefreshUISize(ui_DialogContent);
        RefreshDialogContentPosition(filterData.targetButton);
    }
    #endregion

    #region 私有方法-区段初始化
    /// <summary>
    /// 初始化各区段静态标题文本:确认(1000001)、名字(2000019)、等级(2000020)、稀有度(2000018)、战斗(2000021)、其它(2000022)
    /// </summary>
    protected void InitStaticText()
    {
        SetText(ui_TextSubmit, 1000001);
        SetText(ui_ContentNameTitle, 2000019);
        SetText(ui_ContentLevelTitle, 2000020);
        SetText(ui_ContentRarityTitle, 2000018);
        SetText(ui_ContentDataTitle, 2000021);
        SetText(ui_ContentOtherTitle, 2000022);
    }

    /// <summary>
    /// 初始化名字区:显隐 + 占位提示(复用「输入名字...」302) + 回填默认名字
    /// </summary>
    /// <param name="listFilterType">开放的维度</param>
    /// <param name="defaultName">默认名字</param>
    protected void InitNameSection(List<OrderFilterTypeEnum> listFilterType, string defaultName)
    {
        bool isShow = IsSectionShow(listFilterType, OrderFilterTypeEnum.Name);
        SetSectionActive(ui_ContentName, isShow);
        if (!isShow || ui_ContentNameInput == null)
            return;
        if (ui_ContentNameInput.placeholder is TextMeshProUGUI placeholder)
            placeholder.text = TextHandler.Instance.GetTextById(302);
        ui_ContentNameInput.SetTextWithoutNotify(defaultName ?? "");
        UGUIUtil.RefreshUISize(ui_ContentName);
    }

    /// <summary>
    /// 初始化等级区:显隐 + 回填左右输入(0/不限留空) + 绑定输入约束(>=0、左不大于右)
    /// </summary>
    /// <param name="listFilterType">开放的维度</param>
    /// <param name="defaultMin">默认等级下限</param>
    /// <param name="defaultMax">默认等级上限</param>
    protected void InitLevelSection(List<OrderFilterTypeEnum> listFilterType, int defaultMin, int defaultMax)
    {
        bool isShow = IsSectionShow(listFilterType, OrderFilterTypeEnum.Level);
        SetSectionActive(ui_ContentLevel, isShow);
        if (!isShow)
            return;
        if (ui_ContentLevelLeftInput != null)
        {
            //占位提示「低等级...」
            if (ui_ContentLevelLeftInput.placeholder is TextMeshProUGUI leftPlaceholder)
                leftPlaceholder.text = TextHandler.Instance.GetTextById(2000023);
            ui_ContentLevelLeftInput.SetTextWithoutNotify(defaultMin > 0 ? defaultMin.ToString() : "");
            ui_ContentLevelLeftInput.onEndEdit.RemoveAllListeners();
            ui_ContentLevelLeftInput.onEndEdit.AddListener(OnLevelLeftEndEdit);
        }
        if (ui_ContentLevelRightInput != null)
        {
            //占位提示「高等级...」
            if (ui_ContentLevelRightInput.placeholder is TextMeshProUGUI rightPlaceholder)
                rightPlaceholder.text = TextHandler.Instance.GetTextById(2000024);
            ui_ContentLevelRightInput.SetTextWithoutNotify(defaultMax < int.MaxValue ? defaultMax.ToString() : "");
            ui_ContentLevelRightInput.onEndEdit.RemoveAllListeners();
            ui_ContentLevelRightInput.onEndEdit.AddListener(OnLevelRightEndEdit);
        }
        UGUIUtil.RefreshUISize(ui_ContentLevel);
    }

    /// <summary>
    /// 初始化稀有度区:显隐 + 6 个稀有度项(NameItem 取稀有度配置多语言,多选,回填选中态)
    /// </summary>
    /// <param name="listFilterType">开放的维度</param>
    protected void InitRaritySection(List<OrderFilterTypeEnum> listFilterType)
    {
        bool isShow = IsSectionShow(listFilterType, OrderFilterTypeEnum.Rarity);
        SetSectionActive(ui_ContentRarity, isShow);
        if (!isShow)
            return;
        dicRarityItem.Clear();
        RegisterRarityItem(RarityEnum.N, ui_UIViewDialogOrderFilterItem_Rarity_N);
        RegisterRarityItem(RarityEnum.R, ui_UIViewDialogOrderFilterItem_Rarity_R);
        RegisterRarityItem(RarityEnum.SR, ui_UIViewDialogOrderFilterItem_Rarity_SR);
        RegisterRarityItem(RarityEnum.SSR, ui_UIViewDialogOrderFilterItem_Rarity_SSR);
        RegisterRarityItem(RarityEnum.UR, ui_UIViewDialogOrderFilterItem_Rarity_UR);
        RegisterRarityItem(RarityEnum.L, ui_UIViewDialogOrderFilterItem_Rarity_L);
        foreach (var itemKV in dicRarityItem)
        {
            RarityEnum rarity = itemKV.Key;
            RarityInfoBean rarityInfo = RarityInfoCfg.GetItemData(rarity);
            itemKV.Value.SetDataForRarity(rarity, rarityInfo != null ? rarityInfo.name_language : "", selectRarities.Contains(rarity), OnClickForRarity);
        }
        UGUIUtil.RefreshUISize(ui_ContentRarity);
    }

    /// <summary>
    /// 初始化排序键区(战斗区 ContentData + 其它区 ContentOther):各自显隐、登记并设置选中态;战斗区选中项上移到最前,其它区保持原始顺序
    /// </summary>
    /// <param name="listFilterType">开放的维度</param>
    protected void InitSortSections(List<OrderFilterTypeEnum> listFilterType)
    {
        dicItem.Clear();
        //战斗(数据)区
        bool showData = IsSectionShow(listFilterType, dataTypes);
        SetSectionActive(ui_ContentData, showData);
        if (showData)
        {
            RegisterSortItem(OrderFilterTypeEnum.Damage, ui_UIViewDialogOrderFilterItem_Damage, listFilterType);
            RegisterSortItem(OrderFilterTypeEnum.Kill, ui_UIViewDialogOrderFilterItem_Kill, listFilterType);
            RegisterSortItem(OrderFilterTypeEnum.DamageReceived, ui_UIViewDialogOrderFilterItem_DamageReceived, listFilterType);
            RegisterSortItem(OrderFilterTypeEnum.Exp, ui_UIViewDialogOrderFilterItem_Exp, listFilterType);
        }
        //其它区
        bool showOther = IsSectionShow(listFilterType, otherTypes);
        SetSectionActive(ui_ContentOther, showOther);
        if (showOther)
        {
            RegisterSortItem(OrderFilterTypeEnum.Lineup, ui_UIViewDialogOrderFilterItem_Lineup, listFilterType);
            RegisterSortItem(OrderFilterTypeEnum.Class, ui_UIViewDialogOrderFilterItem_Class, listFilterType);
        }
        //剔除未显示的默认选中排序键(无法选中隐藏的项)
        selectFilterTypes.RemoveAll(t => !dicItem.ContainsKey(t));
        //刷新排序项布局:战斗区选中上移,其它区保持原始顺序
        RefreshSortItemOrder();
    }
    #endregion

    #region 私有方法-辅助
    /// <summary>
    /// 设置标题/确认文本(多语言)
    /// </summary>
    /// <param name="text">目标文本组件</param>
    /// <param name="textId">多语言id</param>
    protected void SetText(TextMeshProUGUI text, long textId)
    {
        if (text != null)
            text.text = TextHandler.Instance.GetTextById(textId);
    }

    /// <summary>
    /// 某区段是否显示:listFilterType 为空显示全部,否则只要包含任一相关维度即显示
    /// </summary>
    /// <param name="listFilterType">开放的维度</param>
    /// <param name="anyOf">该区段关联的维度</param>
    protected bool IsSectionShow(List<OrderFilterTypeEnum> listFilterType, params OrderFilterTypeEnum[] anyOf)
    {
        if (listFilterType == null || listFilterType.Count == 0)
            return true;
        for (int i = 0; i < anyOf.Length; i++)
            if (listFilterType.Contains(anyOf[i]))
                return true;
        return false;
    }

    /// <summary>
    /// 设置区段容器显隐
    /// </summary>
    /// <param name="section">区段容器</param>
    /// <param name="isShow">是否显示</param>
    protected void SetSectionActive(RectTransform section, bool isShow)
    {
        if (section != null)
            section.gameObject.SetActive(isShow);
    }

    /// <summary>
    /// 登记稀有度项到字典
    /// </summary>
    /// <param name="rarity">稀有度</param>
    /// <param name="itemView">稀有度项视图</param>
    protected void RegisterRarityItem(RarityEnum rarity, UIViewDialogOrderFilterItem itemView)
    {
        if (itemView != null)
            dicRarityItem[rarity] = itemView;
    }

    /// <summary>
    /// 登记并初始化单个排序项:按 listFilterType 决定该项显隐,显示则登记并设置选中态与点击回调
    /// </summary>
    /// <param name="type">排序键</param>
    /// <param name="itemView">排序项视图</param>
    /// <param name="listFilterType">开放的维度</param>
    protected void RegisterSortItem(OrderFilterTypeEnum type, UIViewDialogOrderFilterItem itemView, List<OrderFilterTypeEnum> listFilterType)
    {
        if (itemView == null)
            return;
        bool isShow = listFilterType == null || listFilterType.Count == 0 || listFilterType.Contains(type);
        itemView.gameObject.SetActive(isShow);
        if (!isShow)
            return;
        dicItem[type] = itemView;
        itemView.SetData(type, selectFilterTypes.Contains(type), OnClickForSortItem);
    }

    /// <summary>
    /// 刷新排序项容器:战斗区按选择顺序把已选项上移到标题之后(index0=标题,主键在最前)以表达优先级;
    /// 其它区保持原始(预制体)顺序、不随选中上移,仅刷新自身布局
    /// </summary>
    protected void RefreshSortItemOrder()
    {
        //战斗区:选中项上移表达优先级
        ReorderSelectedFirst(ui_ContentData, dataTypes);
        //其它区:保持原始顺序,不上移,仅刷新布局
        if (ui_ContentOther != null && ui_ContentOther.gameObject.activeSelf)
            UGUIUtil.RefreshUISize(ui_ContentOther);
    }

    /// <summary>
    /// 在指定容器内把属于该容器的已选排序键上移到标题之后(保持标题在最前)
    /// </summary>
    /// <param name="container">区段容器</param>
    /// <param name="sectionTypes">该容器关联的排序维度</param>
    protected void ReorderSelectedFirst(RectTransform container, OrderFilterTypeEnum[] sectionTypes)
    {
        if (container == null || !container.gameObject.activeSelf)
            return;
        //index0 为标题,排序项从 index1 起按选择顺序排布
        int order = 1;
        for (int i = 0; i < selectFilterTypes.Count; i++)
        {
            OrderFilterTypeEnum type = selectFilterTypes[i];
            if (Array.IndexOf(sectionTypes, type) < 0)
                continue;
            if (dicItem.TryGetValue(type, out UIViewDialogOrderFilterItem itemView) && itemView != null)
            {
                itemView.transform.SetSiblingIndex(order);
                order++;
            }
        }
        UGUIUtil.RefreshUISize(container);
    }

    /// <summary>
    /// 净化等级输入值:解析为非负整数;空串/非法返回 hasValue=false
    /// </summary>
    /// <param name="value">输入文本</param>
    /// <param name="hasValue">是否为有效输入</param>
    /// <returns>净化后的等级(>=0)</returns>
    protected int SanitizeLevel(string value, out bool hasValue)
    {
        hasValue = !string.IsNullOrEmpty(value);
        if (!hasValue)
            return 0;
        if (!int.TryParse(value, out int v))
        {
            hasValue = false;
            return 0;
        }
        return v < 0 ? 0 : v;
    }

    /// <summary>
    /// 读取等级输入框的值;空/非法返回给定默认值,负数夹为 0
    /// </summary>
    /// <param name="input">输入框</param>
    /// <param name="defaultValue">空/非法时的默认值</param>
    /// <returns>等级值</returns>
    protected int ParseLevelOrDefault(TMP_InputField input, int defaultValue)
    {
        if (input == null || string.IsNullOrEmpty(input.text))
            return defaultValue;
        if (int.TryParse(input.text, out int v))
            return v < 0 ? 0 : v;
        return defaultValue;
    }

    /// <summary>
    /// 根据触发按钮调整弹窗内容(DialogContent)的位置与轴心。
    /// 轴心按鼠标所在屏幕象限取值:左下(0,0)/右下(1,0)/左上(0,1)/右上(1,1),使内容朝屏幕内侧展开;位置对齐到触发按钮处。
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
    /// 确认:汇总各区段为 OrderFilterResultBean(名字/等级/稀有度仅在对应区段显示时生效),回传并关闭弹窗
    /// </summary>
    protected void Confirm()
    {
        OrderFilterResultBean result = new OrderFilterResultBean
        {
            sortTypes = new List<OrderFilterTypeEnum>(selectFilterTypes),
        };
        //名字
        if (ui_ContentName != null && ui_ContentName.gameObject.activeSelf && ui_ContentNameInput != null)
            result.nameFilter = ui_ContentNameInput.text;
        //等级区间(确认时再兜底一次:左不大于右)
        if (ui_ContentLevel != null && ui_ContentLevel.gameObject.activeSelf)
        {
            int min = ParseLevelOrDefault(ui_ContentLevelLeftInput, 0);
            int max = ParseLevelOrDefault(ui_ContentLevelRightInput, int.MaxValue);
            if (min > max)
            {
                int tmp = min; min = max; max = tmp;
            }
            result.levelMin = min;
            result.levelMax = max;
        }
        //稀有度
        if (ui_ContentRarity != null && ui_ContentRarity.gameObject.activeSelf)
            result.rarities = new List<RarityEnum>(selectRarities);
        actionForConfirm?.Invoke(result);
        DestroyDialog();
    }
    #endregion

    #region 点击/输入回调
    /// <summary>
    /// 排序项点击:多选切换。未选中则按选择顺序追加(成为当前最低优先级),已选中则移除;随后刷新选中态并把选中项上移到最前。
    /// </summary>
    /// <param name="filterType">被点击项的排序键</param>
    protected void OnClickForSortItem(OrderFilterTypeEnum filterType)
    {
        if (selectFilterTypes.Contains(filterType))
            selectFilterTypes.Remove(filterType);
        else
            selectFilterTypes.Add(filterType);
        foreach (var itemKV in dicItem)
            itemKV.Value.SetSelect(selectFilterTypes.Contains(itemKV.Key));
        RefreshSortItemOrder();
    }

    /// <summary>
    /// 稀有度项点击:多选切换,刷新所有稀有度项选中态
    /// </summary>
    /// <param name="rarity">被点击项的稀有度</param>
    protected void OnClickForRarity(RarityEnum rarity)
    {
        if (selectRarities.Contains(rarity))
            selectRarities.Remove(rarity);
        else
            selectRarities.Add(rarity);
        foreach (var itemKV in dicRarityItem)
            itemKV.Value.SetSelect(selectRarities.Contains(itemKV.Key));
    }

    /// <summary>
    /// 左等级输入完成:夹为非负;若右输入有值且左大于右,则左夹到右(左不能大于右)
    /// </summary>
    /// <param name="value">输入文本</param>
    protected void OnLevelLeftEndEdit(string value)
    {
        int left = SanitizeLevel(value, out bool hasValue);
        if (!hasValue)
        {
            ui_ContentLevelLeftInput.SetTextWithoutNotify("");
            return;
        }
        int right = ParseLevelOrDefault(ui_ContentLevelRightInput, int.MaxValue);
        if (left > right)
            left = right;
        ui_ContentLevelLeftInput.SetTextWithoutNotify(left.ToString());
    }

    /// <summary>
    /// 右等级输入完成:夹为非负;若左输入有值且右小于左,则右夹到左(右不能小于左)
    /// </summary>
    /// <param name="value">输入文本</param>
    protected void OnLevelRightEndEdit(string value)
    {
        int right = SanitizeLevel(value, out bool hasValue);
        if (!hasValue)
        {
            ui_ContentLevelRightInput.SetTextWithoutNotify("");
            return;
        }
        int left = ParseLevelOrDefault(ui_ContentLevelLeftInput, 0);
        if (right < left)
            right = left;
        ui_ContentLevelRightInput.SetTextWithoutNotify(right.ToString());
    }

    /// <summary>
    /// 确认按钮点击:确认按钮 Submit 节点绑定到基类 ui_Submit,基类会把点击派发到此方法。
    /// </summary>
    public override void SubmitOnClick()
    {
        Confirm();
    }
    #endregion
}
