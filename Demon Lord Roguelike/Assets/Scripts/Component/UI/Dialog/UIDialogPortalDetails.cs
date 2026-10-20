

using System;
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

    //难度item的水平间距(中心±3 共7个, 间距按容器宽度收缩以保证不压到左右切换按钮)
    protected const float itemSpacing = 230f;
    //难度切换左右滑动动画时长
    protected const float animDuration = 0.6f;
    //难度item对象池(7个常驻显示 + 切换滑动时第8个临时滑出)
    protected List<UIViewDialogPortalDetailsItem> listItemPool;
    //一排最多展示的难度item数量(中心 + 左右各3)
    protected const int maxShowCount = 7;
    //中心难度一侧展示的item数(左右各 maxShowCount/2)
    protected const int visibleHalfCount = maxShowCount / 2;
    //用户可挑战(已解锁)的最高难度
    protected int unlockDifficultyMax;
    //该世界配置表中存在的最高难度(用于决定是否展示未解锁的"下一个"预览item)
    protected int configDifficultyMax;
    //难度下限(征服=1; 无尽=2, 第一难度没有无尽模式)
    protected int difficultyMin = 1;
    //当前模式的逐档解锁判定(无尽/100勇士允许跳级, 不能只用 difficulty<=unlockDifficultyMax)
    protected Func<int, bool> isDifficultyUnlocked;
    //当前是否正在播放切换动画(动画期间禁止再次切换)
    protected bool isAnimating;
    //挑战100勇士-来袭魔物item列表(动态实例化, 模板 ui_UIViewDialogPortalDetailsCreatureItem 不列入)
    protected List<UIViewDialogPortalDetailsCreatureItem> listCreatureItem;
    //来袭魔物item最大水平间距(数量多时间距按容器宽度收缩)
    protected const float creatureItemSpacing = 230f;
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

        //三种模式统一走难度选择器; 挑战100勇士额外显示来袭魔物列表
        bool isChallengeHundred = gameWorldInfoRandom.gameFightType == GameFightTypeEnum.ChallengeHundred;
        bool isInfinite = gameWorldInfoRandom.gameFightType == GameFightTypeEnum.Infinite;

        //先按模式归位容器显隐(弹窗复用时防上次打开残留); 必须在量取容器宽高/刷新item之前激活
        if (ui_Difficulty != null)
            ui_Difficulty.gameObject.SetActive(true);
        ui_DifficultySelectLeftBtn.gameObject.SetActive(true);
        ui_DifficultySelectRightBtn.gameObject.SetActive(true);
        if (ui_CreatureList != null)
            ui_CreatureList.gameObject.SetActive(isChallengeHundred);

        if (isInfinite)
        {
            //无尽模式: 难度2起(第一难度没有无尽模式), 逐档解锁(允许跳级), 配置上限=无尽配置表最高难度(供未解锁预览)
            difficultyMin = 2;
            unlockDifficultyMax = userUnlock.GetUnlockInfiniteDifficultyLevel(gameWorldInfoRandom.worldId);
            configDifficultyMax = FightTypeInfiniteInfoCfg.GetMaxLevel(gameWorldInfoRandom.worldId);
            isDifficultyUnlocked = difficulty => userUnlock.CheckInfiniteUnlock(gameWorldInfoRandom.worldId, difficulty);
        }
        else if (isChallengeHundred)
        {
            //挑战100勇士: 独立难度链, 难度1默认可用, 逐档解锁(允许跳级)
            difficultyMin = 1;
            unlockDifficultyMax = userUnlock.GetUnlockChallengeHundredDifficultyLevel(gameWorldInfoRandom.worldId);
            //世界有100勇士难度链时展示配置表最高难度(未解锁档显示锁链预览), 否则仅展示已解锁档
            configDifficultyMax = gameWorldInfo.unlock_id_challenge_hundred != 0
                ? Mathf.Max(unlockDifficultyMax, FightTypeChallengeHundredInfoCfg.GetMaxDifficultyLevel())
                : unlockDifficultyMax;
            isDifficultyUnlocked = difficulty => userUnlock.CheckChallengeHundredUnlock(gameWorldInfoRandom.worldId, difficulty);
            //旧存档兼容: 懒重建逐难度预生成数据
            gameWorldInfoRandom.EnsureChallengeHundredRandomData();
        }
        else
        {
            difficultyMin = 1;
            //用户可选择的最高难度
            unlockDifficultyMax = userUnlock.GetUnlockGameWorldConquerDifficultyLevel(gameWorldInfoRandom.worldId);
            //该世界配置存在的最高难度
            configDifficultyMax = FightTypeConquerInfoCfg.GetMaxLevel(gameWorldInfoRandom.worldId);
            isDifficultyUnlocked = difficulty => difficulty <= unlockDifficultyMax;
        }

        //把默认难度约束在 [难度下限, 展示上限] 内; 若落在未解锁档则取最近的已解锁档
        int defaultDifficulty = ResolveDefaultDifficulty(gameWorldInfoRandom.difficultyLevel);
        gameWorldInfoRandom.SetDifficultyLevel(defaultDifficulty);

        //难度选择器与来袭魔物列表的位置/高度完全沿用 prefab 默认布局, 运行时不改兄弟顺序与间距
        InitItemPool();
        RefreshItemsImmediate(gameWorldInfoRandom.difficultyLevel);
        //挑战100勇士: 按当前活动难度重建来袭魔物列表
        if (isChallengeHundred)
            SetDataForChallengeHundred();

        //出战阵容选择区: 已解锁阵容数量>=2才显示, 默认选中上次保存的出战阵容
        int lineupNum = userUnlock.GetUnlockLineupNum();
        ui_Lineup.gameObject.SetActive(lineupNum >= 2);
        if (lineupNum >= 2)
        {
            ui_LineupTitle.text = TextHandler.Instance.GetTextById(30009);
            RefreshLineupShow();
        }
        //由内到外依次强制重建布局: 文本(ContentPro)→内容容器(ContentShow)→对话框内容(DialogContent), 让嵌套 ContentSizeFitter 先算内层首选尺寸再撑开外层
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_ContentPro.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_ContentShow);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_DialogContent);
    }

    /// <summary>
    /// 解析默认难度: 夹到 [下限, 展示上限] 后若落在未解锁档, 优先向低再向高找最近的已解锁档
    /// </summary>
    /// <param name="activeDifficulty">存档中的当前难度</param>
    /// <returns>可用的默认难度</returns>
    protected int ResolveDefaultDifficulty(int activeDifficulty)
    {
        int displayMax = Mathf.Max(unlockDifficultyMax, configDifficultyMax);
        int desired = Mathf.Clamp(activeDifficulty, difficultyMin, Mathf.Max(difficultyMin, displayMax));
        if (IsDifficultyUnlock(desired))
            return desired;
        //优先向低找
        for (int difficulty = desired - 1; difficulty >= difficultyMin; difficulty--)
        {
            if (IsDifficultyUnlock(difficulty))
                return difficulty;
        }
        //再向高找
        for (int difficulty = desired + 1; difficulty <= displayMax; difficulty++)
        {
            if (IsDifficultyUnlock(difficulty))
                return difficulty;
        }
        //兜底: 已解锁最高难度
        return Mathf.Max(difficultyMin, unlockDifficultyMax);
    }

    /// <summary>
    /// 指定难度是否已解锁(无尽/100勇士逐档判定, 允许跳级; 征服为连续档)
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <returns>true=已解锁</returns>
    protected bool IsDifficultyUnlock(int difficultyLevel)
    {
        if (isDifficultyUnlocked != null)
            return isDifficultyUnlocked(difficultyLevel);
        return difficultyLevel >= difficultyMin && difficultyLevel <= unlockDifficultyMax;
    }

    /// <summary>
    /// 从指定难度向指定方向找下一个已解锁难度(跳过跳级造成的未解锁空档)
    /// </summary>
    /// <param name="fromDifficulty">当前难度</param>
    /// <param name="direction">方向(-1 上一个 / +1 下一个)</param>
    /// <returns>下一个已解锁难度; 找不到返回原难度</returns>
    protected int FindNextUnlockDifficulty(int fromDifficulty, int direction)
    {
        int displayMax = Mathf.Max(unlockDifficultyMax, configDifficultyMax);
        for (int difficulty = fromDifficulty + direction;
             direction > 0 ? difficulty <= displayMax : difficulty >= difficultyMin;
             difficulty += direction)
        {
            if (IsDifficultyUnlock(difficulty))
                return difficulty;
        }
        return fromDifficulty;
    }

    /// <summary>
    /// 设置挑战100勇士模式数据: 按冻结配置行(challengeHundredRowId)的敌人列表去重后动态实例化来袭魔物item, 手动等距居中排布
    /// </summary>
    protected void SetDataForChallengeHundred()
    {
        if (ui_UIViewDialogPortalDetailsCreatureItem == null || ui_CreatureList == null)
            return;

        //清理上次打开的魔物item(模板不列入列表, 不销毁)
        if (listCreatureItem != null)
        {
            for (int i = 0; i < listCreatureItem.Count; i++)
            {
                if (listCreatureItem[i] != null)
                    Destroy(listCreatureItem[i].gameObject);
            }
        }
        listCreatureItem = new List<UIViewDialogPortalDetailsCreatureItem>();
        //模板item保持隐藏作为克隆蓝本
        ui_UIViewDialogPortalDetailsCreatureItem.gameObject.SetActive(false);

        //取传送门生成时冻结的配置行
        var challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(gameWorldInfoRandom.challengeHundredRowId);
        if (challengeHundredInfo == null)
        {
            LogUtil.LogError($"挑战100勇士详情弹窗找不到配置行 rowId:{gameWorldInfoRandom.challengeHundredRowId}");
            return;
        }
        //敌人列表去重(同一魔物可能重复配置)
        List<long> enemyIds = new List<long>();
        long[] enemyIdArray = challengeHundredInfo.GetEnemyIdList();
        for (int i = 0; i < enemyIdArray.Length; i++)
        {
            if (!enemyIds.Contains(enemyIdArray[i]))
                enemyIds.Add(enemyIdArray[i]);
        }
        //动态实例化魔物item, 手动等距居中排布(数量多时间距收缩)
        int count = enemyIds.Count;
        float spacing = creatureItemSpacing;
        float containerWidth = ui_CreatureList.rect.width;
        if (count > 1 && containerWidth > 0 && spacing * (count - 1) > containerWidth)
            spacing = containerWidth / (count - 1);
        for (int i = 0; i < count; i++)
        {
            GameObject objItem = Instantiate(ui_UIViewDialogPortalDetailsCreatureItem.gameObject, ui_CreatureList);
            objItem.transform.localScale = Vector3.one;
            //实例化尺寸跟随来袭魔物容器高度(当前 prefab 300), 保持方形背景不变形, 并让 Spine 按该高度等比适配
            RectTransform itemRect = objItem.transform as RectTransform;
            float itemSize = ui_CreatureList.rect.height;
            if (itemSize > 0)
                itemRect.sizeDelta = new Vector2(itemSize, itemSize);
            UIViewDialogPortalDetailsCreatureItem itemView = objItem.GetComponent<UIViewDialogPortalDetailsCreatureItem>();
            itemView.SetData(enemyIds[i]);
            //以容器中心为原点等距排布
            float posX = (i - (count - 1) / 2f) * spacing;
            (objItem.transform as RectTransform).anchoredPosition = new Vector2(posX, 0);
            listCreatureItem.Add(itemView);
        }
    }

    /// <summary>
    /// 刷新出战阵容显示(阵容名: 自定义名优先, 未改名显示默认「阵容 {序号}」)
    /// </summary>
    protected void RefreshLineupShow()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        ui_LineupName.text = userData.GetLineupShowName(userData.GetLineupFightIndex());
    }

    /// <summary>
    /// 初始化难度item对象池(以模板item为第一个, 再克隆出共8个: 7个常驻显示 + 1个切换时临时滑出)
    /// </summary>
    protected void InitItemPool()
    {
        if (listItemPool != null)
            return;
        listItemPool = new List<UIViewDialogPortalDetailsItem>();
        //模板item作为对象池第一个
        listItemPool.Add(ui_UIViewDialogPortalDetailsItem);
        Transform itemParent = ui_UIViewDialogPortalDetailsItem.transform.parent;
        //再克隆7个(共8个)
        for (int i = 1; i < 8; i++)
        {
            GameObject objItem = Instantiate(ui_UIViewDialogPortalDetailsItem.gameObject, itemParent);
            objItem.transform.localScale = Vector3.one;
            UIViewDialogPortalDetailsItem itemView = objItem.GetComponent<UIViewDialogPortalDetailsItem>();
            listItemPool.Add(itemView);
            //运行时克隆的item不在 Awake 的 RegisterButtons 收集范围内, 需手动注册按钮点击(模板item的按钮已在 Awake 注册)
            Button[] itemButtons = objItem.GetComponentsInChildren<Button>(true);
            for (int j = 0; j < itemButtons.Length; j++)
                RegisterButton(itemButtons[j]);
        }
    }

    /// <summary>
    /// 立即按标准布局刷新一排最多7个item(中心±3, 越界的难度不显示)
    /// </summary>
    /// <param name="centerDifficulty">中间(当前选中)的难度</param>
    protected void RefreshItemsImmediate(int centerDifficulty)
    {
        HideAllItems();
        int poolIndex = 0;
        for (int offset = -visibleHalfCount; offset <= visibleHalfCount; offset++)
        {
            int difficulty = centerDifficulty + offset;
            if (!IsValidDisplayDifficulty(difficulty))
                continue;
            UIViewDialogPortalDetailsItem itemView = listItemPool[poolIndex++];
            ShowItem(itemView, difficulty, offset * itemSpacing);
            itemView.SetAlpha(GetItemAlpha(difficulty, centerDifficulty));
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
        //大幅跳级(跨越超过可视范围)直接刷新, 避免长距离滑入观感突兀
        if (Mathf.Abs(newCenter - oldCenter) > visibleHalfCount)
        {
            RefreshItemsImmediate(newCenter);
            return;
        }
        isAnimating = true;
        HideAllItems();

        //关键: 动画结束时停在中心的item, 必须与随后 RefreshItemsImmediate(newCenter) 占据中心的是同一个对象池对象.
        //否则(典型为向右切换)中心item会在动画末尾被瞬间换成另一个正在缩小的item, 表现为"缩放最后一刻一瞬间放大".
        //因此这里按"切换后"中心的紧凑顺序(与 RefreshItemsImmediate 完全一致)把中间7档难度依次分配到 pool[0..6],
        //再把滑出可视区的那一档放到剩余槽位, 保证前后池索引一致、每个屏幕位置的item对象在刷新瞬间无缝衔接.
        int poolIndex = 0;
        for (int offset = -visibleHalfCount; offset <= visibleHalfCount; offset++)
        {
            int difficulty = newCenter + offset;
            if (!IsValidDisplayDifficulty(difficulty))
                continue;
            if (poolIndex >= listItemPool.Count)
                break;
            AnimSwitchOneItem(listItemPool[poolIndex++], difficulty, oldCenter, newCenter);
        }
        //滑出可视区的临时item(切换前在可视边缘、切换后超出当前±3范围的那一档), 从外侧滑出并淡出; 大幅跳转时其余出界item直接隐藏
        int leavingDifficulty = newCenter > oldCenter ? oldCenter - visibleHalfCount : oldCenter + visibleHalfCount;
        if (IsValidDisplayDifficulty(leavingDifficulty) && poolIndex < listItemPool.Count)
        {
            AnimSwitchOneItem(listItemPool[poolIndex++], leavingDifficulty, oldCenter, newCenter);
        }

        //动画结束后回到标准7item布局(顺带回收多余item)
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
        //切到中间的item放大到1, 滑到两侧的item按梯度缩小(缩放跟随透明度渐变)
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
        bool isUnlock = IsDifficultyUnlock(difficulty);
        Color bgColor = GetDifficultyBGColor(difficulty);
        itemView.gameObject.SetActive(true);
        //通关标记仅征服模式显示(无尽无通关统计)
        itemView.SetData(gameWorldInfo, gameWorldInfoRandom, difficulty, isUnlock, bgColor, isShowCompleteMark: gameWorldInfoRandom.gameFightType == GameFightTypeEnum.Conquer);
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
    /// 判断一个难度是否可作为item展示(在 [难度下限, 展示上限] 内, 展示上限取已解锁与配置最高的较大值)
    /// </summary>
    /// <param name="difficulty">难度等级</param>
    protected bool IsValidDisplayDifficulty(int difficulty)
    {
        int displayMax = Mathf.Max(unlockDifficultyMax, configDifficultyMax);
        return difficulty >= difficultyMin && difficulty <= displayMax;
    }

    /// <summary>
    /// 计算某难度在以 center 为中心布局时应有的透明度(按距中心距离取梯度; 超出可视区|offset|>3 的临时进出item透明度为0)
    /// </summary>
    /// <param name="difficulty">item代表的难度</param>
    /// <param name="center">布局中心难度</param>
    protected float GetItemAlpha(int difficulty, int center)
    {
        int distance = Mathf.Abs(difficulty - center);
        if (distance > visibleHalfCount)
            return 0f;
        return UIViewDialogPortalDetailsItem.alphaByDistance[distance];
    }

    /// <summary>
    /// 计算某难度在以 center 为中心布局时应有的缩放(按距中心距离取梯度; 超出可视区的进出item取最外档缩放)
    /// </summary>
    /// <param name="difficulty">item代表的难度</param>
    /// <param name="center">布局中心难度</param>
    protected float GetItemScale(int difficulty, int center)
    {
        int distance = Mathf.Abs(difficulty - center);
        return UIViewDialogPortalDetailsItem.scaleByDistance[Mathf.Min(distance, visibleHalfCount)];
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
        else if (viewButton == ui_LineupLeftBtn)
        {
            OnClickForChangeLineup(-1);
        }
        else if (viewButton == ui_LineupRightBtn)
        {
            OnClickForChangeLineup(1);
        }
        else if (viewButton != null)
        {
            //点击难度item直接切换到该item代表的难度
            UIViewDialogPortalDetailsItem itemView = viewButton.GetComponentInParent<UIViewDialogPortalDetailsItem>();
            if (itemView != null)
                OnClickForDifficultyLevel(itemView);
        }
    }

    /// <summary>
    /// 点击切换出战阵容(循环切换: 第1套往左到第N套, 第N套往右回第1套), 切换后保存到用户数据
    /// </summary>
    /// <param name="change">切换方向(-1 上一套 / +1 下一套)</param>
    public void OnClickForChangeLineup(int change)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        int lineupNum = userData.GetUserUnlockData().GetUnlockLineupNum();
        if (lineupNum < 2)
            return;
        int oldIndex = userData.GetLineupFightIndex();
        //循环切换: 先转0基序号偏移再绕回, 最后转回1基
        int newIndex = (oldIndex - 1 + change) % lineupNum;
        if (newIndex < 0)
            newIndex += lineupNum;
        newIndex += 1;
        if (newIndex == oldIndex)
            return;
        userData.SetLineupFightIndex(newIndex);
        //选择即落盘, 下次打开本弹窗默认选中该阵容
        GameDataHandler.Instance.manager.SaveUserData();
        RefreshLineupShow();
    }

    /// <summary>
    /// 点击难度item直接切换到该难度(未解锁难度提示并回弹, 点击当前难度不响应)
    /// </summary>
    /// <param name="itemView">被点击的难度item</param>
    public void OnClickForDifficultyLevel(UIViewDialogPortalDetailsItem itemView)
    {
        if (isAnimating || itemView == null)
            return;
        int oldDifficulty = gameWorldInfoRandom.difficultyLevel;
        int targetDifficulty = itemView.DifficultyLevel;
        if (targetDifficulty == oldDifficulty)
            return;
        //未解锁难度不可切换: 朝目标方向回弹 + 提示
        if (!IsDifficultyUnlock(targetDifficulty))
        {
            int changeLevel = targetDifficulty > oldDifficulty ? 1 : -1;
            AnimSwitchBlocked(changeLevel);
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(404), 0);
            return;
        }
        //切换难度并同步该难度预生成的道路/关卡随机数据(气泡与实际战斗均读取这些字段)
        gameWorldInfoRandom.SetDifficultyLevel(targetDifficulty);
        //挑战100勇士: 刷新当前难度的来袭魔物列表
        if (gameWorldInfoRandom.gameFightType == GameFightTypeEnum.ChallengeHundred)
            SetDataForChallengeHundred();
        AnimSwitchDifficulty(oldDifficulty, targetDifficulty);
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
        //向指定方向找下一个已解锁难度(跳过跳级造成的未解锁空档)
        int newDifficulty = FindNextUnlockDifficulty(oldDifficulty, changeLevel > 0 ? 1 : -1);
        if (newDifficulty == oldDifficulty)
        {
            //已到边界/前方全是未解锁档: 播放回弹动画
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
        //挑战100勇士: 刷新当前难度的来袭魔物列表
        if (gameWorldInfoRandom.gameFightType == GameFightTypeEnum.ChallengeHundred)
            SetDataForChallengeHundred();
        AnimSwitchDifficulty(oldDifficulty, newDifficulty);
    }
    #endregion
}
