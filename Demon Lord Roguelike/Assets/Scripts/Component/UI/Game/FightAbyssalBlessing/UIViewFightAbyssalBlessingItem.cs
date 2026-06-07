
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UIViewFightAbyssalBlessingItem : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{
    public AbyssalBlessingInfoBean abyssalBlessingInfo;

    /// <summary>
    /// 设置数据。等级差异由等级角标体现，名字/详情统一取馈赠自身的多语言文本。
    /// </summary>
    public void SetData(AbyssalBlessingInfoBean abyssalBlessingInfo)
    {
        this.abyssalBlessingInfo = abyssalBlessingInfo;
        SetName(abyssalBlessingInfo.name_language);
        SetDetails(abyssalBlessingInfo.details_language);
        SetIcon(abyssalBlessingInfo.icon_res);
        SetLevel(abyssalBlessingInfo.level);
    }

    /// <summary>
    /// 设置图像
    /// </summary>
    public void SetIcon(string iconName)
    {
        IconHandler.Instance.SetAbyssalBlessingIcon(iconName, ui_Icon);
    }

    /// <summary>
    /// 设置等级角标与等级配色：按等级统一为内容底/图标底/详情底着色（Lv1-5 共 5 种颜色）；
    /// level&gt;0 时额外显示角标并为角标底/角标文本着色，level&lt;=0（可重复馈赠）隐藏角标。
    /// </summary>
    public void SetLevel(int level)
    {
        Color levelColor = AbyssalBlessingInfoCfg.GetLevelColor(level);
        if (ui_ContentBG != null)
            ui_ContentBG.color = levelColor;
        if (ui_NameBG != null)
            ui_NameBG.color = levelColor;
        if (ui_IconContent != null)
            ui_IconContent.color = levelColor;
        if (ui_DetailsBG != null)
            ui_DetailsBG.color = levelColor;

        bool show = level > 0;
        if (ui_LevelBG != null)
        {
            ui_LevelBG.gameObject.SetActive(show);
            if (show)
                ui_LevelBG.color = levelColor;
        }
        if (ui_LevelText != null)
            ui_LevelText.gameObject.SetActive(show);
        if (!show) return;
        if (ui_LevelText != null)
        {
            ui_LevelText.text = $"{level}";
            ui_LevelText.color = levelColor;
        }
    }

    #region 悬停动画

    /// <summary>
    /// 鼠标悬停放大倍率
    /// </summary>
    private const float HOVER_SCALE = 1.08f;

    /// <summary>
    /// 悬停动画时长
    /// </summary>
    private const float HOVER_ANIM_DURATION = 0.15f;

    /// <summary>
    /// 悬停动画 Tween，复用时先 Kill 避免叠加
    /// </summary>
    private Tween hoverTween;

    /// <summary>
    /// 鼠标进入-放大强调
    /// </summary>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        hoverTween?.Kill();
        hoverTween = transform
            .DOScale(Vector3.one * HOVER_SCALE, HOVER_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }

    /// <summary>
    /// 鼠标离开-还原大小
    /// </summary>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        hoverTween?.Kill();
        hoverTween = transform
            .DOScale(Vector3.one, HOVER_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }

    /// <summary>
    /// 关闭悬停动画并复位缩放
    /// 用于UI关闭时主动清理，避免鼠标停留在控件上未触发 OnPointerExit 导致动画残留
    /// </summary>
    public void KillHoverAnim()
    {
        hoverTween?.Kill();
        hoverTween = null;
        transform.localScale = Vector3.one;
    }

    #endregion

    #region 动画

    /// <summary>
    /// 出现动画总时长
    /// </summary>
    private const float SHOW_ANIM_DURATION = 0.35f;

    /// <summary>
    /// 出现动画起始下移距离（像素），用于营造"从下方弹出"的效果
    /// </summary>
    private const float SHOW_ANIM_RISE_DISTANCE = 150f;

    /// <summary>
    /// 首次缓存的原始 anchoredPosition，用于每次重播复位
    /// </summary>
    private Vector2 originalAnchoredPos;

    /// <summary>
    /// 是否已缓存过原始位置
    /// </summary>
    private bool isOriginalPosCached;

    /// <summary>
    /// 出现动画-从下方弹出（位置上滑 OutCubic + 缩放 0→1 OutBack 回弹），快速灵动
    /// </summary>
    /// <param name="delay">延迟时间，用于多卡片错位出现</param>
    public void AnimForShow(float delay = 0f)
    {
        var rt = (RectTransform)transform;
        if (!isOriginalPosCached)
        {
            originalAnchoredPos = rt.anchoredPosition;
            isOriginalPosCached = true;
        }
        rt.DOKill();
        rt.localScale = Vector3.zero;
        rt.anchoredPosition = new Vector2(originalAnchoredPos.x, originalAnchoredPos.y - SHOW_ANIM_RISE_DISTANCE);

        DOTween.Sequence()
            .Join(rt.DOScale(Vector3.one, SHOW_ANIM_DURATION).SetEase(Ease.OutBack, 2.2f))
            .Join(rt.DOAnchorPos(originalAnchoredPos, SHOW_ANIM_DURATION).SetEase(Ease.OutCubic))
            .SetDelay(delay)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }

    /// <summary>
    /// 选中动画-先柔和放大强调、短暂停顿，再回弹缩小消失，节奏更顺滑灵动，结束回调用于触发后续逻辑
    /// </summary>
    public void AnimForSelect(Action onComplete = null)
    {
        transform.DOKill();
        DOTween.Sequence()
            // 柔和放大强调：OutBack 带轻微回弹，时长拉长让动作更舒展
            .Append(transform.DOScale(Vector3.one * 1.18f, 0.22f).SetEase(Ease.OutBack, 1.6f))
            // 短暂停顿，给选中一个"定格"的强调感
            .AppendInterval(0.05f)
            // 平滑缩小消失：InBack 轻度蓄力，overshoot 调小避免突兀
            .Append(transform.DOScale(Vector3.zero, 0.32f).SetEase(Ease.InBack, 1.2f))
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 隐藏动画-缩到 0 消失（未被选中卡片 / 跳过时使用）
    /// </summary>
    public void AnimForHide(Action onComplete = null)
    {
        transform.DOKill();
        transform
            .DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
            .OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = name;
    }

    /// <summary>
    /// 设置详情
    /// </summary>
    /// <param name="details"></param>
    public void SetDetails(string details)
    {
        ui_DetailsText.text = details;
    }

    #region 点击
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        //ContentBG 上的 Button 与 ui_ContentBG(Image) 是同一 GameObject 上的不同组件实例，
        //直接用 == 比较两个不同类型的组件恒为 false，需改为比较所在 GameObject
        if (ui_ContentBG != null && viewButton.gameObject == ui_ContentBG.gameObject)
        {
            OnClickForSelect();
        }
    }

    public void OnClickForSelect()
    {
        var targetUI = UIHandler.Instance.GetUI<UIFightAbyssalBlessing>();
        targetUI.OnClickForSelect(this, abyssalBlessingInfo);
    }
    #endregion
}