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

        //状态
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var achievementData = userData.GetUserAchievementData();
        var state = achievementData.GetAchievementState(info.id);
        RefreshStateUI(state);
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
            ui_TxtProgress.text = string.Format(TextHandler.Instance.GetTextById(4000006), progress, target);
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
    }

    public void OnClickForUnlock()
    {
        if (info == null) return;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var state = userData.GetUserAchievementData().GetAchievementState(info.id);
        if (state != AchievementStateEnum.Reached) return;
        actionForUnlock?.Invoke(info);
    }
}
