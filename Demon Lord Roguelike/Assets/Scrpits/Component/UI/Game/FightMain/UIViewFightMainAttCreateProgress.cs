using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public void SetProgress(float progress)
    {
        this.progress = progress;
        RectTransform rtfProgress = (RectTransform)ui_CreateProgress.transform;
        RectTransform rtfProgressParent = (RectTransform)ui_CreateProgress.transform.parent;
        float targetX = -(1 - progress / 1f) * rtfProgressParent.sizeDelta.x;
        rtfProgress.sizeDelta = new Vector2(targetX, rtfProgress.sizeDelta.y);
    }
}
