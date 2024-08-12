using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UIViewBaseInfoContent : BaseUIView
{
    public override void Awake()
    {
        this.RegisterEvent(EventsInfo.Coin_Change, RefreshUIData);
        this.RegisterEvent(EventsInfo.Magic_Change, RefreshUIData);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RefreshUIData();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public void RefreshUIData()
    {
        if (ui_Coin.gameObject.activeSelf)
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            if (userData != null)
            {
                SetCoinData(userData.coin);
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
    public void SetCoinData(long coin)
    {
        ui_CoinText.text = $"{coin}";
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
