using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 成就UI主界面
/// 包含两个页签：成就列表(5列网格) / 统计数据(1列列表)
/// </summary>
public partial class UIAchievement : BaseUIComponent, IRadioGroupCallBack
{
    /// <summary>
    /// 页签单选按钮组(预制体上管理 RbAchievement/RbStatistic 的 RadioGroupView)
    /// </summary>
    private RadioGroupView _radioGroup;

    /// <summary>
    /// 成就列表缓存(按排序)
    /// </summary>
    private List<AchievementInfoBean> _listAchievement;

    /// <summary>
    /// 统计条目缓存
    /// </summary>
    private List<AchievementStatisticItemBean> _listStatistic;

    /// <summary>
    /// 退出回调(由各打开入口在打开前设置，决定退出时的关闭/跳转逻辑)
    /// 例: 由 UIBaseCore 打开则返回 UIBaseCore; 由场景成就石碑打开则返回 UIBaseMain。
    /// </summary>
    public Action actionForExit;

    #region 生命周期

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetAchievementCamera(int.MaxValue, true);

        //注册页签单选按钮组回调(预制体上的 RadioGroupView 会接管按钮点击，必须由本界面作为组回调接收)
        InitRadioGroup();

        //初始化Tab
        //运行期不做达成判定与实时刷新; 每次打开界面时由各卡片 GetCurrentLevelState 依据统计数据实时计算达成状态
        SwitchTab(0);
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
    /// 初始化页签单选按钮组并注册本界面为组回调
    /// 预制体上挂有 RadioGroupView 管理 RbAchievement/RbStatistic，其 Start() 会接管按钮点击，
    /// 因此必须通过 IRadioGroupCallBack 接收选中事件，而不能直接监听单个 RadioButton。
    /// </summary>
    private void InitRadioGroup()
    {
        if (_radioGroup == null && ui_RbAchievement != null)
        {
            _radioGroup = ui_RbAchievement.GetComponentInParent<RadioGroupView>(true);
        }
        if (_radioGroup != null)
        {
            _radioGroup.SetCallBack(this);
        }
    }

    /// <summary>
    /// 切换页签
    /// </summary>
    public void SwitchTab(int tabIndex)
    {
        if (ui_TabAchievement != null) ui_TabAchievement.gameObject.SetActive(tabIndex == 0);
        if (ui_TabStatistic != null) ui_TabStatistic.gameObject.SetActive(tabIndex == 1);

        //同步单选按钮组的选中态(isCallBack=false 避免回调递归触发 SwitchTab)
        if (_radioGroup != null)
        {
            _radioGroup.SetPosition(tabIndex, false);
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
    /// 回调-按钮组选中(由 RadioGroupView 派发)
    /// </summary>
    public void RadioButtonSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {
        SwitchTab(position);
    }

    /// <summary>
    /// 回调-按钮组取消选中(本界面无需处理)
    /// </summary>
    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {
    }

    #endregion

    #region 成就列表

    /// <summary>
    /// 刷新成就列表
    /// 每个可升级成就一张卡(单行多级), 由卡片内部解析当前激活等级。
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
    /// 成就单元格刷新(卡片内部解析当前激活等级)
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
    /// 点击解锁(领取当前激活等级奖励)。领取后刷新列表使卡片推进到下一级或显示"已完成"。
    /// </summary>
    /// <param name="info">要领取的成就(卡片传入)</param>
    private void OnClickForUnlockAchievement(AchievementInfoBean info)
    {
        if (info == null) return;
        bool ok = AchievementHandler.Instance.TryUnlockNextLevel(info.id);
        if (ok)
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(4000008), 1);
            //领取成功后本地刷新列表, 使该卡片推进到下一级(或全部领完后显示"已完成")
            RefreshAchievementList();
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

        //按世界×难度分别统计征服通关次数(仅展示在征服配置表中存在难度的世界)
        var allWorld = GameWorldInfoCfg.GetAllArrayData();
        if (allWorld != null)
        {
            for (int w = 0; w < allWorld.Length; w++)
            {
                var worldInfo = allWorld[w];
                int maxLevel = FightTypeConquerInfoCfg.GetMaxLevel(worldInfo.id);
                if (maxLevel <= 0)
                    continue;
                string worldName = worldInfo.name_language;
                for (int lv = 1; lv <= maxLevel; lv++)
                {
                    string difficultyLabel = string.Format(TextHandler.Instance.GetTextById(4000013), lv);
                    string conquerLabel = TextHandler.Instance.GetTextById(4000012) + " (" + worldName + " " + difficultyLabel + ")";
                    list.Add(new AchievementStatisticItemBean()
                    {
                        label = conquerLabel,
                        value = achievementData.GetConquerCompleteCount(worldInfo.id, lv).ToString(),
                    });
                }
            }
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

    #region 点击事件

    /// <summary>
    /// 点击退出: 执行由打开入口注入的退出回调(关闭/跳转逻辑由各入口自行处理)
    /// </summary>
    public void OnClickForExit()
    {
        actionForExit?.Invoke();
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
