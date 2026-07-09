
using UnityEngine;
using DG.Tweening;

public partial class UIViewDialogPortalDetailsItem : BaseUIView
{
    #region 数据
    //当前item代表的难度等级
    protected int difficultyLevel;
    //当前(选中)难度item的透明度
    public const float alphaCurrent = 1f;
    //非当前难度item的透明度
    public const float alphaOther = 0.5f;
    //当前(选中)难度item的缩放
    public const float scaleCurrent = 1f;
    //非当前难度item的缩放
    public const float scaleOther = 0.6f;
    //Icon 漂浮 idle 动画的 Animator(挂在item根节点, 控制器 UIViewDialogPortalDetailsItem.controller)
    protected Animator idleAnimator;
    //idle 状态在控制器中的状态名(与 manage_animation 创建的默认状态一致)
    protected const string idleStateName = "Idle";
    //本item漂浮动画的随机相位偏移(归一化0~1), 一次性确定后保持不变, 让各item漂浮错开; <0 表示尚未生成
    protected float idleAnimNormalizedOffset = -1f;
    //随机相位是否已成功应用到Animator(应用后不再重复应用, 避免难度切换时漂浮被还原重播)
    protected bool idleOffsetApplied;

    /// <summary>
    /// 当前item代表的难度等级
    /// </summary>
    public int DifficultyLevel => difficultyLevel;
    #endregion

    #region 生命周期
    /// <summary>
    /// 启用回调: 首次启用时尝试应用随机漂浮相位.
    /// 注意只在"首次"应用一次: 难度切换会反复 SetActive 关/开本item, 若每次都重新随机/重播, 漂浮会被还原.
    /// 配合 Animator.keepAnimatorStateOnDisable=true(见 TryApplyIdleAnimOffset), 切换时漂浮可无缝续播.
    /// </summary>
    public override void OnEnable()
    {
        base.OnEnable();
        TryApplyIdleAnimOffset();
    }

    /// <summary>
    /// 每帧轮询: 首次OnEnable时弹窗层级可能刚激活、Animator尚未初始化, 随机相位无法落定;
    /// 这里持续重试, 直到Animator可正常求值并成功应用一次(应用后即不再进入实际逻辑).
    /// </summary>
    public void Update()
    {
        if (!idleOffsetApplied)
            TryApplyIdleAnimOffset();
    }
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置难度item数据
    /// </summary>
    /// <param name="gameWorldInfo">世界配置数据(用于图标资源判断)</param>
    /// <param name="gameWorldInfoRandom">世界随机数据(提供星球图标种子, 并作为传送门详情气泡数据源)</param>
    /// <param name="difficultyLevel">该item代表的难度等级</param>
    /// <param name="isUnlock">该难度是否已解锁(未解锁时显示锁链图标)</param>
    /// <param name="bgColor">该难度的背景颜色(由难度表 bg_color 决定)</param>
    public void SetData(GameWorldInfoBean gameWorldInfo, GameWorldInfoRandomBean gameWorldInfoRandom, int difficultyLevel, bool isUnlock, Color bgColor)
    {
        this.difficultyLevel = difficultyLevel;
        SetIcon(gameWorldInfo.icon_res, gameWorldInfoRandom.iconSeed);
        SetLevel(difficultyLevel);
        SetUnlock(isUnlock);
        SetBGColor(bgColor);
        SetPopup(gameWorldInfo, gameWorldInfoRandom, isUnlock);
        SetComplete(gameWorldInfoRandom.worldId, difficultyLevel);
        //漂浮相位的随机化改在 OnEnable 里做(此处弹窗可能尚未激活, Animator 无法求值)
    }

    /// <summary>
    /// 设置传送门详情气泡数据(与 UIViewBasePortalItem 一致, 悬停 IconBG 时展示 PopupEnum.PortalDetails)
    /// 未解锁难度不展示气泡(targetData 置空, PopupButtonCommonView 悬停时直接跳过)
    /// </summary>
    /// <param name="gameWorldInfo">世界配置数据</param>
    /// <param name="gameWorldInfoRandom">世界随机数据(气泡展示名字/线路数/关卡数)</param>
    /// <param name="isUnlock">该难度是否已解锁(未解锁不显示气泡)</param>
    public void SetPopup(GameWorldInfoBean gameWorldInfo, GameWorldInfoRandomBean gameWorldInfoRandom, bool isUnlock)
    {
        if (ui_IconBG_PopupButtonCommonView == null)
            return;
        if (!isUnlock)
        {
            //未解锁难度: 清空气泡数据, 悬停不弹窗
            ui_IconBG_PopupButtonCommonView.SetData(null, PopupEnum.PortalDetails);
            return;
        }
        //气泡展示本item代表的难度数据(而非共享的当前难度), 切换难度后各item气泡互不串味
        ui_IconBG_PopupButtonCommonView.SetData((gameWorldInfo, gameWorldInfoRandom, difficultyLevel), PopupEnum.PortalDetails);
    }

    /// <summary>
    /// 设置难度背景色(IconBG)
    /// </summary>
    /// <param name="bgColor">背景颜色</param>
    public void SetBGColor(Color bgColor)
    {
        if (ui_IconBG_Image == null)
            return;
        ui_IconBG_Image.color = bgColor;
    }

    /// <summary>
    /// 设置星球图标(与 UIViewBasePortalItem.SetIcon 逻辑一致)
    /// </summary>
    /// <param name="iconRes">图标资源名(为空则根据种子程序化生成星球图)</param>
    /// <param name="iconSeed">星球图标随机种子</param>
    public void SetIcon(string iconRes, int iconSeed)
    {
        if (iconRes.IsNull())
        {
            CreateToolsForPlanetTextureBean createData = new CreateToolsForPlanetTextureBean(iconSeed);
            var planetTex = CreateTools.CreatePlanetTexture(createData);
            ui_Icon.texture = planetTex;
            ui_Icon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 设置难度文本(与 UIDialogPortalDetails 原难度文本一致, 文本id 403 "难度 等级{0}")
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    public void SetLevel(int difficultyLevel)
    {
        ui_Level.text = string.Format(TextHandler.Instance.GetTextById(403), difficultyLevel);
    }

    /// <summary>
    /// 设置解锁状态(未解锁时显示锁链图标 Chain_1/Chain_2)
    /// </summary>
    /// <param name="isUnlock">该难度是否已解锁</param>
    public void SetUnlock(bool isUnlock)
    {
        ui_Chain_1.gameObject.SetActive(!isUnlock);
        ui_Chain_2.gameObject.SetActive(!isUnlock);
    }

    /// <summary>
    /// 设置通关标记: 玩家曾通关过该世界对应难度的征服模式(征服通关统计次数>0)则显示 ui_Complete_1.
    /// ui_Complete_0 为测试用备用标记, 始终隐藏(默认关闭, 仅供测试), 正式通关标记走 ui_Complete_1.
    /// </summary>
    /// <param name="worldId">世界id</param>
    /// <param name="difficultyLevel">难度等级</param>
    public void SetComplete(long worldId, int difficultyLevel)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        bool isComplete = userData != null
            && userData.GetUserAchievementData().GetConquerCompleteCount(worldId, difficultyLevel) > 0;
        //Complete_0 仅测试用, 始终隐藏
        if (ui_Complete_0 != null)
            ui_Complete_0.gameObject.SetActive(false);
        //Complete_1 为正式通关标记, 按是否通关显隐
        if (ui_Complete_1 != null)
            ui_Complete_1.gameObject.SetActive(isComplete);
    }
    #endregion

    #region 透明度
    /// <summary>
    /// 立即设置透明度(当前难度1 / 两侧0.5 / 滑出可视区0)
    /// </summary>
    /// <param name="alpha">目标透明度</param>
    public void SetAlpha(float alpha)
    {
        if (ui_UIViewDialogPortalDetailsItem == null)
            return;
        ui_UIViewDialogPortalDetailsItem.alpha = alpha;
    }

    /// <summary>
    /// 渐变透明度(用于左右滑动动画; 进出可视区的临时item渐变到0/从0渐入, 避免突兀地出现和消失)
    /// </summary>
    /// <param name="alpha">动画结束时的目标透明度</param>
    /// <param name="duration">渐变时长</param>
    public void DoFadeAlpha(float alpha, float duration)
    {
        if (ui_UIViewDialogPortalDetailsItem == null)
            return;
        ui_UIViewDialogPortalDetailsItem.DOKill();
        ui_UIViewDialogPortalDetailsItem.DOFade(alpha, duration);
    }

    /// <summary>
    /// 终止item上的所有动画(位移、缩放与透明度)
    /// </summary>
    public void KillAnim()
    {
        rectTransform.DOKill();
        if (ui_UIViewDialogPortalDetailsItem != null)
            ui_UIViewDialogPortalDetailsItem.DOKill();
    }
    #endregion

    #region 缩放
    /// <summary>
    /// 立即设置缩放(当前难度1 / 两侧0.8)
    /// </summary>
    /// <param name="scale">目标统一缩放</param>
    public void SetScale(float scale)
    {
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// 渐变缩放(用于左右滑动动画; 切到中间的item放大到1, 滑到两侧的item缩小到0.8)
    /// </summary>
    /// <param name="scale">动画结束时的目标统一缩放</param>
    /// <param name="duration">渐变时长</param>
    public void DoScale(float scale, float duration)
    {
        rectTransform.DOScale(new Vector3(scale, scale, 1f), duration);
    }
    #endregion

    #region 动画
    /// <summary>
    /// 尝试给本item的 Icon idle 漂浮动画设置一个一次性随机起始相位(归一化0~1), 让各item漂浮错开.
    /// 仅在 Animator 真正可求值(层级已激活且组件已启用)时应用, 且全程只成功应用一次:
    /// - 解决"3个item漂浮同步": 首次OnEnable可能在弹窗层级激活前, 此时随机会被吞掉, 故用 Update 重试至落定.
    /// - 解决"切换难度漂浮被还原重播": 应用后置 idleOffsetApplied=true 不再重置, 并开启 keepAnimatorStateOnDisable,
    ///   使难度切换的 SetActive 关/开不重置 Animator 状态, 漂浮相位无缝续播.
    /// </summary>
    public void TryApplyIdleAnimOffset()
    {
        if (idleOffsetApplied)
            return;
        if (idleAnimator == null)
            idleAnimator = GetComponent<Animator>();
        if (idleAnimator == null || idleAnimator.runtimeAnimatorController == null)
            return;
        //层级未真正激活或 Animator 未启用时无法稳定求值, 等待下一帧 Update 重试
        if (!gameObject.activeInHierarchy || !idleAnimator.isActiveAndEnabled)
            return;
        //保持状态: 难度切换反复 SetActive 关/开本item时不重置漂浮动画, 避免被还原重播
        idleAnimator.keepAnimatorStateOnDisable = true;
        //相位一次性确定, 之后保持不变
        if (idleAnimNormalizedOffset < 0f)
            idleAnimNormalizedOffset = Random.Range(0f, 1f);
        //以随机归一化时间从 Idle 状态开始播放, 并强制当帧求值把相位固定下来
        idleAnimator.Play(idleStateName, 0, idleAnimNormalizedOffset);
        idleAnimator.Update(0f);
        idleOffsetApplied = true;
    }
    #endregion
}
