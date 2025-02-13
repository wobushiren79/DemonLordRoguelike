using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public partial class UIViewFightMainAttCreateProgress : BaseUIView
{
    [Range(0,1)]
    public float progress;

#if UNITY_EDITOR
    private void OnValidate()
    {
        LogUtil.Log("OnValidate");
        SetProgress(progress);
    }
#endif

    /// <summary>
    /// …Ë÷√Ω¯∂»
    /// </summary>
    /// <param name="progress"></param>
    public void SetProgress(float progress, float animTime = 0)
    {
        this.progress = progress;
        RectTransform rtfProgress = (RectTransform)ui_CreateProgress.transform;
        RectTransform rtfProgressParent = (RectTransform)ui_CreateProgress.transform.parent;
        float targetX = -(1 - progress / 1f) * rtfProgressParent.sizeDelta.x;
        Vector2 targetSizeDelta = new Vector2(targetX, rtfProgress.sizeDelta.y);

        rtfProgress.DOKill();
        if (animTime > 0)
        {
            rtfProgress
                .DOSizeDelta(targetSizeDelta, animTime)
                .SetEase(Ease.Linear);
        }
        else
        {
            rtfProgress.sizeDelta = targetSizeDelta;
        }
    }
}
