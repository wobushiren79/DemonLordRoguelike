using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就卡片视图(网格5列), 一张卡 = 一个可升级成就(单行多级)。
/// 卡片始终展示"当前激活等级"(= 已领取等级数, 下一个待领取等级)的目标/进度/奖励:
///   未达成(灰) / 可领取(亮+可点) / 已完成(全部等级都已领取)。
/// 必须逐级领取(等级门控由 AchievementHandler.GetCurrentLevelState 天然实现, 无法跳级)。
/// </summary>
public partial class UIViewAchievementCard : BaseUIView
{
    /// <summary>
    /// 成就配置(单行多级)
    /// </summary>
    public AchievementInfoBean info;

    /// <summary>
    /// 当前激活等级的0基索引(= 已领取等级数); 等于等级总数时表示整族完成
    /// </summary>
    private int currentLevelIndex;

    /// <summary>
    /// 用于展示的等级索引: 进行中=当前激活等级; 已完成=最高等级
    /// </summary>
    private int displayLevelIndex;

    /// <summary>
    /// 等级总数
    /// </summary>
    private int levelCount;

    /// <summary>
    /// 是否已完成(所有等级都已领取)
    /// </summary>
    private bool isCompleted;

    /// <summary>
    /// 领取回调(参数为该成就)
    /// </summary>
    private Action<AchievementInfoBean> actionForUnlock;

    /// <summary>
    /// 领取奖励时的卡片动画(避免领取后直接刷新列表显得生硬)
    /// </summary>
    private Sequence animForUnlock;

    /// <summary>
    /// ui_Content 的初始锚点位置, 用于动画结束/清理时精确复位(不假设其为零点)
    /// </summary>
    private Vector2 _contentOriginPos;

    /// <summary>
    /// 是否已记录过 ui_Content 的初始锚点位置
    /// </summary>
    private bool _contentOriginCaptured;

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
    /// 设置数据(传入成就配置)
    /// </summary>
    /// <param name="info">成就配置(单行多级)</param>
    /// <param name="actionForUnlock">领取回调</param>
    public void SetData(AchievementInfoBean info, Action<AchievementInfoBean> actionForUnlock)
    {
        //单元格被网格复用时先清理上一次的领取动画, 复位缩放
        ClearAnim();
        this.info = info;
        this.actionForUnlock = actionForUnlock;
        RefreshShow();
    }

    /// <summary>
    /// 刷新显示: 解析当前激活等级, 再按"进行中/已完成"刷新各UI
    /// </summary>
    public void RefreshShow()
    {
        if (info == null) return;

        //解析当前激活等级(= 已领取等级数)与完成状态
        levelCount = info.GetLevelCount();
        currentLevelIndex = AchievementHandler.Instance.GetClaimedLevelCount(info);
        isCompleted = currentLevelIndex >= levelCount;
        //展示用等级: 进行中=当前激活等级; 已完成=最高等级
        displayLevelIndex = isCompleted ? levelCount - 1 : currentLevelIndex;
        if (displayLevelIndex < 0) displayLevelIndex = 0;

        //图标
        if (ui_Icon != null && !info.icon_res.IsNull())
        {
            IconHandler.Instance.SetUIIcon(info.icon_res, ui_Icon);
        }

        //详情气泡（鼠标悬停/点击展示当前等级的成就描述文本）
        RefreshPopup();

        //状态显示
        RefreshStateUI();
    }

    /// <summary>
    /// 刷新详情气泡：把当前展示等级的描述文本注入文本气泡按钮，悬停/点击时以文本Popup展示详情
    /// </summary>
    private void RefreshPopup()
    {
        if (ui_UIViewAchievementCard_PopupButtonCommonView == null) return;
        ui_UIViewAchievementCard_PopupButtonCommonView.SetData(info.GetLevelDescription(displayLevelIndex), PopupEnum.Text);
    }

    /// <summary>
    /// 刷新状态显示。
    /// 已完成: 关闭蒙版/锁/奖励, 进度区显示"已完成"。
    /// 进行中: 显示蒙版/锁/当前等级奖励, 进度区显示 "Lv.当前/总  当前进度/目标"(可领取时绿色)。
    /// </summary>
    private void RefreshStateUI()
    {
        //当前激活等级状态(已完成时无激活等级)
        AchievementStateEnum state = isCompleted
            ? AchievementStateEnum.Unlocked
            : AchievementHandler.Instance.GetCurrentLevelState(info);
        bool reached = state == AchievementStateEnum.Reached;

        //进度容器: 始终显示(已完成时显示"已完成")
        if (ui_Progress != null)
        {
            ui_Progress.gameObject.SetActive(true);
        }

        //进度文本与颜色
        if (ui_TxtProgress != null)
        {
            if (isCompleted)
            {
                //整族完成: 显示"已完成"
                ui_TxtProgress.text = TextHandler.Instance.GetTextById(4000017);
                ui_TxtProgress.color = ProgressColorReached;
            }
            else
            {
                //进行中: "Lv.当前/总  当前进度/目标"
                long target = info.GetLevelTargetValue(currentLevelIndex);
                long progress = AchievementHandler.Instance.GetAchievementProgress(info);
                if (progress > target) progress = target;
                string levelText = string.Format(TextHandler.Instance.GetTextById(4000016), currentLevelIndex + 1, levelCount);
                string countText = string.Format(TextHandler.Instance.GetTextById(4000006), info.FormatValueByType(progress), info.FormatValueByType(target));
                ui_TxtProgress.text = levelText + "  " + countText;
                ui_TxtProgress.color = reached ? ProgressColorReached : ProgressColorDefault;
            }
        }

        //置灰蒙版: 已完成关闭, 进行中打开
        if (ui_UIViewAchievementCard_MaskUIView != null)
        {
            if (isCompleted)
            {
                ui_UIViewAchievementCard_MaskUIView.HideMask();
            }
            else
            {
                ui_UIViewAchievementCard_MaskUIView.ShowMask();
            }
        }

        //锁图标: 进行中显示, 已完成隐藏
        if (ui_Lock != null)
        {
            ui_Lock.gameObject.SetActive(!isCompleted);
        }

        //奖励图标: 进行中显示当前等级奖励, 已完成隐藏
        if (ui_Reward != null)
        {
            ui_Reward.gameObject.SetActive(!isCompleted);
        }

        //奖励数量: 进行中显示当前等级魔晶奖励, 已完成隐藏
        if (ui_RewardNum != null)
        {
            ui_RewardNum.gameObject.SetActive(!isCompleted);
            if (!isCompleted)
            {
                ui_RewardNum.text = info.GetLevelReward(currentLevelIndex).ToString();
            }
        }
    }


    public void OnClickForUnlock()
    {
        //整族已完成时不可领取
        if (isCompleted) return;
        //仅当前激活等级处于"达成未领取"时可领奖(实时判定, 已含等级门控)
        if (AchievementHandler.Instance.GetCurrentLevelState(info) != AchievementStateEnum.Reached) return;
        //先播放领取动画, 动画结束后再真正发奖(领取当前激活等级)并刷新列表
        var target = info;
        AnimForUnlock(() => actionForUnlock?.Invoke(target));
    }

    /// <summary>
    /// 播放领取奖励动画: 卡片内容弹跳+轻微抖动, 期间锁屏防止重复点击, 结束后回调真正发奖刷新
    /// 注意: 只动 ui_Content(卡片内容根), 不动 cell 自身 transform, 避免干扰 ScrollGrid 的布局定位
    /// </summary>
    private void AnimForUnlock(Action onComplete)
    {
        //没有内容根时直接发奖, 不做动画
        if (ui_Content == null)
        {
            onComplete?.Invoke();
            return;
        }
        //记录内容根初始锚点, 供动画结束/清理时精确复位
        if (!_contentOriginCaptured)
        {
            _contentOriginPos = ui_Content.anchoredPosition;
            _contentOriginCaptured = true;
        }
        ClearAnim();
        UIHandler.Instance.ShowScreenLock();
        animForUnlock = DOTween.Sequence();
        //弹一下放大再回弹, 表现"领取成功"
        animForUnlock.Append(ui_Content.DOScale(Vector3.one * 1.18f, 0.12f).SetEase(Ease.OutBack));
        animForUnlock.Join(ui_Content.DOShakeAnchorPos(0.2f, 8f, 20));
        animForUnlock.Append(ui_Content.DOScale(Vector3.one, 0.1f).SetEase(Ease.InBack));
        animForUnlock.OnComplete(() =>
        {
            UIHandler.Instance.HideScreenLock();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 清理领取动画并复位卡片内容的缩放/位置
    /// </summary>
    private void ClearAnim()
    {
        if (animForUnlock != null)
        {
            animForUnlock.Kill();
            animForUnlock = null;
        }
        if (ui_Content != null)
        {
            ui_Content.localScale = Vector3.one;
            if (_contentOriginCaptured)
            {
                ui_Content.anchoredPosition = _contentOriginPos;
            }
        }
    }
}
