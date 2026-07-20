using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public partial class UIViewFightMainAttCreateProgress : BaseUIView, IRadioButtonCallBack
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

    public override void Awake()
    {
        base.Awake();
        //2倍速按钮走 RadioButtonView 自身的选中回调（OnClickForButton 只处理普通 Button）
        ui_Speed2_RadioButtonView.SetCallBack(this);
    }

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

    #region 2倍速按钮
    /// <summary>
    /// RadioButton 选择回调（IRadioButtonCallBack）
    /// </summary>
    /// <param name="view">触发回调的 RadioButtonView</param>
    /// <param name="isSelect">切换后的选中状态</param>
    public void RadioButtonSelected(RadioButtonView view, bool isSelect)
    {
        if (view == ui_Speed2_RadioButtonView)
        {
            OnSpeed2Changed(isSelect);
        }
    }

    /// <summary>
    /// 2倍速开关变化：设置战斗游戏速度
    /// 只改 FightLogic 的游戏时间流速(fightData.gameSpeed)，不改 Time.timeScale；速度挂在 fightData 上仅本场战斗有效，战斗结束自然还原
    /// </summary>
    /// <param name="isOpen">true=开启2倍速, false=还原原速</param>
    private void OnSpeed2Changed(bool isOpen)
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
            return;
        gameFightLogic.SetGameSpeed(isOpen ? GameFightLogic.GAME_SPEED_2X : 1f);
    }

    /// <summary>
    /// 设置2倍速按钮显隐
    /// 2倍速与世界绑定：仅征服模式且已解锁当前世界当前难度的「2倍速游戏」研究时才显示
    /// </summary>
    /// <param name="isShow">是否显示2倍速按钮</param>
    public void SetSpeed2ButtonShow(bool isShow)
    {
        if (ui_Speed2_RadioButtonView != null)
            ui_Speed2_RadioButtonView.gameObject.SetActive(isShow);
    }

    /// <summary>
    /// 按当前战斗游戏速度同步2倍速按钮选中态（无回调）
    /// 打开战斗主UI/重载战斗场景(征服BOSS关)后调用，保证按钮状态与 fightData.gameSpeed 一致
    /// </summary>
    public void RefreshSpeed2State()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null || ui_Speed2_RadioButtonView == null)
            return;
        ui_Speed2_RadioButtonView.ChangeStates(gameFightLogic.fightData.gameSpeed >= GameFightLogic.GAME_SPEED_2X);
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
