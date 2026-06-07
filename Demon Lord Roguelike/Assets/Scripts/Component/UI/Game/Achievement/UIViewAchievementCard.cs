using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就卡片视图(网格5列)
/// 三种状态: 未达成(灰) / 可领取(亮+可点) / 已解锁(已领取)
/// </summary>
public partial class UIViewAchievementCard : BaseUIView
{
    public AchievementInfoBean info;
    private Action<AchievementInfoBean> actionForUnlock;

    /// <summary>
    /// 已完成未领取时进度文本颜色（柔和绿，不刺眼）
    /// </summary>
    private static readonly Color ProgressColorReached = new Color(0.48f, 0.77f, 0.50f, 1f);

    /// <summary>
    /// 未达成时进度文本默认色
    /// </summary>
    private static readonly Color ProgressColorDefault = Color.white;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewAchievementCard_Button)
        {
            OnClickForUnlock();
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(AchievementInfoBean info, Action<AchievementInfoBean> actionForUnlock)
    {
        this.info = info;
        this.actionForUnlock = actionForUnlock;
        RefreshShow();
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    public void RefreshShow()
    {
        if (info == null) return;

        //图标
        if (ui_Icon != null && !info.icon_res.IsNull())
        {
            IconHandler.Instance.SetUIIcon(info.icon_res, ui_Icon);
        }

        //详情气泡（鼠标悬停/点击展示成就描述文本）
        RefreshPopup();

        //状态(实时计算: 已领取读存档, 其余按统计数据判定)
        var state = AchievementHandler.Instance.GetAchievementState(info);
        RefreshStateUI(state);
    }

    /// <summary>
    /// 刷新详情气泡：把成就描述文本注入文本气泡按钮，悬停/点击时以文本Popup展示详情
    /// </summary>
    private void RefreshPopup()
    {
        if (ui_UIViewAchievementCard_PopupButtonCommonView == null) return;
        ui_UIViewAchievementCard_PopupButtonCommonView.SetData(info.description_language, PopupEnum.Text);
    }

    /// <summary>
    /// 刷新状态显示
    /// </summary>
    private void RefreshStateUI(AchievementStateEnum state)
    {
        bool unlocked = state == AchievementStateEnum.Unlocked;
        bool reached = state == AchievementStateEnum.Reached;

        //进度容器：已领取隐藏，未达成/可领取显示
        if (ui_Progress != null)
        {
            ui_Progress.gameObject.SetActive(!unlocked);
        }

        //进度文本与颜色
        if (!unlocked && ui_TxtProgress != null)
        {
            long progress = AchievementHandler.Instance.GetAchievementProgress(info);
            long target = info.target_value;
            if (progress > target) progress = target;
            ui_TxtProgress.text = string.Format(TextHandler.Instance.GetTextById(4000006), FormatProgressValue(progress), FormatProgressValue(target));
            ui_TxtProgress.color = reached ? ProgressColorReached : ProgressColorDefault;
        }

        //置灰蒙版：已领取关闭，未达成/可领取打开
        if (ui_UIViewAchievementCard_MaskUIView != null)
        {
            if (unlocked)
            {
                ui_UIViewAchievementCard_MaskUIView.HideMask();
            }
            else
            {
                ui_UIViewAchievementCard_MaskUIView.ShowMask();
            }
        }

        //锁图标：未领取时显示，已领取隐藏
        if (ui_Lock != null)
        {
            ui_Lock.gameObject.SetActive(!unlocked);
        }

        //奖励图标：未领取时显示该成就可领取的奖励，已领取隐藏
        if (ui_Reward != null)
        {
            ui_Reward.gameObject.SetActive(!unlocked);
        }

        //奖励数量：未领取时显示该成就的魔晶奖励数量，已领取隐藏
        if (ui_RewardNum != null)
        {
            ui_RewardNum.gameObject.SetActive(!unlocked);
            if (!unlocked)
            {
                ui_RewardNum.text = info.reward_crystal.ToString();
            }
        }
    }

    /// <summary>
    /// 格式化进度数值：游玩/通关时间(PlayTime)成就把秒转换为小时显示(如 1/2)，其余类型按原值显示
    /// </summary>
    private string FormatProgressValue(long value)
    {
        if (info != null && info.GetAchievementType() == AchievementTypeEnum.PlayTime)
        {
            //秒 -> 小时，保留至多一位小数(整点小时不显示小数)
            const float secondsPerHour = 3600f;
            return (value / secondsPerHour).ToString("0.#");
        }
        return value.ToString();
    }

    public void OnClickForUnlock()
    {
        if (info == null) return;
        //仅"达成未领取"可领奖(实时判定)
        if (AchievementHandler.Instance.GetAchievementState(info) != AchievementStateEnum.Reached) return;
        actionForUnlock?.Invoke(info);
    }
}
