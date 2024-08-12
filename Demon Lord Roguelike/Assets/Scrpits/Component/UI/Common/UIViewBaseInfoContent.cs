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
    /// ���õ�ǰħ��
    /// </summary>
    public void SetMagicData(int magic)
    {
        ui_MagicText.text = $"{magic}";
    }

    /// <summary>
    /// ���õ�ǰ��ң�ħ����
    /// </summary>
    public void SetCoinData(long coin)
    {
        ui_CoinText.text = $"{coin}";
    }

    /// <summary>
    /// ħ����������
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
