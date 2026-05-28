using UnityEngine;
using DG.Tweening;

public partial class UIViewFightMainAttCreateProgress : BaseUIView
{
    [Range(0, 1)]
    public float progress;

#if UNITY_EDITOR
    private void OnValidate()
    {
        SetProgress(progress);
    }
#endif

    /// <summary>
    /// 设置进度
    /// </summary>
    /// <param name="progress">进度值 0~1</param>
    /// <param name="animTime">动画时长，0 表示立即设置</param>
    public void SetProgress(float progress, float animTime = 0)
    {
        this.progress = progress;
        RectTransform rtfParent = (RectTransform)ui_CreateProgress.transform.parent;
        RectTransform rtfEnd = (RectTransform)ui_CreateEnd.transform;
        float endX = progress * rtfParent.rect.width;

        ui_CreateProgress.DOKill();
        rtfEnd.DOKill();
        if (animTime > 0)
        {
            ui_CreateProgress.DOFillAmount(progress, animTime).SetEase(Ease.Linear);
            rtfEnd.DOAnchorPosX(endX, animTime).SetEase(Ease.Linear);
        }
        else
        {
            ui_CreateProgress.fillAmount = progress;
            Vector2 endPos = rtfEnd.anchoredPosition;
            endPos.x = endX;
            rtfEnd.anchoredPosition = endPos;
        }
    }
}
