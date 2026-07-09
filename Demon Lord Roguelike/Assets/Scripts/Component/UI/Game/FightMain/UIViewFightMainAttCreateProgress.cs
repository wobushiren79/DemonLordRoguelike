using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public partial class UIViewFightMainAttCreateProgress : BaseUIView
{
    [Range(0, 1)]
    public float progress;

    /// <summary>
    /// 点击 Quick 单次推进的进攻进度比例（总进攻时长的 10%）
    /// </summary>
    private const float QUICK_ADVANCE_RATE = 0.1f;

    /// <summary>
    /// 点击 Quick 后进度条平滑过渡到目标进度的动画时长（秒）——避免瞬间跳变，给一个过渡过程
    /// </summary>
    private const float QUICK_ANIM_TIME = 0.3f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        SetProgress(progress);
    }
#endif

    #region 点击按钮
    /// <summary>
    /// 按钮点击回调
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_Quick)
        {
            OnClickForQuick();
        }
    }

    /// <summary>
    /// 点击 Quick：立即向前推进 10% 进攻进度并生成这段时间的进攻生物；进度平滑过渡，已到 100% 则不再推进
    /// </summary>
    private void OnClickForQuick()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
            return;
        //已到进攻末尾(100%)：点击无效
        if (gameFightLogic.fightData.fightAttackData.GetAttackProgress() >= 1f)
            return;
        //推进并生成对应波次生物，取推进后的最新进度平滑过渡显示
        float newProgress = gameFightLogic.QuickAdvanceAttackCreate(QUICK_ADVANCE_RATE);
        SetProgress(newProgress, animTime: QUICK_ANIM_TIME);
    }
    #endregion

    #region 显隐控制
    /// <summary>
    /// 设置 Quick 按钮显隐
    /// Quick 与世界绑定：仅征服模式且已解锁当前世界的「加快进攻节奏」研究时才显示
    /// </summary>
    /// <param name="isShow">是否显示 Quick 按钮</param>
    public void SetQuickButtonShow(bool isShow)
    {
        if (ui_Quick != null)
            ui_Quick.gameObject.SetActive(isShow);
    }
    #endregion

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
