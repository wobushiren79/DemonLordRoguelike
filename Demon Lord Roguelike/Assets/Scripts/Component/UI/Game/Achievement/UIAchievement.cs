using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 成就UI主界面
/// 包含两个页签：成就列表(5列网格) / 统计数据(1列列表)
/// </summary>
public partial class UIAchievement : BaseUIComponent, IRadioButtonCallBack
{
    /// <summary>
    /// 当前激活的 Tab(0=成就 1=统计)
    /// </summary>
    private int currentTab = 0;

    /// <summary>
    /// 成就列表缓存(按排序)
    /// </summary>
    private List<AchievementInfoBean> _listAchievement;

    /// <summary>
    /// 统计条目缓存
    /// </summary>
    private List<AchievementStatisticItemBean> _listStatistic;

    #region 生命周期

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue, true);

        //刷新所有成就达成情况(防止后台未触发事件时进度未同步)
        AchievementHandler.Instance.CheckAllAchievements();

        //初始化Tab
        SwitchTab(0);

        //注册事件监听以便实时刷新
        this.RegisterEvent<long>(EventsInfo.Achievement_StateChange, OnEventAchievementStateChange);
        this.RegisterEvent<long>(EventsInfo.Achievement_ProgressChange, OnEventAchievementProgressChange);
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    #endregion

    #region Tab 切换

    /// <summary>
    /// 切换页签
    /// </summary>
    public void SwitchTab(int tabIndex)
    {
        currentTab = tabIndex;
        if (ui_TabAchievement != null) ui_TabAchievement.SetActive(tabIndex == 0);
        if (ui_TabStatistic != null) ui_TabStatistic.SetActive(tabIndex == 1);

        if (ui_RbAchievement != null)
        {
            ui_RbAchievement.SetCallBack(null);
            ui_RbAchievement.ChangeStates(tabIndex == 0);
            ui_RbAchievement.SetCallBack(this);
        }
        if (ui_RbStatistic != null)
        {
            ui_RbStatistic.SetCallBack(null);
            ui_RbStatistic.ChangeStates(tabIndex == 1);
            ui_RbStatistic.SetCallBack(this);
        }

        if (tabIndex == 0)
        {
            RefreshAchievementList();
        }
        else
        {
            RefreshStatisticList();
        }
    }

    /// <summary>
    /// 回调-RadioButton 选择
    /// </summary>
    public void RadioButtonSelected(RadioButtonView radioButton, bool isSelect)
    {
        if (!isSelect) return;
        if (radioButton == ui_RbAchievement) SwitchTab(0);
        else if (radioButton == ui_RbStatistic) SwitchTab(1);
    }

    #endregion

    #region 成就列表

    /// <summary>
    /// 刷新成就列表
    /// </summary>
    public void RefreshAchievementList()
    {
        if (ui_ScrollAchievement == null) return;
        _listAchievement = AchievementHandler.Instance.manager.GetAllAchievementsSorted();
        ui_ScrollAchievement.ClearAllCell();
        ui_ScrollAchievement.SetCellCount(_listAchievement.Count);
        ui_ScrollAchievement.AddCellListener(OnAchievementCellUpdate);
    }

    /// <summary>
    /// 成就单元格刷新
    /// </summary>
    private void OnAchievementCellUpdate(ScrollGridCell cell)
    {
        if (_listAchievement == null) return;
        int idx = cell.index;
        if (idx < 0 || idx >= _listAchievement.Count) return;
        var info = _listAchievement[idx];
        var view = cell.GetComponent<UIViewAchievementCard>();
        if (view == null) return;
        view.SetData(info, OnClickForUnlockAchievement);
    }

    /// <summary>
    /// 点击解锁(领取奖励)
    /// </summary>
    private void OnClickForUnlockAchievement(AchievementInfoBean info)
    {
        if (info == null) return;
        bool ok = AchievementHandler.Instance.TryUnlockAchievement(info.id);
        if (ok)
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(4000008));
        }
    }

    #endregion

    #region 统计列表

    /// <summary>
    /// 构建统计条目数据
    /// </summary>
    private List<AchievementStatisticItemBean> BuildStatisticList()
    {
        List<AchievementStatisticItemBean> list = new List<AchievementStatisticItemBean>();
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var achievementData = userData.GetUserAchievementData();

        //游玩时间
        long sec = userData.gameTime;
        long h = sec / 3600;
        long m = (sec % 3600) / 60;
        list.Add(new AchievementStatisticItemBean()
        {
            label = TextHandler.Instance.GetTextById(4000010),
            value = string.Format(TextHandler.Instance.GetTextById(4000014), h, m),
        });

        //总击杀生物
        list.Add(new AchievementStatisticItemBean()
        {
            label = TextHandler.Instance.GetTextById(4000011),
            value = achievementData.GetTotalKillCount().ToString(),
        });

        //总通关次数
        list.Add(new AchievementStatisticItemBean()
        {
            label = TextHandler.Instance.GetTextById(4000015),
            value = achievementData.GetTotalConquerCompleteCount().ToString(),
        });

        //按难度分别统计征服通关次数(1~10)
        for (int lv = 1; lv <= 10; lv++)
        {
            string difficultyLabel = string.Format(TextHandler.Instance.GetTextById(4000013), lv);
            string conquerLabel = TextHandler.Instance.GetTextById(4000012) + " (" + difficultyLabel + ")";
            list.Add(new AchievementStatisticItemBean()
            {
                label = conquerLabel,
                value = achievementData.GetConquerCompleteCount(lv).ToString(),
            });
        }
        return list;
    }

    /// <summary>
    /// 刷新统计列表
    /// </summary>
    public void RefreshStatisticList()
    {
        if (ui_ScrollStatistic == null) return;
        _listStatistic = BuildStatisticList();
        ui_ScrollStatistic.ClearAllCell();
        ui_ScrollStatistic.SetCellCount(_listStatistic.Count);
        ui_ScrollStatistic.AddCellListener(OnStatisticCellUpdate);
    }

    /// <summary>
    /// 统计单元格刷新
    /// </summary>
    private void OnStatisticCellUpdate(ScrollGridCell cell)
    {
        if (_listStatistic == null) return;
        int idx = cell.index;
        if (idx < 0 || idx >= _listStatistic.Count) return;
        var view = cell.GetComponent<UIViewAchievementStatistic>();
        if (view == null) return;
        view.SetData(_listStatistic[idx]);
    }

    #endregion

    #region 事件回调

    private void OnEventAchievementStateChange(long achievementId)
    {
        if (currentTab == 0) RefreshAchievementList();
    }

    private void OnEventAchievementProgressChange(long achievementId)
    {
        if (currentTab == 0)
        {
            //轻量刷新当前显示的cell
            if (ui_ScrollAchievement != null)
            {
                ui_ScrollAchievement.RefreshAllCells();
            }
        }
    }

    #endregion

    #region 点击事件

    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    #endregion
}

/// <summary>
/// 统计条目数据
/// </summary>
public class AchievementStatisticItemBean
{
    public string label;
    public string value;
}
