using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
        //速度按钮走 RadioButtonView 自身的选中回调（OnClickForButton 只处理普通 Button）
        ui_Speed2_RadioButtonView.SetCallBack(this);
        if (ui_Speed0_RadioButtonView != null)
            ui_Speed0_RadioButtonView.SetCallBack(this);
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

    #region 速度按钮（Speed0 0速暂停 / Speed2 2倍速）
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
        else if (view == ui_Speed0_RadioButtonView)
        {
            OnSpeed0Changed(isSelect);
        }
    }

    /// <summary>
    /// 输入快捷键：F1=切换2倍速(Speed2)，F2=加快进攻节奏(同点击Quick)，Space=切换0速(Speed0)；速度切换走与点击按钮相同的 SetStates 路径
    /// </summary>
    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.F1)
        {
            ToggleSpeed2();
        }
        else if (inputType == InputActionUIEnum.F2)
        {
            TriggerQuickByKey();
        }
        else if (inputType == InputActionUIEnum.Space)
        {
            ToggleSpeed0();
        }
    }

    /// <summary>
    /// 快捷键触发加快进攻节奏（F2）：Quick 按钮隐藏(未解锁研究/非征服模式)时无效
    /// </summary>
    private void TriggerQuickByKey()
    {
        if (ui_Quick == null || !ui_Quick.gameObject.activeInHierarchy)
            return;
        OnClickForQuick();
    }

    /// <summary>
    /// 切换0速（Space 快捷键）；按钮隐藏时无效
    /// </summary>
    private void ToggleSpeed0()
    {
        if (ui_Speed0_RadioButtonView == null || !ui_Speed0_RadioButtonView.gameObject.activeInHierarchy)
            return;
        ui_Speed0_RadioButtonView.SetStates(!ui_Speed0_RadioButtonView.isSelect);
    }

    /// <summary>
    /// 切换2倍速（F1 快捷键）；按钮隐藏(未解锁研究)时无效
    /// </summary>
    private void ToggleSpeed2()
    {
        if (ui_Speed2_RadioButtonView == null || !ui_Speed2_RadioButtonView.gameObject.activeInHierarchy)
            return;
        ui_Speed2_RadioButtonView.SetStates(!ui_Speed2_RadioButtonView.isSelect);
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
        //0速与2倍速互斥：开启2倍速时取消0速选中态（速度由下方 SetGameSpeed 直接设置，无需回调）
        if (isOpen && ui_Speed0_RadioButtonView != null && ui_Speed0_RadioButtonView.isSelect)
            ui_Speed0_RadioButtonView.ChangeStates(false);
        gameFightLogic.SetGameSpeed(isOpen ? GameFightLogic.GAME_SPEED_2X : 1f);
    }

    /// <summary>
    /// 0速开关变化：暂停/恢复战斗游戏速度（0速=游戏时间流速归零，一切走游戏速度的逻辑与生物动画全部冻结）
    /// 与2倍速同款机制，只改 fightData.gameSpeed，不改 Time.timeScale；按钮显隐由「0倍速游戏」研究门控（UIFightMain.RefreshSpeed0ButtonShow）
    /// </summary>
    /// <param name="isOpen">true=开启0速(暂停), false=还原原速</param>
    private void OnSpeed0Changed(bool isOpen)
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
            return;
        //0速与2倍速互斥：开启0速时取消2倍速选中态（速度由下方 SetGameSpeed 直接设置，无需回调）
        if (isOpen && ui_Speed2_RadioButtonView != null && ui_Speed2_RadioButtonView.isSelect)
            ui_Speed2_RadioButtonView.ChangeStates(false);
        gameFightLogic.SetGameSpeed(isOpen ? GameFightLogic.GAME_SPEED_0X : 1f);
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
    /// 设置0速按钮显隐
    /// 0速与世界绑定：仅征服模式且已解锁当前世界当前难度的「0倍速游戏」研究时才显示
    /// </summary>
    /// <param name="isShow">是否显示0速按钮</param>
    public void SetSpeed0ButtonShow(bool isShow)
    {
        if (ui_Speed0_RadioButtonView != null)
            ui_Speed0_RadioButtonView.gameObject.SetActive(isShow);
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

    /// <summary>
    /// 按当前战斗游戏速度同步0速按钮选中态（无回调）
    /// 0速按钮由「0倍速游戏」研究门控显隐，显示后需同步选中态与 fightData.gameSpeed 一致（打开战斗主UI/重载战斗场景(征服BOSS关)后调用）
    /// </summary>
    public void RefreshSpeed0State()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null || ui_Speed0_RadioButtonView == null)
            return;
        ui_Speed0_RadioButtonView.ChangeStates(gameFightLogic.fightData.gameSpeed <= GameFightLogic.GAME_SPEED_0X);
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
