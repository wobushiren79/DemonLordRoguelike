
using DG.Tweening;
using UnityEngine;

public partial class UIViewDialogBossShowItem : BaseUIView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="npcId"></param>
    public void SetData(long npcId)
    {
        var npcInfo = NpcInfoCfg.GetItemData(npcId);
        if (npcInfo != null)
        {
            ui_Name.text = npcInfo.name_language;
            // 设置 Spine
            var creatureData = new CreatureBean(npcInfo);
            if (creatureData != null)
            {
                GameUIUtil.SetCreatureUIForDetails(ui_Icon, null, creatureData, customUISize: 2);
            }
        }
    }

    /// <summary>
    /// 动画展示
    /// </summary>
    /// <param name="timeForAnim"></param>
    /// <param name="state">1出现 0消失</param>
    public void AnimForShow(float timeForAnim, int state)
    {
        Vector2 startPos;
        Vector3 endPos;
        float startAlpha;
        float endAlpha;
        Ease easeType;
        if (state == 1)
        {
            endPos = Vector2.zero;
            startPos = new Vector2(-1000, 0);
            startAlpha = 0;
            endAlpha = 1;
            easeType = Ease.OutExpo;
        }
        else
        {
            endPos = new Vector2(1000, 0);
            startPos = Vector2.zero;
            startAlpha = 1;
            endAlpha = 0;
            easeType = Ease.InExpo;
        }
        ui_BG_RectTransform.anchoredPosition = startPos;
        ui_BG_CanvasGroup.alpha = startAlpha;

        ui_BG_CanvasGroup
            .DOFade(endAlpha, timeForAnim)
            .SetEase(easeType)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);

        ui_BG_RectTransform
            .DOAnchorPos(endPos, timeForAnim)
            .SetEase(easeType)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }
}