using System;
using System.Collections.Generic;
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
    /// 动态实例化出的等级格子(按等级总数, 各对应一级)。
    /// 用列表持有并复用, 避免单元格被网格(ScrollGrid)池化复用时反复实例化产生冗余格子。
    /// </summary>
    private readonly List<RectTransform> listLevelItem = new List<RectTransform>();

    /// <summary>
    /// 领取回调(参数为该成就)
    /// </summary>
    private Action<AchievementInfoBean> actionForUnlock;

    /// <summary>
    /// 领取奖励时的等级图标动画(避免领取后直接刷新列表显得生硬)
    /// </summary>
    private Sequence animForUnlock;

    /// <summary>
    /// 领取动画中 LevelIcon "砸下"的起始放大倍数(突然以放大态出现, 再从大到小砸向格子)
    /// </summary>
    private const float IconSlamStartScale = 3f;

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
    /// 已完成: 关闭蒙版/奖励, 进度区显示"已完成"。
    /// 进行中: 显示蒙版/当前等级奖励, 进度区显示"当前进度/目标"(可领取时绿色), 并按等级总数刷新等级格子。
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

        //等级格子: 按等级总数动态实例化 LevelItem, 已领取(完成)的等级显示其 LevelIcon
        RefreshLevelItems();

        //进度文本与颜色(只显示"当前进度/目标", 等级改用 LevelItem 图标格子展示)
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
                //进行中: "当前进度/目标"
                long target = info.GetLevelTargetValue(currentLevelIndex);
                long progress = AchievementHandler.Instance.GetAchievementProgress(info);
                if (progress > target) progress = target;
                string countText = string.Format(TextHandler.Instance.GetTextById(4000006), info.FormatValueByType(progress), info.FormatValueByType(target));
                ui_TxtProgress.text = countText;
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


    /// <summary>
    /// 刷新等级格子: 按等级总数(levelCount)动态实例化/复用 LevelItem(模板 ui_LevelItem, 父节点 ui_Level)。
    /// 已领取(完成)的等级(索引 &lt; currentLevelIndex)显示其子节点 LevelIcon, 否则隐藏。
    /// 复用 listLevelItem 持有的实例(只补足/隐藏多余), 避免单元格被网格池化复用时重复实例化。
    /// </summary>
    private void RefreshLevelItems()
    {
        if (ui_Level == null || ui_LevelItem == null) return;

        //按等级总数补足/复用格子; 模板 ui_LevelItem 本身始终保持隐藏, 不计入
        for (int i = 0; i < levelCount; i++)
        {
            RectTransform itemRect;
            if (i < listLevelItem.Count)
            {
                itemRect = listLevelItem[i];
            }
            else
            {
                GameObject newObj = Instantiate(ui_LevelItem.gameObject, ui_Level);
                itemRect = newObj.GetComponent<RectTransform>();
                listLevelItem.Add(itemRect);
            }
            itemRect.gameObject.SetActive(true);
            //复位格子缩放(领取动画可能中途被打断, 保证池化复用时干净)
            itemRect.localScale = Vector3.one;

            //已领取(完成)的等级显示对勾图标 LevelIcon, 未完成隐藏
            Transform levelIcon = itemRect.Find("LevelIcon");
            if (levelIcon != null)
            {
                levelIcon.localScale = Vector3.one;
                levelIcon.gameObject.SetActive(i < currentLevelIndex);
            }
        }

        //隐藏多余格子(本次等级数比上一次少时)
        for (int i = levelCount; i < listLevelItem.Count; i++)
        {
            listLevelItem[i].gameObject.SetActive(false);
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
    /// 播放领取奖励动画: 待领取等级的 LevelIcon 突然以放大态出现, 从大到小"砸"向对应 LevelItem,
    /// 砸到位后 LevelItem 抖动一下; 期间锁屏防止重复点击, 结束后回调真正发奖刷新。
    /// 注意: 只动目标等级格子(LevelIcon/LevelItem), 不动 cell 自身 transform, 避免干扰 ScrollGrid 的布局定位
    /// </summary>
    private void AnimForUnlock(Action onComplete)
    {
        //取当前待领取等级(currentLevelIndex)对应的格子与其 LevelIcon; 缺失则直接发奖不做动画
        RectTransform targetItem = (currentLevelIndex >= 0 && currentLevelIndex < listLevelItem.Count)
            ? listLevelItem[currentLevelIndex]
            : null;
        Transform levelIcon = targetItem == null ? null : targetItem.Find("LevelIcon");
        if (targetItem == null || levelIcon == null)
        {
            onComplete?.Invoke();
            return;
        }

        ClearAnim();
        UIHandler.Instance.ShowScreenLock();

        //突然出现: 立即以放大态显示 LevelIcon, 同时复位格子缩放
        targetItem.localScale = Vector3.one;
        levelIcon.localScale = Vector3.one * IconSlamStartScale;
        levelIcon.gameObject.SetActive(true);

        animForUnlock = DOTween.Sequence();
        //砸落(往下压)起始瞬间播放上锁音效(与研究解锁下压同一音效)
        animForUnlock.AppendCallback(() => AudioHandler.Instance.PlaySound(AudioEnum.sound_lock_5));
        //从大到小"砸"向格子(InBack 收尾带轻微过冲, 强化砸落感)
        animForUnlock.Append(levelIcon.DOScale(Vector3.one, 0.22f).SetEase(Ease.InBack));
        //砸到位后 LevelItem 抖动一下
        animForUnlock.Append(targetItem.DOShakeScale(0.3f, 0.45f, 12, 90, true));
        animForUnlock.OnComplete(() =>
        {
            UIHandler.Instance.HideScreenLock();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 清理领取动画: Kill 正在播放的序列(格子缩放由 RefreshLevelItems 每次刷新统一复位)
    /// </summary>
    private void ClearAnim()
    {
        if (animForUnlock != null)
        {
            animForUnlock.Kill();
            animForUnlock = null;
        }
    }
}
