
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
    /// 仅"多级升级链"馈赠显示角标并为角标底/角标文本着色；
    /// level&lt;=0（单级可重复）与"单级不可重复"（族内仅 1 级）均隐藏角标，避免误导玩家可升级。
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

        //单级不可重复馈赠（族内仅 1 级、不可升级）隐藏角标，与单级可重复(level<=0)表现一致
        bool show = level > 0 && !AbyssalBlessingInfoCfg.IsSingleLevelOnce(abyssalBlessingInfo);
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
    /// 是否已进入选中/隐藏流程：置位后悬停 enter/exit 不再驱动缩放。
    /// 避免选中时 ShowScreenLock 触发 OnPointerExit，使悬停还原 Tween 与选中动画同时修改卡面缩放。
    /// </summary>
    private bool isSelecting;

    /// <summary>
    /// 鼠标进入时放大卡面并播放悬停音效（已进入选中或隐藏流程时忽略）
    /// </summary>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (isSelecting) return;
        AudioHandler.Instance.PlaySound(AudioEnum.sound_card_7);
        hoverTween?.Kill();
        hoverTween = CardContent
            .DOScale(Vector3.one * HOVER_SCALE, HOVER_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }

    /// <summary>
    /// 鼠标离开时还原卡面大小（已进入选中或隐藏流程时忽略）
    /// </summary>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (isSelecting) return;
        hoverTween?.Kill();
        hoverTween = CardContent
            .DOScale(Vector3.one, HOVER_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
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
    /// 卡面的初始局部坐标，供动画复位使用
    /// </summary>
    private Vector2? contentRestPosition;

    /// <summary>
    /// 动画使用的卡面节点，卡片根节点由横向布局组定位
    /// </summary>
    private RectTransform CardContent => ui_ContentBG.rectTransform;

    /// <summary>
    /// 停止卡面上的所有动画并还原位置、缩放和悬停状态
    /// </summary>
    public void ResetAnimations()
    {
        var content = CardContent;
        contentRestPosition ??= content.anchoredPosition;
        content.DOKill();
        hoverTween = null;
        isSelecting = false;
        content.anchoredPosition = contentRestPosition.Value;
        content.localScale = Vector3.one;
    }

    /// <summary>
    /// 卡面从卡槽下方弹入，错位延迟结束后播放 sound_card_8
    /// </summary>
    /// <param name="delay">延迟时间，用于多卡片错位出现</param>
    public void AnimForShow(float delay = 0f)
    {
        ResetAnimations();
        var content = CardContent;
        Vector2 restPosition = content.anchoredPosition;
        content.localScale = Vector3.zero;
        content.anchoredPosition = restPosition - Vector2.up * SHOW_ANIM_RISE_DISTANCE;

        //标记序列目标，让卡面复用时一并取消延迟中的音效回调。
        DOTween.Sequence()
            .SetTarget(content)
            .Join(content.DOScale(Vector3.one, SHOW_ANIM_DURATION).SetEase(Ease.OutBack, 2.2f))
            .Join(content.DOAnchorPos(restPosition, SHOW_ANIM_DURATION).SetEase(Ease.OutCubic))
            .InsertCallback(0f, () => AudioHandler.Instance.PlaySound(AudioEnum.sound_card_8))
            .SetDelay(delay)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }

    /// <summary>
    /// 选中卡面先放大、短暂停顿，再缩小消失并触发回调
    /// </summary>
    public void AnimForSelect(Action onComplete = null)
    {
        var content = CardContent;
        isSelecting = true;
        content.DOKill();
        hoverTween = null;
        DOTween.Sequence()
            .SetTarget(content)
            .Append(content.DOScale(Vector3.one * 1.18f, 0.22f).SetEase(Ease.OutBack, 1.6f))
            .AppendInterval(0.05f)
            .Append(content.DOScale(Vector3.zero, 0.32f).SetEase(Ease.InBack, 1.2f))
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 卡面缩小消失（用于未选中的卡片及跳过流程）
    /// </summary>
    public void AnimForHide(Action onComplete = null)
    {
        var content = CardContent;
        isSelecting = true;
        content.DOKill();
        hoverTween = null;
        content
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
