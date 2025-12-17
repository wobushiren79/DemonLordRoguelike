
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public partial class UIViewBaseResearchItem : BaseUIView
{
    public ResearchInfoBean researchInfo;
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
        SetLevel();
        //设置浮窗信息
        ui_BG_PopupButtonCommonView.SetData(researchInfo, PopupEnum.ResearchInfo);
    }

    /// <summary>
    /// 设置等级
    /// </summary>
    public void SetLevel()
    {
        int maxLevel = researchInfo.level_max;
        int level = researchInfo.GetResearchLevel();
        if (level == maxLevel || level == 0)
        {
            ui_Level.gameObject.SetActive(false);
        }
        else
        {
            ui_Level.gameObject.SetActive(true);
            ui_Level.text = $"{level}";
        }
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
        var userUnlockData = userData.GetUserUnlockData();
        bool isUnlock = userUnlockData.CheckIsUnlock(researchInfo.unlock_id);
        if (isUnlock)
        {
            ui_UIViewBaseResearchItem_MaskUIView.HideMask();
            SetIcon(researchInfo.icon_res);
        }
        else
        {
            ui_UIViewBaseResearchItem_MaskUIView.ShowMask();
            SetIcon("ui_unlock_1");
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
        var userUnlock = userData.GetUserUnlockData();
        int level = researchInfo.GetResearchLevel();
        //检测是否已经解锁
        if (level == researchInfo.level_max)
        {
            return;
        }
        //检测魔晶够不够
        long payCrystal = researchInfo.GetPayCrystal(level + 1);
        if (!userData.CheckHasCrystal(payCrystal, isHint: true))
        {
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(62001), payCrystal);
        dialogData.actionSubmit = (view, data) =>
        {
            //扣除魔晶
            if (!userData.CheckHasCrystal(payCrystal, isHint: true, isAddCrystal: true))
            {
                return;
            }
            //添加解锁ID
            userData.GetUserUnlockData().AddUnlock(researchInfo.unlock_id + level);
            //播放解锁动画
            AnimForUnlock();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 动画解锁
    /// </summary>
    public void AnimForUnlock()
    {
        UIHandler.Instance.ShowScreenLock();
        ClearAnim();
        //先隐藏mask
        ui_UIViewBaseResearchItem_MaskUIView.HideMask();
        //UI放大
        animForUnlock = DOTween.Sequence();
        animForUnlock.Append(transform.DOScale(Vector3.one * 2f, 1));
        animForUnlock.Join(transform.DOShakePosition(1, 25, 50));
        animForUnlock.Append(transform.DOScale(Vector3.one, 0.1f));
        animForUnlock.OnComplete(() =>
        {
            ui_UIViewBaseResearchItem_MaskUIView.HideMask();
            UIHandler.Instance.HideScreenLock();
            //播放粒子特效
            var targetUI = UIHandler.Instance.GetUI<UIBaseResearch>();
            targetUI.AnimForShowUnlockEffect(transform.position);
            //刷新数据
            targetUI.InitResearchItems(targetUI.researchInfoType);
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