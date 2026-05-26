
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewFightAbyssalBlessingItem : BaseUIView
{
    public AbyssalBlessingInfoBean abyssalBlessingInfo;

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="resolvedBuffInfo">有等级的BUFF时，传入已解析的下一级BuffInfo用于展示；无等级时传null</param>
    public void SetData(AbyssalBlessingInfoBean abyssalBlessingInfo, BuffInfoBean resolvedBuffInfo = null)
    {
        this.abyssalBlessingInfo = abyssalBlessingInfo;
        if (resolvedBuffInfo != null)
        {
            SetName(resolvedBuffInfo.name_language);
            SetDetails(resolvedBuffInfo.content_language);
        }
        else
        {
            SetName(abyssalBlessingInfo.name_language);
            SetDetails(abyssalBlessingInfo.details_language);
        }
        SetIcon(abyssalBlessingInfo.icon_res);
    }

    /// <summary>
    /// 设置图像
    /// </summary>
    public void SetIcon(string iconName)
    {
        IconHandler.Instance.SetAbyssalBlessingIcon(iconName, ui_Icon);
    }

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
    /// 选中动画-先放大强调再缩小消失，结束回调用于触发后续逻辑
    /// </summary>
    public void AnimForSelect(Action onComplete = null)
    {
        transform.DOKill();
        DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one * 1.15f, 0.1f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
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
        if (viewButton == ui_Content)
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