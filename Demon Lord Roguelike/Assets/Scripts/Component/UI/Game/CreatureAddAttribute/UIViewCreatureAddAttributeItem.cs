using System;
using UnityEngine.UI;

/// <summary>
/// 单个属性加点项 UI: 展示某一属性(HP/护甲/攻击/攻速)的图标与本次已分配点数,
/// 通过左右按钮加减一点, 通过 Max/Min 按钮一键拉满剩余点数或清零本项。
/// </summary>
public partial class UIViewCreatureAddAttributeItem : BaseUIView
{
    #region 数据
    /// <summary>一键拉满时向父级请求的极大增量(父级会按剩余点数钳制, 不会溢出)</summary>
    private const int DeltaForSetMax = int.MaxValue;
    /// <summary>本项对应的属性类型</summary>
    public CreatureAttributeTypeEnum attributeType;
    /// <summary>每点该属性增加的数值(由 CreatureUtil.GetAttributePointAddValue 给出)</summary>
    public float addValuePerPoint;
    /// <summary>本次加点会话中已分配到该属性的点数</summary>
    public int allocatedCount;
    /// <summary>加减回调: 参数为(本项, 增量), 增量可为负数或超过剩余点数的极值, 由父级 UICreatureAddAttribute 钳制后落实</summary>
    private Action<UIViewCreatureAddAttributeItem, int> actionForChange;
    #endregion

    #region 公有方法
    /// <summary>
    /// 设置数据: 绑定属性类型与加减回调,并重置本次加点计数。
    /// <para>图标(ui_Icon)按属性固定,在预制体内配置,代码不动态设置。</para>
    /// </summary>
    /// <param name="attributeType">属性类型</param>
    /// <param name="actionForChange">加减回调(本项, 增量), 父级负责钳制</param>
    public void SetData(CreatureAttributeTypeEnum attributeType, Action<UIViewCreatureAddAttributeItem, int> actionForChange)
    {
        this.attributeType = attributeType;
        this.addValuePerPoint = CreatureUtil.GetAttributePointAddValue(attributeType);
        this.actionForChange = actionForChange;
        this.allocatedCount = 0;
        RefreshNum();
    }

    /// <summary>
    /// 刷新加点数显示(本次会话已分配到该属性的点数)。
    /// <para>仅展示分配点数, 与单点实际增量(HP/护甲每点+10、攻击/攻速每点+1)解耦, 各属性步进器统一显示点数。</para>
    /// </summary>
    public void RefreshNum()
    {
        ui_Num.text = $"+{allocatedCount}";
    }
    #endregion

    #region 点击事件
    /// <summary>
    /// 按钮点击: 左按钮减一点,右按钮加一点; Min 一键清零本项, Max 一键拉满剩余点数(实际增减由父级回调钳制后执行)。
    /// </summary>
    /// <param name="viewButton">被点击的按钮</param>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_LeftButton)
        {
            actionForChange?.Invoke(this, -1);
        }
        else if (viewButton == ui_RightButton)
        {
            actionForChange?.Invoke(this, 1);
        }
        else if (viewButton == ui_LeftButtonMin)
        {
            //一键清零: 把本项已加点数全部退回剩余点数(差值即当前已加数)
            actionForChange?.Invoke(this, -allocatedCount);
        }
        else if (viewButton == ui_RightButtonMax)
        {
            //一键拉满: 请求加满, 实际增量由父级按剩余点数钳制
            actionForChange?.Invoke(this, DeltaForSetMax);
        }
    }
    #endregion
}
