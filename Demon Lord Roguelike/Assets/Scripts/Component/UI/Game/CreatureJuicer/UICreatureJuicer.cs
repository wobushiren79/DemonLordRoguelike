using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.UI;

public partial class UICreatureJuicer : BaseUIComponent
{
    //退出回调(由打开入口注入:场景E键交互返回UIBaseMain)
    public Action actionForExit;
    //当前展示的可榨汁魔物列表
    public List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //当前已选中要投入榨汁的魔物(多选,上限由研究门控)
    public List<CreatureBean> listSelectCreature = new List<CreatureBean>();
    //魔汁机摄像头(CV_Juicer,打开时切换)
    public CinemachineCamera juicerCamera;

    #region 生命周期
    public override void OpenUI()
    {
        base.OpenUI();
        //关闭基地移动控制(与其它基地子界面一致):避免榨汁界面期间仍能控制角色移动
        GameControlHandler.Instance.SetBaseControl(false);
        //切换魔汁机摄像头 + 关闭远景虚化(对准魔汁机建筑)
        juicerCamera = CameraHandler.Instance.SetJuicerCamera(int.MaxValue, true);
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        InitCreatureData();
        RefreshUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewCreatureCardList_Target.CloseUI();
        //恢复远景虚化(基地镜头由返回 UIBaseMain 时统一还原)
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
    }

    /// <summary>
    /// 刷新UI:重刷目标魔物列表卡片(选中态由 OnCellChangeForTarget 逐卡回填) + 计数文本
    /// </summary>
    public void RefreshUI()
    {
        ui_UIViewCreatureCardList_Target.RefreshAllCard();
        RefreshLimitText();
    }
    #endregion

    #region 数据
    /// <summary>
    /// 初始化可榨汁魔物列表:取背包内空闲(未上阵、未被其它流程占用)的魔物,默认按等级降序排序。
    /// 复用进阶目标列表的卡片使用态(CreatureAscendTarget),与预制体已挂的卡片变体(UIViewCreatureCardItemForCreatureAscend)匹配。
    /// </summary>
    public void InitCreatureData()
    {
        listSelectCreature.Clear();
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
        //默认排序:等级降序(高→低)
        listCreatureData.Sort((a, b) => b.level.CompareTo(a.level));
        ui_UIViewCreatureCardList_Target.SetData(listCreatureData, CardUseStateEnum.CreatureAscendTarget, OnCellChangeForTarget);
    }

    /// <summary>
    /// 目标魔物卡片刷新:按是否已被选中(在投入列表内)设置选中/未选中样式
    /// </summary>
    public void OnCellChangeForTarget(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (listSelectCreature.Contains(itemData))
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }

    /// <summary>
    /// 获取当前可投入魔物上限(基础5 + 投入上限研究等级 JuicerNum,满级15)。
    /// </summary>
    /// <returns>可投入选择的最大魔物数量</returns>
    protected int GetJuicerMax()
    {
        return GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockJuicerCreatureMax();
    }

    /// <summary>
    /// 刷新投入计数文本:格式「已选/上限」,达上限时数量转通用警示红(ColorUtil.WrapLimitFull)。
    /// </summary>
    public void RefreshLimitText()
    {
        if (ui_LimmitText == null)
            return;
        int juicerMax = GetJuicerMax();
        int selectCount = listSelectCreature.Count;
        ui_LimmitText.text = ColorUtil.WrapLimitFull($"{selectCount}/{juicerMax}", selectCount >= juicerMax);
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
    /// 点击开始榨汁:校验至少投入一只魔物后交由 CreatureJuicerLogic 处理(榨汁流程/奖励后续接入)
    /// </summary>
    public void OnClickForStart()
    {
        if (listSelectCreature.Count == 0)
        {
            //未投入任何魔物:提示并拦截
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61010));
            return;
        }
        //交由逻辑层开始榨汁(当前为留桩,后续接入榨汁流程与奖励)
        GameHandler.Instance.StartCreatureJuicer(listSelectCreature);
    }
    #endregion

    #region 事件
    /// <summary>
    /// 目标魔物选择(多选:再次点击已选魔物则移出;新增时超过投入上限则提示并拦截)
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        if (listSelectCreature.Contains(selectCreatureData))
        {
            //再次点击已选魔物:移出投入列表
            listSelectCreature.Remove(selectCreatureData);
        }
        else
        {
            //投入数量达到上限(基础5+投入上限研究等级)则拒绝并提示
            int juicerMax = GetJuicerMax();
            if (listSelectCreature.Count >= juicerMax)
            {
                UIHandler.Instance.ToastHintText(string.Format(TextHandler.Instance.GetTextById(61012), juicerMax));
            }
            else
            {
                listSelectCreature.Add(selectCreatureData);
            }
        }
        ui_UIViewCreatureCardList_Target.RefreshAllCard();
        RefreshLimitText();
    }
    #endregion
}
