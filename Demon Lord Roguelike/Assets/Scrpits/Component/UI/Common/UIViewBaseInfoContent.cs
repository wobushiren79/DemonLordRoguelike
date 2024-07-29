using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UIViewBaseInfoContent : BaseUIView
{
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
