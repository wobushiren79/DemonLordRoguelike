using DG.Tweening;
using System;
using UnityEngine;

//生物卡片通用动画(出现弹入动画,战斗发牌与阵容管理初始化共用)
public partial class UIViewCreatureCardItem
{
    #region 动画相关
    /// <summary>
    /// 卡片出现动画(由下方弹入:位移 OutBack + 缩放过冲回弹,启动时与落位后各播放一次卡片音效,每张卡各自播放)
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
        rectTransform.localPosition = targetLocalPos + new Vector3(0, -200f, 0);
        rectTransform.localScale = Vector3.one;
        return DOTween.Sequence()
            .AppendInterval(index * delayTime)
            //错开延迟结束、动画从下方启动时播放卡片音效(每张卡各自播放)
            .AppendCallback(() => AudioHandler.Instance.PlaySound(AudioEnum.sound_card_7))
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
    #endregion
}
