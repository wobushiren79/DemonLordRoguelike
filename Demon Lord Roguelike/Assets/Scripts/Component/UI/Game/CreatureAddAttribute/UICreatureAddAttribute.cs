using System;
using UnityEngine.UI;

/// <summary>
/// 属性加点界面: 献祭升级成功后弹出, 玩家把本次升级获得的属性点手动分配到 HP/护甲(DR)/攻击(ATK)/攻速(ASPD)。
/// <para>加点实时作用于生物的升级加点属性并刷新详情展示; 剩余点数必须全部分配完才能确认离开(当场加完, 不持久化剩余点数)。</para>
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
    /// 初始化四个属性加点项(HP/护甲/攻击/攻速),绑定属性类型与加减回调。
    /// </summary>
    public void InitItems()
    {
        ui_UIViewCreatureAddAttributeItem_HP.SetData(CreatureAttributeTypeEnum.HP, OnItemChangeForAttribute);
        ui_UIViewCreatureAddAttributeItem_DR.SetData(CreatureAttributeTypeEnum.DR, OnItemChangeForAttribute);
        ui_UIViewCreatureAddAttributeItem_ATK.SetData(CreatureAttributeTypeEnum.ATK, OnItemChangeForAttribute);
        ui_UIViewCreatureAddAttributeItem_ASPD.SetData(CreatureAttributeTypeEnum.ASPD, OnItemChangeForAttribute);
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
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击确认/离开: 剩余点数未分配完时提示并拦截, 全部分配完后触发确认回调。
    /// </summary>
    public void OnClickForExit()
    {
        if (remainPoint > 0)
        {
            //剩余点数未分配完: 提示并拦截(多语言 textId 61004)
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61004), 1);
            return;
        }
        actionForConfirm?.Invoke();
    }
    #endregion
}
