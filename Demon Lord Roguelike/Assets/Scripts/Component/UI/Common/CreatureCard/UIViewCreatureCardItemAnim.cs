using DG.Tweening;
using System;
using UnityEngine;

//生物卡片通用动画(出现动画两套:战斗发牌用 AnimForCardShow 从下方弹入,阵容管理用 AnimForCardShowByFadeScale 原地淡入放大)
public partial class UIViewCreatureCardItem
{
    #region 动画相关
    //淡入淡出用 CanvasGroup(运行时懒加, prefab 无需配置)
    protected CanvasGroup canvasGroupForAnim;

    /// <summary>
    /// 卡片出现动画(战斗发牌专用,由下方弹入:位移 OutBack + 缩放过冲回弹,启动时与落位后各播放一次卡片音效,每张卡各自播放)
    /// </summary>
    /// <param name="targetLocalPos">落位目标本地坐标</param>
    /// <param name="index">卡片序号,用于错开延迟</param>
    /// <param name="delayTime">每张卡片错开延迟时间</param>
    /// <param name="moveTime">弹入移动时间</param>
    /// <param name="ease">弹入缓动函数</param>
    /// <param name="actionForComplete">本卡落位完成回调(在音效播放后触发)</param>
    /// <returns>动画Tween(便于调用方管理句柄)</returns>
    public Tween AnimForCardShow(Vector3 targetLocalPos, int index, float delayTime, float moveTime, Ease ease, Action actionForComplete = null)
    {
        //起点=目标位下方200px;DOLocalMove 的起点在 tween 真正启动时才捕获,延迟期间的外部写坐标会污染起点,故延迟结束时刻重新吸附一次
        Vector3 startLocalPos = targetLocalPos + new Vector3(0, -200f, 0);
        rectTransform.localPosition = startLocalPos;
        rectTransform.localScale = Vector3.one;
        //SetTarget(rectTransform)：让本序列可被 transform.DOKill() 整体杀掉(含延迟中的音效回调)，避免切页签/复用卡片时旧序列残留
        return DOTween.Sequence()
            .SetTarget(rectTransform)
            .AppendInterval(index * delayTime)
            .AppendCallback(() =>
            {
                //重新吸附起点(防外部布局刷新在延迟期间改坐标,导致位移变"原地→原地"),并播放卡片音效
                rectTransform.localPosition = startLocalPos;
                AudioHandler.Instance.PlaySound(AudioEnum.sound_card_7);
            })
            .Append(rectTransform.DOLocalMove(targetLocalPos, moveTime).SetEase(ease))
            .Join(rectTransform.DOScale(1.15f, moveTime * 0.6f).SetEase(Ease.OutQuad))
            .Append(rectTransform.DOScale(1f, moveTime * 0.4f).SetEase(Ease.InOutQuad))
            //动画落位完成后播放卡片音效(每张卡各自播放)
            .OnComplete(() =>
            {
                AudioHandler.Instance.PlaySound(AudioEnum.sound_card_1);
                actionForComplete?.Invoke();
            });
    }

    /// <summary>
    /// 卡片原地出现动画(淡入+放大):先吸附到目标位置再淡入放大,不产生任何位移、不会遮挡下方列表(阵容管理切换阵容用)
    /// <para>缩放 startScale→overshootScale→1(前半段放大过冲、后半段回落),透明度 0→1 同步淡入</para>
    /// <para>音效时机与 AnimForCardShow 一致:错开延迟结束播 sound_card_7、落位播 sound_card_1</para>
    /// </summary>
    /// <param name="targetLocalPos">落位目标本地坐标</param>
    /// <param name="index">卡片序号,用于错开延迟</param>
    /// <param name="delayTime">每张卡片错开延迟时间</param>
    /// <param name="showTime">放大动画总时长(60%放大过冲 + 40%回落)</param>
    /// <param name="fadeTime">淡入时长</param>
    /// <param name="startScale">起始缩放(小于1,从缩小状态弹出)</param>
    /// <param name="overshootScale">过冲缩放峰值(大于1,随后回落至1)</param>
    /// <param name="actionForComplete">本卡落位完成回调(在音效播放后触发)</param>
    /// <param name="maxDelay">错开延迟上限(负数=不封顶,卡片多时用上限避免末尾卡等待过久)</param>
    /// <returns>动画Tween(便于调用方管理句柄)</returns>
    public Tween AnimForCardShowByFadeScale(Vector3 targetLocalPos, int index, float delayTime, float showTime, float fadeTime,
        float startScale = 0.88f, float overshootScale = 1.08f, Action actionForComplete = null, float maxDelay = -1f)
    {
        //原地出现:直接吸附目标位,杜绝从下方 -200px 弹入盖住背包列表
        rectTransform.localPosition = targetLocalPos;
        rectTransform.localScale = new Vector3(startScale, startScale, 1f);
        CanvasGroup targetCanvasGroup = GetOrAddCanvasGroupForAnim();
        targetCanvasGroup.alpha = 0f;
        //错开延迟按 index*delayTime 计算,maxDelay>=0 时封顶(卡片多时不会拖沓)
        float delay = maxDelay >= 0f ? Mathf.Min(index * delayTime, maxDelay) : index * delayTime;
        //SetTarget(rectTransform)：让本序列可被 transform.DOKill() 整体杀掉(含延迟中的音效回调与嵌套淡入tween)
        return DOTween.Sequence()
            .SetTarget(rectTransform)
            .AppendInterval(delay)
            //错开延迟结束、动画启动时播放卡片音效(每张卡各自播放)
            .AppendCallback(() => AudioHandler.Instance.PlaySound(AudioEnum.sound_card_7))
            .Join(rectTransform.DOScale(overshootScale, showTime * 0.6f).SetEase(Ease.OutQuad))
            .Join(targetCanvasGroup.DOFade(1f, fadeTime).SetEase(Ease.OutQuad))
            .Insert(delay + showTime * 0.6f, rectTransform.DOScale(Vector3.one, showTime * 0.4f).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                //终态强制复位:池复用/后续动画开始前保证不透明、原始尺寸
                targetCanvasGroup.alpha = 1f;
                rectTransform.localScale = Vector3.one;
                AudioHandler.Instance.PlaySound(AudioEnum.sound_card_1);
                actionForComplete?.Invoke();
            });
    }

    /// <summary>
    /// 复位卡片动画状态(杀掉位移/缩放/淡入序列,缩放与透明度回满),入池、复用或重新播动画前调用;不改动坐标
    /// </summary>
    public void ResetCardAnimStateForReuse()
    {
        //先杀 transform 上的序列(含延迟中的音效回调与嵌套淡入tween)
        rectTransform.DOKill();
        //双保险:清掉可能独立作用于 CanvasGroup 的残留tween
        if (canvasGroupForAnim != null)
        {
            canvasGroupForAnim.DOKill();
            canvasGroupForAnim.alpha = 1f;
        }
        rectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// 获取(或运行时补挂)淡入淡出用 CanvasGroup;保持默认 blocksRaycasts=true,不影响卡片点击
    /// </summary>
    /// <returns>卡片上的 CanvasGroup</returns>
    protected CanvasGroup GetOrAddCanvasGroupForAnim()
    {
        if (canvasGroupForAnim == null)
        {
            canvasGroupForAnim = GetComponent<CanvasGroup>();
            if (canvasGroupForAnim == null)
            {
                canvasGroupForAnim = gameObject.AddComponent<CanvasGroup>();
            }
        }
        return canvasGroupForAnim;
    }
    #endregion
}
