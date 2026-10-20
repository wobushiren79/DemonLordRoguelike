using DG.Tweening;
using System;
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
    /// <param name="actionForComplete">本卡落位完成回调(一般只给最后一张卡传,用于广播整组动画结束)</param>
    public void AnimForCreateShow(int index, Action actionForComplete = null)
    {
        ClearAnim();
        //先归位到 originalCardPos(SetData 只记坐标不动位置,InitCreatureCardList 流程此时 transform 还在模板原位),再以当前本地坐标为落位目标
        rectTransform.anchoredPosition = cardData.originalCardPos;
        animForCreate = AnimForCardShow(rectTransform.localPosition, index, animCardCreateDelayTime, animCardCreateTimeType2, animCardCreateEase, actionForComplete);
    }

    /// <summary>
    /// 创建动画(由下方弹入)是否播放中,含错开延迟阶段。
    /// <para>重排布局时须跳过直接改坐标:位移 tween 的起点在真正启动时才捕获,延迟期间被外部写成目标位会让动画变成"原地→原地"(只剩缩放没有上移)。</para>
    /// </summary>
    /// <returns>是否正在播创建动画</returns>
    public bool IsPlayingCreateAnim()
    {
        return animForCreate != null && animForCreate.IsActive();
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
    /// <param name="actionForComplete">复原动画播完回调(卡片层级须等缩回原始尺寸后再还原，否则放大状态会被邻居卡遮住并抢走悬停)</param>
    public void PlaySelectExitAnim(Action actionForComplete = null)
    {
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd)
                //KillAnimForSelect 未播完就杀掉的 tween 不会触发 OnComplete(重新悬停时即走此路径)
                .OnComplete(() => actionForComplete?.Invoke());
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
