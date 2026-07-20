using DG.Tweening;
using UnityEngine;

//战斗卡片特殊设置 - 动画相关(动画参数 / Tween 句柄 / 创建·选择·避让动画)
public partial class UIViewCreatureCardItemForFight
{
    [Header("卡片创建动画延迟时间")]
    public float animCardCreateDelayTime = 0.05f;
    [Header("卡片创建动画时间")]
    public float animCardCreateTimeType2 = 0.4f;
    [Header("卡片创建动画缓动函数")]
    public Ease animCardCreateEase = Ease.OutBack;

    [Header("卡片选择动画进入时间")]
    public float animCardSelectStartTime = 0.25f;
    [Header("卡片选择动画缓动函数-进入")]
    public Ease animCardSelectStart = Ease.OutBack;
    [Header("卡片选择动画放大参数")]
    public float animCardSelectStartScale = 1.6f;

    [Header("卡片选择动画退出时间")]
    public float animCardSelectEndTime = 0.5f;
    [Header("卡片选择动画缓动函数-退出")]
    public Ease animCardSelectEnd = Ease.OutBack;

    protected Tween animForCreate;//创建卡片动画
    protected Tween animForSelectStart;//选择卡片动画
    protected Tween animForSelectEnd;//选择卡片动画
    protected Tween animForSelectKeepStart;//选择卡片避让动画
    protected Tween animForSelectKeepEnd;//选择卡片避让动画

    #region 动画相关
    /// <summary>
    /// 创建动画(出现弹入统一走 UIViewCreatureCardItem.AnimForCardShow,此处仅做清理与参数透传)
    /// </summary>
    /// <param name="index">卡片序号，用于错开延迟</param>
    public void AnimForCreateShow(int index)
    {
        ClearAnim();
        //先归位到 originalCardPos(SetData 只记坐标不动位置,InitCreatureCardList 流程此时 transform 还在模板原位),再以当前本地坐标为落位目标
        rectTransform.anchoredPosition = cardData.originalCardPos;
        animForCreate = AnimForCardShow(rectTransform.localPosition, index, animCardCreateDelayTime, animCardCreateTimeType2, animCardCreateEase);
    }

    /// <summary>
    /// 选择动画-进入：鼠标进入时放大本卡(由 OnPointerEnter 调用)
    /// </summary>
    public void PlaySelectEnterAnim()
    {
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
    }

    /// <summary>
    /// 选择动画-退出：鼠标离开时还原本卡缩放(由 OnPointerExit 调用)
    /// </summary>
    public void PlaySelectExitAnim()
    {
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
    }

    /// <summary>
    /// 避让动画-让位：相邻卡片向两侧让出空间(由 EventForForSelectKeep 调用)
    /// </summary>
    /// <param name="offsetPos">相对原位置的让位偏移</param>
    public void PlaySelectKeepAnim(Vector2 offsetPos)
    {
        KillAnimForKeep();
        animForSelectKeepStart = rectTransform
            .DOAnchorPos(cardData.originalCardPos + offsetPos, animCardSelectStartTime)
            .SetEase(animCardSelectStart);
    }

    /// <summary>
    /// 避让动画-归位：让位结束后回到原位置(由 EventForForSelectKeep 调用)
    /// </summary>
    public void PlaySelectKeepReturnAnim()
    {
        KillAnimForKeep();
        animForSelectKeepEnd = rectTransform
            .DOAnchorPos(cardData.originalCardPos, animCardSelectEndTime)
            .SetEase(animCardSelectEnd);
    }

    /// <summary>
    /// 清除所有动画
    /// </summary>
    public void ClearAnim()
    {
        if (animForCreate != null && animForCreate.IsPlaying())
        {
            animForCreate.Complete();
        }
        if (animForSelectStart != null && animForSelectStart.IsPlaying())
        {
            animForSelectStart.Complete();
        }
        if (animForSelectEnd != null && animForSelectEnd.IsPlaying())
        {
            animForSelectEnd.Complete();
        }
        if (animForSelectKeepStart != null && animForSelectKeepStart.IsPlaying())
        {
            animForSelectKeepStart.Complete();
        }
        if (animForSelectKeepEnd != null && animForSelectKeepEnd.IsPlaying())
        {
            animForSelectKeepEnd.Complete();
        }
    }

    /// <summary>
    /// Keep动画关闭
    /// </summary>
    public void KillAnimForKeep()
    {
        if (animForSelectKeepStart != null && animForSelectKeepStart.IsPlaying())
            animForSelectKeepStart.Kill();
        if (animForSelectKeepEnd != null && animForSelectKeepEnd.IsPlaying())
            animForSelectKeepEnd.Kill();
    }

    /// <summary>
    /// 关闭选择动画
    /// </summary>
    public void KillAnimForSelect()
    {
        if (animForSelectStart != null && animForSelectStart.IsPlaying())
            animForSelectStart.Kill();
        if (animForSelectEnd != null && animForSelectEnd.IsPlaying())
            animForSelectEnd.Kill();
    }
    #endregion
}
