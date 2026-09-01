using System;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 属性加点界面: 献祭升级成功后弹出, 玩家把本次升级获得的属性点手动分配到该生物 creatureInfo.show_attribute 配置的属性上(HP/护甲(DR)/攻击(ATK)/攻速(ASPD) 4 项内按配置显隐)。
/// <para>加点实时作用于生物的升级加点属性并刷新详情展示; 必须分配完所有点数后, 点击确认按钮(BtnConfirm)弹出二次确认弹窗, 确认后才保存数据(当场加完, 不持久化剩余点数)。</para>
/// </summary>
public partial class UICreatureAddAttribute : BaseUIComponent
{
    #region 数据
    /// <summary>正在加点的目标生物(与存档同一引用,加点原地生效)</summary>
    public CreatureBean creatureData;
    /// <summary>本次可分配的总点数</summary>
    public int totalPoint;
    /// <summary>尚未分配的剩余点数</summary>
    public int remainPoint;
    /// <summary>确认回调(剩余点数全部分配完后触发, 由调用方负责存档与界面跳转)</summary>
    private Action actionForConfirm;
    #endregion

    #region 生命周期
    /// <summary>
    /// 设置数据: 在 OpenUI 前调用, 绑定目标生物、可分配点数与确认回调。
    /// </summary>
    /// <param name="creatureData">目标生物</param>
    /// <param name="totalPoint">本次可分配的总点数</param>
    /// <param name="actionForConfirm">点数分配完毕后的确认回调</param>
    public void SetData(CreatureBean creatureData, int totalPoint, Action actionForConfirm)
    {
        this.creatureData = creatureData;
        this.totalPoint = totalPoint;
        this.remainPoint = totalPoint;
        this.actionForConfirm = actionForConfirm;
    }

    /// <summary>
    /// 打开界面: 屏蔽基地控制, 初始化属性项与详情展示, 刷新剩余点数。
    /// </summary>
    public override void OpenUI()
    {
        base.OpenUI();
        //与其它基地子界面一致: 加点期间不可控制角色移动
        GameControlHandler.Instance.SetBaseControl(false);
        InitItems();
        ui_UIViewCreatureCardDetails.SetData(creatureData);
        RefreshLimmit();
    }
    #endregion

    #region 设置数据
    /// <summary>
    /// 初始化属性加点项: 按 creatureInfo.show_attribute 配置开关 HP/护甲/攻击/攻速 4 个固定项的显隐(每项显式设置防UI复用残留),
    /// 仅可见项绑定属性类型与加减回调;配置含 4 项以外属性时告警忽略;配置无可加点属性时强制显示HP项兜底可分配出口。
    /// </summary>
    public void InitItems()
    {
        var listShowAttribute = creatureData.creatureInfo.GetShowAttributeList();
        InitItem(ui_UIViewCreatureAddAttributeItem_HP, CreatureAttributeTypeEnum.HP, listShowAttribute);
        InitItem(ui_UIViewCreatureAddAttributeItem_DR, CreatureAttributeTypeEnum.DR, listShowAttribute);
        InitItem(ui_UIViewCreatureAddAttributeItem_ATK, CreatureAttributeTypeEnum.ATK, listShowAttribute);
        InitItem(ui_UIViewCreatureAddAttributeItem_ASPD, CreatureAttributeTypeEnum.ASPD, listShowAttribute);
        //配置含 4 固定项以外的属性(如MSPD移速)时告警忽略(加点界面无对应item),同时检查是否存在可加点项
        bool hasAllocatable = false;
        for (int i = 0; i < listShowAttribute.Count; i++)
        {
            var attributeType = listShowAttribute[i];
            if (attributeType == CreatureAttributeTypeEnum.HP || attributeType == CreatureAttributeTypeEnum.DR
                || attributeType == CreatureAttributeTypeEnum.ATK || attributeType == CreatureAttributeTypeEnum.ASPD)
            {
                hasAllocatable = true;
            }
            else
            {
                LogUtil.LogWarning($"生物 id:{creatureData.creatureId} show_attribute 配置了加点界面不支持的属性:{attributeType},已忽略");
            }
        }
        //兜底:配置无可加点属性时强制显示HP项,否则点数无法分配会被"需分配完"拦截卡死
        if (!hasAllocatable)
        {
            LogUtil.LogWarning($"生物 id:{creatureData.creatureId} show_attribute 配置无可加点属性,已兜底显示HP项");
            ui_UIViewCreatureAddAttributeItem_HP.gameObject.SetActive(true);
            ui_UIViewCreatureAddAttributeItem_HP.SetData(CreatureAttributeTypeEnum.HP, OnItemChangeForAttribute);
        }
    }

    /// <summary>
    /// 初始化单个加点项: 配置列表包含该属性则显示并绑定数据,否则隐藏(显式设置防UI复用残留)。
    /// </summary>
    /// <param name="item">加点项</param>
    /// <param name="attributeType">该项对应的属性类型</param>
    /// <param name="listShowAttribute">配置的可加点属性列表(creatureInfo.show_attribute)</param>
    protected void InitItem(UIViewCreatureAddAttributeItem item, CreatureAttributeTypeEnum attributeType, List<CreatureAttributeTypeEnum> listShowAttribute)
    {
        bool isShow = listShowAttribute.Contains(attributeType);
        item.gameObject.SetActive(isShow);
        if (isShow)
        {
            item.SetData(attributeType, OnItemChangeForAttribute);
        }
    }

    /// <summary>
    /// 刷新剩余加点数量显示("剩余点数:{0}", 多语言 textId 61005)。
    /// </summary>
    public void RefreshLimmit()
    {
        ui_LimmitText.text = string.Format(TextHandler.Instance.GetTextById(61005), remainPoint);
    }
    #endregion

    #region 事件
    /// <summary>
    /// 属性项加减回调: 加点需有剩余点数, 减点不能低于本次已加点数; 增减实时作用于生物属性并刷新详情。
    /// </summary>
    /// <param name="item">触发的属性项</param>
    /// <param name="delta">增量(+1 加点 / -1 减点)</param>
    public void OnItemChangeForAttribute(UIViewCreatureAddAttributeItem item, int delta)
    {
        //加点
        if (delta > 0)
        {
            //没有剩余点数
            if (remainPoint <= 0)
                return;
            item.allocatedCount += 1;
            remainPoint -= 1;
            creatureData.creatureAttribute.AddAttributeForLevelUp(item.attributeType, item.addValuePerPoint);
        }
        //减点
        else
        {
            //本次未对该属性加过点, 不能再减
            if (item.allocatedCount <= 0)
                return;
            item.allocatedCount -= 1;
            remainPoint += 1;
            creatureData.creatureAttribute.AddAttributeForLevelUp(item.attributeType, -item.addValuePerPoint);
        }
        //刷新本项数值、详情属性与剩余点数
        item.RefreshNum();
        ui_UIViewCreatureCardDetails.RefreshCard();
        RefreshLimmit();
    }
    #endregion

    #region 点击事件
    /// <summary>
    /// 按钮点击处理。
    /// </summary>
    /// <param name="viewButton">被点击的按钮</param>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnConfirm)
        {
            OnClickForConfirm();
        }
    }

    /// <summary>
    /// 点击确认: 剩余点数未分配完时提示并拦截(需加完才能确认); 全部分配完后弹出二次确认弹窗, 确认后触发保存回调。
    /// </summary>
    public void OnClickForConfirm()
    {
        if (remainPoint > 0)
        {
            //还有未分配的属性点: 提示需全部分配完才能确认(多语言 textId 61004)
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61004), 1);
            return;
        }
        //点数已全部分配: 弹出二次确认弹窗, 确认后才保存(分配后无法更改, textId 61006)
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(61006);
        dialogData.actionSubmit = (view, data) =>
        {
            actionForConfirm?.Invoke();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
    #endregion
}
