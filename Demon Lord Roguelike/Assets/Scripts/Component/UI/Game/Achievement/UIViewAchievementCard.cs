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

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        // if (viewButton == ui_BtnUnlock)
        // {
        //     OnClickForUnlock();
        // }
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
        // if (info == null) return;
        // //名字
        // if (ui_TxtName != null) ui_TxtName.text = info.name_language;
        // //描述
        // if (ui_TxtDescription != null) ui_TxtDescription.text = info.description_language;
        // //图标
        // if (ui_Icon != null)
        // {
        //     if (!info.icon_res.IsNull())
        //         IconHandler.Instance.SetUIIcon(info.icon_res, ui_Icon);
        // }
        // //奖励
        // if (ui_TxtReward != null) ui_TxtReward.text = "x" + info.reward_crystal;

        //进度
        long progress = AchievementHandler.Instance.GetAchievementProgress(info);
        long target = info.target_value;
        if (progress > target) progress = target;
        if (ui_TxtProgress != null)
        {
            ui_TxtProgress.text = string.Format(TextHandler.Instance.GetTextById(4000006), progress, target);
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
        bool canUnlock = state == AchievementStateEnum.Reached;
        bool unlocked = state == AchievementStateEnum.Unlocked;

        // //按钮交互性
        // if (ui_BtnUnlock != null)
        // {
        //     ui_BtnUnlock.interactable = canUnlock;
        // }
        // //图标遮罩(未达成/已领取均可显示灰色蒙版以做区分)
        // if (ui_IconMask != null)
        // {
        //     ui_IconMask.gameObject.SetActive(state == AchievementStateEnum.NotReached);
        // }
        // //奖励显示(已解锁不再显示)
        // if (ui_RewardRoot != null)
        // {
        //     ui_RewardRoot.SetActive(!unlocked);
        // }
        // //文本状态
        // if (ui_TxtState != null)
        // {
        //     switch (state)
        //     {
        //         case AchievementStateEnum.NotReached:
        //             ui_TxtState.text = TextHandler.Instance.GetTextById(4000003);
        //             break;
        //         case AchievementStateEnum.Reached:
        //             ui_TxtState.text = TextHandler.Instance.GetTextById(4000004);
        //             break;
        //         case AchievementStateEnum.Unlocked:
        //             ui_TxtState.text = TextHandler.Instance.GetTextById(4000005);
        //             break;
        //     }
        // }
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
