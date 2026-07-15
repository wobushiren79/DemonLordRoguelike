using System;
using System.Collections.Generic;
using UnityEngine.UI;

public partial class UICreatureJuicer : BaseUIComponent
{
    //退出回调(由打开入口注入:场景E键交互返回UIBaseMain)
    public Action actionForExit;
    //当前展示的可榨汁魔物列表
    public List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //当前选中的目标魔物(单选)
    public CreatureBean targetCreatureSelect;

    #region 生命周期
    public override void OpenUI()
    {
        base.OpenUI();
        //关闭基地移动控制(与其它基地子界面一致):避免榨汁界面期间仍能控制角色移动
        GameControlHandler.Instance.SetBaseControl(false);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        InitCreatureData();
        RefreshUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewCreatureCardList_Target.CloseUI();
    }

    /// <summary>
    /// 刷新UI:重刷目标魔物列表卡片(选中态由 OnCellChangeForTarget 逐卡回填)
    /// </summary>
    public void RefreshUI()
    {
        ui_UIViewCreatureCardList_Target.RefreshAllCard();
    }
    #endregion

    #region 数据
    /// <summary>
    /// 初始化可榨汁魔物列表:取背包内空闲(未上阵、未被其它流程占用)的魔物。
    /// 复用进阶目标列表的卡片使用态(CreatureAscendTarget),与预制体已挂的卡片变体(UIViewCreatureCardItemForCreatureAscend)匹配。
    /// </summary>
    public void InitCreatureData()
    {
        targetCreatureSelect = null;
        listCreatureData.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.GetUserBackpackCreatureData().listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            //仅空闲态魔物可榨汁(排除进阶中/献祭中等占用态)
            if (creatureData.creatureState != CreatureStateEnum.Idle)
                return;
            //排除已上阵魔物
            if (userData.CheckIsInAnyLineup(creatureData.creatureUUId))
                return;
            listCreatureData.Add(creatureData);
        });
        ui_UIViewCreatureCardList_Target.SetData(listCreatureData, CardUseStateEnum.CreatureAscendTarget, OnCellChangeForTarget);
    }

    /// <summary>
    /// 目标魔物卡片刷新:按是否为当前选中态设置选中/未选中样式
    /// </summary>
    public void OnCellChangeForTarget(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (targetCreatureSelect == itemData)
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }
    #endregion

    #region 点击事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnStart)
        {
            OnClickForStart();
        }
    }

    /// <summary>
    /// 点击离开:执行由打开入口注入的退出回调(场景E键交互返回UIBaseMain)
    /// </summary>
    public void OnClickForExit()
    {
        actionForExit?.Invoke();
    }

    /// <summary>
    /// 点击开始榨汁:校验已选目标魔物后交由 CreatureJuicerLogic 处理(榨汁流程/奖励后续接入)
    /// </summary>
    public void OnClickForStart()
    {
        if (targetCreatureSelect == null)
        {
            //未选择目标魔物:提示并拦截
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61010));
            return;
        }
        //交由逻辑层开始榨汁(当前为留桩,后续接入榨汁流程与奖励)
        GameHandler.Instance.StartCreatureJuicer(targetCreatureSelect);
    }
    #endregion

    #region 事件
    /// <summary>
    /// 目标魔物选择(单选:再次点击已选魔物则取消选择)
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        //再次点击已选目标:取消选择;否则切换为新目标
        if (targetCreatureSelect != null && targetCreatureSelect == selectCreatureData)
        {
            targetCreatureSelect = null;
        }
        else
        {
            targetCreatureSelect = selectCreatureData;
        }
        ui_UIViewCreatureCardList_Target.RefreshAllCard();
    }
    #endregion
}
