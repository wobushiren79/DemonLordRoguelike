
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
    public const float scaleOther = 0.8f;
    //Icon 漂浮 idle 动画的 Animator(挂在item根节点, 控制器 UIViewDialogPortalDetailsItem.controller)
    protected Animator idleAnimator;
    //idle 状态在控制器中的状态名(与 manage_animation 创建的默认状态一致)
    protected const string idleStateName = "Idle";

    /// <summary>
    /// 当前item代表的难度等级
    /// </summary>
    public int DifficultyLevel => difficultyLevel;
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
        SetPopup(gameWorldInfo, gameWorldInfoRandom);
        RandomizeIdleAnimOffset();
    }

    /// <summary>
    /// 设置传送门详情气泡数据(与 UIViewBasePortalItem 一致, 悬停 IconBG 时展示 PopupEnum.PortalDetails)
    /// </summary>
    /// <param name="gameWorldInfo">世界配置数据</param>
    /// <param name="gameWorldInfoRandom">世界随机数据(气泡展示名字/线路数/关卡数)</param>
    public void SetPopup(GameWorldInfoBean gameWorldInfo, GameWorldInfoRandomBean gameWorldInfoRandom)
    {
        if (ui_IconBG_PopupButtonCommonView == null)
            return;
        ui_IconBG_PopupButtonCommonView.SetData((gameWorldInfo, gameWorldInfoRandom), PopupEnum.PortalDetails);
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
    /// 随机化 Icon idle 漂浮动画的起始播放点(归一化 0~1), 避免所有难度item的漂浮完全同步
    /// </summary>
    public void RandomizeIdleAnimOffset()
    {
        if (idleAnimator == null)
            idleAnimator = GetComponent<Animator>();
        if (idleAnimator == null || idleAnimator.runtimeAnimatorController == null)
            return;
        //以随机归一化时间从 Idle 状态开始播放, 让每个item的漂浮相位错开
        idleAnimator.Play(idleStateName, 0, Random.Range(0f, 1f));
        //本方法常在 item 刚 SetActive(true) 的同一帧被调用; 此时 Animator 尚未求值, 若不强制提交,
        //Animator 启用后的首次 rebind 会把状态重置回默认 normalizedTime=0, 导致上面的随机偏移被吞掉、所有item漂浮同步.
        //用 Update(0f) 强制当帧求值, 把随机起始相位固定下来.
        idleAnimator.Update(0f);
    }
    #endregion
}
