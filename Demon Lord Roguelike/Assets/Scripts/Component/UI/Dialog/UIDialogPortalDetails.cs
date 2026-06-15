

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public partial class UIDialogPortalDetails : DialogView
{
    #region 数据
    protected GameWorldInfoBean gameWorldInfo;
    protected GameWorldInfoRandomBean gameWorldInfoRandom;

    //难度item的水平间距(上一个/当前/下一个 分别位于 -itemSpacing/0/+itemSpacing)
    protected const float itemSpacing = 500f;
    //难度切换左右滑动动画时长
    protected const float animDuration = 0.6f;
    //难度item对象池(3个常驻显示 + 切换滑动时第4个临时进出)
    protected List<UIViewDialogPortalDetailsItem> listItemPool;
    //用户可挑战(已解锁)的最高难度
    protected int unlockDifficultyMax;
    //该世界配置表中存在的最高难度(用于决定是否展示未解锁的"下一个"预览item)
    protected int configDifficultyMax;
    //当前是否正在播放切换动画(动画期间禁止再次切换)
    protected bool isAnimating;
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(GameWorldInfoBean gameWorldInfo, GameWorldInfoRandomBean gameWorldInfoRandom)
    {
        this.gameWorldInfo = gameWorldInfo;
        this.gameWorldInfoRandom = gameWorldInfoRandom;

        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //用户可选择的最高难度
        unlockDifficultyMax = userUnlock.GetUnlockGameWorldConquerDifficultyLevel(gameWorldInfoRandom.worldId);
        //该世界配置存在的最高难度
        configDifficultyMax = FightTypeConquerInfoCfg.GetMaxLevel(gameWorldInfoRandom.worldId);
        //把默认难度约束在 [1, 已解锁最高] 范围内
        gameWorldInfoRandom.difficultyLevel = Mathf.Clamp(gameWorldInfoRandom.difficultyLevel, 1, Mathf.Max(1, unlockDifficultyMax));

        InitItemPool();
        RefreshItemsImmediate(gameWorldInfoRandom.difficultyLevel);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_DialogContent);
    }

    /// <summary>
    /// 初始化难度item对象池(以模板item为第一个, 再克隆出共4个)
    /// </summary>
    protected void InitItemPool()
    {
        if (listItemPool != null)
            return;
        listItemPool = new List<UIViewDialogPortalDetailsItem>();
        //模板item作为对象池第一个
        listItemPool.Add(ui_UIViewDialogPortalDetailsItem);
        Transform itemParent = ui_UIViewDialogPortalDetailsItem.transform.parent;
        //再克隆3个(滑动时最多同时存在4个item)
        for (int i = 1; i < 4; i++)
        {
            GameObject objItem = Instantiate(ui_UIViewDialogPortalDetailsItem.gameObject, itemParent);
            objItem.transform.localScale = Vector3.one;
            UIViewDialogPortalDetailsItem itemView = objItem.GetComponent<UIViewDialogPortalDetailsItem>();
            listItemPool.Add(itemView);
        }
    }

    /// <summary>
    /// 立即按标准布局刷新3个item(上一个 -itemSpacing / 当前 0 / 下一个 +itemSpacing), 越界的难度不显示
    /// </summary>
    /// <param name="centerDifficulty">中间(当前选中)的难度</param>
    protected void RefreshItemsImmediate(int centerDifficulty)
    {
        HideAllItems();
        int poolIndex = 0;
        for (int offset = -1; offset <= 1; offset++)
        {
            int difficulty = centerDifficulty + offset;
            if (!IsValidDisplayDifficulty(difficulty))
                continue;
            UIViewDialogPortalDetailsItem itemView = listItemPool[poolIndex++];
            ShowItem(itemView, difficulty, offset * itemSpacing);
            //当前难度透明度为1, 其余(上一个/下一个)为0.5
            itemView.SetAlpha(GetItemAlpha(difficulty, centerDifficulty));
            //当前难度缩放为1, 其余(上一个/下一个)为0.8
            itemView.SetScale(GetItemScale(difficulty, centerDifficulty));
        }
    }

    /// <summary>
    /// 播放难度切换的左右滑动动画(整体平移, 出界item滑出, 新item从外侧滑入)
    /// </summary>
    /// <param name="oldCenter">切换前的当前难度</param>
    /// <param name="newCenter">切换后的当前难度</param>
    protected void AnimSwitchDifficulty(int oldCenter, int newCenter)
    {
        isAnimating = true;
        HideAllItems();

        //覆盖切换前后所有需要参与动画的难度(两侧各扩一个), 共最多4个
        int difficultyFrom = Mathf.Min(oldCenter, newCenter) - 1;
        int difficultyTo = Mathf.Max(oldCenter, newCenter) + 1;
        int poolIndex = 0;
        for (int difficulty = difficultyFrom; difficulty <= difficultyTo; difficulty++)
        {
            if (!IsValidDisplayDifficulty(difficulty))
                continue;
            if (poolIndex >= listItemPool.Count)
                break;
            UIViewDialogPortalDetailsItem itemView = listItemPool[poolIndex++];
            //起点按"切换前"中心计算, 终点按"切换后"中心计算
            float fromX = (difficulty - oldCenter) * itemSpacing;
            float toX = (difficulty - newCenter) * itemSpacing;
            ShowItem(itemView, difficulty, fromX);
            //起点透明度/缩放按"切换前"中心, 终点按"切换后"中心渐变;
            //滑出可视区的item渐变到0、滑入的item从0渐入, 避免临时第4个item突兀地出现/消失
            itemView.SetAlpha(GetItemAlpha(difficulty, oldCenter));
            itemView.SetScale(GetItemScale(difficulty, oldCenter));
            itemView.KillAnim();
            itemView.rectTransform.DOAnchorPosX(toX, animDuration).SetEase(Ease.OutBack);
            itemView.DoFadeAlpha(GetItemAlpha(difficulty, newCenter), animDuration);
            //切到中间的item放大到1, 滑到两侧的item缩小到0.8
            itemView.DoScale(GetItemScale(difficulty, newCenter), animDuration);
        }

        //动画结束后回到标准3item布局(顺带回收多余item)
        DOVirtual.DelayedCall(animDuration, () =>
        {
            if (this == null || listItemPool == null)
                return;
            isAnimating = false;
            RefreshItemsImmediate(newCenter);
        });
    }

    /// <summary>
    /// 配置并显示一个item到指定位置
    /// </summary>
    /// <param name="itemView">目标item</param>
    /// <param name="difficulty">该item代表的难度</param>
    /// <param name="posX">水平位置</param>
    protected void ShowItem(UIViewDialogPortalDetailsItem itemView, int difficulty, float posX)
    {
        bool isUnlock = difficulty <= unlockDifficultyMax;
        Color bgColor = GetDifficultyBGColor(difficulty);
        itemView.gameObject.SetActive(true);
        itemView.SetData(gameWorldInfo, gameWorldInfoRandom, difficulty, isUnlock, bgColor);
        itemView.rectTransform.anchoredPosition = new Vector2(posX, 0);
    }

    /// <summary>
    /// 获取指定难度的背景色(从征服难度表 bg_color 读取, 无配置返回白色)
    /// </summary>
    /// <param name="difficulty">难度等级</param>
    protected Color GetDifficultyBGColor(int difficulty)
    {
        var conquerInfo = FightTypeConquerInfoCfg.GetItemData(gameWorldInfoRandom.worldId, difficulty);
        if (conquerInfo == null)
            return Color.white;
        return conquerInfo.GetBGColor();
    }

    /// <summary>
    /// 隐藏对象池中所有item
    /// </summary>
    protected void HideAllItems()
    {
        if (listItemPool == null)
            return;
        for (int i = 0; i < listItemPool.Count; i++)
        {
            listItemPool[i].KillAnim();
            listItemPool[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 判断一个难度是否可作为item展示(在 [1, 展示上限] 内, 展示上限取已解锁与配置最高的较大值)
    /// </summary>
    /// <param name="difficulty">难度等级</param>
    protected bool IsValidDisplayDifficulty(int difficulty)
    {
        int displayMax = Mathf.Max(unlockDifficultyMax, configDifficultyMax);
        return difficulty >= 1 && difficulty <= displayMax;
    }

    /// <summary>
    /// 计算某难度在以 center 为中心布局时应有的透明度
    /// 仅展示3个(上一个/当前/下一个); 超出可视区(|offset|>1)的临时进出item透明度为0, 当前为1, 两侧为0.5
    /// </summary>
    /// <param name="difficulty">item代表的难度</param>
    /// <param name="center">布局中心难度</param>
    protected float GetItemAlpha(int difficulty, int center)
    {
        if (Mathf.Abs(difficulty - center) > 1)
            return 0f;
        return difficulty == center ? UIViewDialogPortalDetailsItem.alphaCurrent : UIViewDialogPortalDetailsItem.alphaOther;
    }

    /// <summary>
    /// 计算某难度在以 center 为中心布局时应有的缩放(当前难度1, 其余两侧/进出item为0.8)
    /// </summary>
    /// <param name="difficulty">item代表的难度</param>
    /// <param name="center">布局中心难度</param>
    protected float GetItemScale(int difficulty, int center)
    {
        return difficulty == center ? UIViewDialogPortalDetailsItem.scaleCurrent : UIViewDialogPortalDetailsItem.scaleOther;
    }

    /// <summary>
    /// 无法继续切换难度时的回弹动画(朝目标方向试探滑动 1/3 间距后弹回原位)
    /// </summary>
    /// <param name="changeLevel">尝试的切换方向(-1 上一个 / +1 下一个)</param>
    protected void AnimSwitchBlocked(int changeLevel)
    {
        isAnimating = true;
        //点击右(难度+1)时内容向左试探, 故位移取反方向
        float offsetX = -changeLevel * itemSpacing / 3f;
        float halfDuration = animDuration / 2f;
        for (int i = 0; i < listItemPool.Count; i++)
        {
            UIViewDialogPortalDetailsItem itemView = listItemPool[i];
            if (!itemView.gameObject.activeSelf)
                continue;
            var rt = itemView.rectTransform;
            float originX = rt.anchoredPosition.x;
            rt.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOAnchorPosX(originX + offsetX, halfDuration).SetEase(Ease.OutQuad));
            seq.Append(rt.DOAnchorPosX(originX, halfDuration).SetEase(Ease.OutQuad));
        }
        DOVirtual.DelayedCall(animDuration, () =>
        {
            if (this == null)
                return;
            isAnimating = false;
        });
    }
    #endregion

    #region 按钮点击
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_DifficultySelectLeftBtn)
        {
            OnClickForChangeDifficultyLevel(-1);
        }
        else if (viewButton == ui_DifficultySelectRightBtn)
        {
            OnClickForChangeDifficultyLevel(1);
        }
    }

    /// <summary>
    /// 点击改变难度(带左右滑动动画, 到最高/最低难度时不再切换)
    /// </summary>
    /// <param name="changeLevel">难度变化量(-1 上一个 / +1 下一个)</param>
    public void OnClickForChangeDifficultyLevel(int changeLevel)
    {
        if (isAnimating)
            return;
        int oldDifficulty = gameWorldInfoRandom.difficultyLevel;
        //难度限制在 [1, 已解锁最高]
        int newDifficulty = Mathf.Clamp(oldDifficulty + changeLevel, 1, Mathf.Max(1, unlockDifficultyMax));
        if (newDifficulty == oldDifficulty)
        {
            //已到边界无法切换: 播放回弹动画
            AnimSwitchBlocked(changeLevel);
            //向更高难度切换且存在更高的未解锁难度时, 提示"难度未解锁"
            if (changeLevel > 0 && configDifficultyMax > unlockDifficultyMax)
            {
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(404), 0);
            }
            return;
        }
        gameWorldInfoRandom.difficultyLevel = newDifficulty;
        AnimSwitchDifficulty(oldDifficulty, newDifficulty);
    }
    #endregion
}
