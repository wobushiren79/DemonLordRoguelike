
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public partial class UIViewBaseResearchItem : BaseUIView
{
    protected ResearchInfoBean researchInfo;
    protected Vector2 itemPosition;
    protected Sequence animForUnlock;//解锁动画
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BG_Button)
        {
            OnClickForPay();
        }
    }

    public override void OnDestroy()
    {       
        ClearAnim();
        base.OnDestroy();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ResearchInfoBean researchInfo)
    {
        this.researchInfo = researchInfo;
        itemPosition = new Vector2(researchInfo.position_x, researchInfo.position_y);
        SetPosition(itemPosition);
        SetState();
        SetIcon(researchInfo.icon_res);
    }

    /// <summary>
    /// 设置位置
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public void SetState()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        bool isUnlock = userData.GetUserUnlockData().CheckIsUnlock(researchInfo.unlock_id);
        if (isUnlock)
        {
            ui_UIViewBaseResearchItem_MaskUIView.HideMask();
        }
        else
        {
            ui_UIViewBaseResearchItem_MaskUIView.ShowMask();
        }
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    public void SetIcon(string iconRes)
    {
        IconHandler.Instance.SetUIIcon(iconRes, ui_Icon);
    }

    /// <summary>
    /// 点击购买
    /// </summary>
    public void OnClickForPay()
    {
        //先检测魔晶够不够
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (!userData.CheckHasCrystal(researchInfo.pay_crystal, isHint: true))
        {
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(72001), researchInfo.pay_crystal);
        dialogData.actionSubmit = (view, data) =>
        {
            //扣除魔晶
            if (!userData.CheckHasCrystal(researchInfo.pay_crystal, isHint: true, isAddCrystal: true))
            {
                return;
            }
            //解锁成就
            userData.GetUserUnlockData().AddUnlock(researchInfo.unlock_id);
            //播放解锁动画
            AnimForUnlock();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
    public float animScaleTime1 = 3;
    public float animScaleTime2 = 0.2f;
    public float animShakeTime = 3;
    public float shakeS = 1;
    public int vibrato = 10;
    public float randomness = 90f;
    /// <summary>
    /// 动画解锁
    /// </summary>
    public void AnimForUnlock()
    {
        UIHandler.Instance.ShowScreenLock();
        ClearAnim();
        //UI放大
        animForUnlock = DOTween.Sequence();
        animForUnlock.Append(transform.DOScale(Vector3.one * 2f, animScaleTime1));
        animForUnlock.Join(transform.DOShakePosition(animShakeTime,shakeS,vibrato,randomness));
        animForUnlock.Append(transform.DOScale(Vector3.one, animScaleTime2));
        animForUnlock.OnComplete(() =>
        {
            ui_UIViewBaseResearchItem_MaskUIView.HideMask();
            UIHandler.Instance.HideScreenLock();
        });
    }

    /// <summary>
    /// 清理动画数据
    /// </summary>
    public void ClearAnim()
    {
        transform.localScale = Vector3.one;
        if (animForUnlock != null)
        {
            animForUnlock.Kill();
        }
    }
}