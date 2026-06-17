

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
        //把默认难度约束在 [1, 已解锁最高] 范围内, 并同步该难度预生成的道路/关卡随机数据
        int defaultDifficulty = Mathf.Clamp(gameWorldInfoRandom.difficultyLevel, 1, Mathf.Max(1, unlockDifficultyMax));
        gameWorldInfoRandom.SetDifficultyLevel(defaultDifficulty);

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

        //关键: 动画结束时停在中心的item, 必须与随后 RefreshItemsImmediate(newCenter) 占据中心的是同一个对象池对象.
        //否则(典型为向右切换)中心item会在动画末尾被瞬间换成另一个正在缩小的item, 表现为"缩放最后一刻一瞬间放大".
        //因此这里按"切换后"中心的紧凑顺序(与 RefreshItemsImmediate 完全一致)把中间三档难度依次分配到 pool[0..2],
        //再把滑出可视区的那一档放到剩余槽位, 保证前后池索引一致、每个屏幕位置的item对象在刷新瞬间无缝衔接.
        int poolIndex = 0;
        for (int offset = -1; offset <= 1; offset++)
        {
            int difficulty = newCenter + offset;
            if (!IsValidDisplayDifficulty(difficulty))
                continue;
            if (poolIndex >= listItemPool.Count)
                break;
            AnimSwitchOneItem(listItemPool[poolIndex++], difficulty, oldCenter, newCenter);
        }
        //滑出可视区的临时item(切换前在可视边缘、切换后超出当前±1范围的那一档), 从外侧滑出并淡出
        int leavingDifficulty = newCenter > oldCenter ? oldCenter - 1 : oldCenter + 1;
        if (IsValidDisplayDifficulty(leavingDifficulty) && poolIndex < listItemPool.Count)
        {
            AnimSwitchOneItem(listItemPool[poolIndex++], leavingDifficulty, oldCenter, newCenter);
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
    /// 播放单个难度item的切换动画(起点按"切换前"中心、终点按"切换后"中心计算位移/透明度/缩放)
    /// </summary>
    /// <param name="itemView">目标item</param>
    /// <param name="difficulty">该item代表的难度</param>
    /// <param name="oldCenter">切换前的当前难度</param>
    /// <param name="newCenter">切换后的当前难度</param>
    protected void AnimSwitchOneItem(UIViewDialogPortalDetailsItem itemView, int difficulty, int oldCenter, int newCenter)
    {
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

    #region 输入响应
    /// <summary>
    /// 输入响应: 按 ESC 退出(关闭)弹窗, 等同点击取消; 按左右方向键切换难度(等同点击左右切换按钮)
    /// </summary>
    /// <param name="inputType">触发的输入动作类型</param>
    /// <param name="callback">输入回调上下文</param>
    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            //ESC 退出弹窗
            CancelOnClick();
        }
        else if (inputType == InputActionUIEnum.Navigate)
        {
            //左右方向键切换难度(上一档/下一档), 上下方向不处理
            Vector2 navigateData = callback.ReadValue<Vector2>();
            if (navigateData.x < 0)
            {
                OnClickForChangeDifficultyLevel(-1);
            }
            else if (navigateData.x > 0)
            {
                OnClickForChangeDifficultyLevel(1);
            }
        }
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
        //切换难度并同步该难度预生成的道路/关卡随机数据(气泡与实际战斗均读取这些字段)
        gameWorldInfoRandom.SetDifficultyLevel(newDifficulty);
        AnimSwitchDifficulty(oldDifficulty, newDifficulty);
    }
    #endregion
}
