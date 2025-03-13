using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UIViewBaseInfoContent : BaseUIView
{
    protected Tween animForCoinChange;//魔晶变化动画
    protected Tween animForCoinScale;//魔晶变化动画

    public override void Awake()
    {
        this.RegisterEvent(EventsInfo.Coin_Change, RefreshUIData);
        this.RegisterEvent(EventsInfo.Magic_Change, RefreshUIData);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RefreshUIData(false);
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public void RefreshUIData()
    {
        RefreshUIData(true);
    }

    public void RefreshUIData(bool isAnim)
    {
        if (ui_Coin.gameObject.activeSelf)
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            if (userData != null)
            {
                SetCoinData(userData.coin, isAnim);
            }
        }
        if (ui_Magic.gameObject.activeSelf)
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            if (gameFightLogic != null)
            {
                SetMagicData(gameFightLogic.fightData.currentMagic);
            }
        }
    }

    /// <summary>
    /// 设置当前魔力
    /// </summary>
    public void SetMagicData(int magic)
    {
        ui_MagicText.text = $"{magic}";
    }

    /// <summary>
    /// 设置当前金币（魔晶）
    /// </summary>
    public void SetCoinData(long coin, bool isAnim = true)
    {
        ClearAnim();
        if (isAnim)
        {
            AnimateNumber(ui_CoinText, long.Parse(ui_CoinText.text), coin, 1f);
        }
        else
        {
            ui_CoinText.text = $"{coin}";
        }
    }

    /// <summary>
    /// 清理动画
    /// </summary>
    public void ClearAnim()
    {
        animForCoinChange?.Kill();
        animForCoinScale?.Kill();
        ui_CoinText.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 数字变化动画
    /// </summary>
    public void AnimateNumber(TextMeshProUGUI textView, long from, long to, float duration, Action onComplete = null)
    {
        animForCoinScale = textView.transform.DOPunchScale(Vector3.one * 0.1f, duration);
        animForCoinChange = DOTween.To
        (
            () => { return from; },
            (value) => { textView.text = value.ToString(); },
            to,
            duration)
        .SetEase(Ease.Linear)
        .OnComplete(() => onComplete?.Invoke()
        );
    }

    /// <summary>
    /// 魔力不够动画
    /// </summary>
    public void PlayAnimForMagicNoEnough()
    {
        ui_MagicText.DOKill();
        ui_MagicText.DOColor(Color.red, 0.05f).SetLoops(6, LoopType.Yoyo).OnComplete(() =>
        {
            ui_MagicText.color = Color.white;
        });
    }
}
