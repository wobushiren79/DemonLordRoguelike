using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class AnimUtil
{
    /// <summary>
    /// 数字变化动画
    /// </summary>
    public static Sequence AnimForUINumberChange(Sequence oldAnim, TextMeshProUGUI textView, long from, long to, float duration, string otherText = null, Action onComplete = null)
    {
        if (oldAnim != null && oldAnim.IsPlaying())
            oldAnim.Complete();
        Sequence animForSacrificeCamera = DOTween.Sequence();
        animForSacrificeCamera.Append(textView.transform.DOPunchScale(Vector3.one * 0.1f, duration));
        animForSacrificeCamera.Join(
            DOTween.To
            (
                () => { return from; },
                (value) => 
                    { 
                        if(otherText != null)
                        {
                            textView.text = string.Format(otherText,value.ToString()); 
                        }
                        else
                        {
                            textView.text = value.ToString(); 
                        }
                    },
                to,
                duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => onComplete?.Invoke()
        ));
        return animForSacrificeCamera;
    }

    public static Sequence AnimForUINumberChange(Sequence oldAnim, TextMeshProUGUI textView, int from, int to, float duration, string otherText = null, Action onComplete = null)
    {
        if (oldAnim != null && oldAnim.IsPlaying())
            oldAnim.Complete();
        Sequence animForSacrificeCamera = DOTween.Sequence();
        animForSacrificeCamera.Append(textView.transform.DOPunchScale(Vector3.one * 0.1f, duration));
        animForSacrificeCamera.Join(
            DOTween.To
            (
                () => { return from; },
                (value) => 
                    { 
                        if(otherText != null)
                        {
                            textView.text = string.Format(otherText,value.ToString()); 
                        }
                        else
                        {
                            textView.text = value.ToString(); 
                        }
                    },
                to,
                duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => onComplete?.Invoke()
        ));
        return animForSacrificeCamera;
    }
}
