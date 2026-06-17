
using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

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
        gameObject.name = $"{researchInfo.id}";
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
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        int maxLevel = researchInfo.level_max;
        int level = userUnlock.GetUnlockResearchLevelByResearchInfo(researchInfo);
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
        var userUnlock = userData.GetUserUnlockData();
        int unlockLevel = userUnlock.GetUnlockResearchLevelByResearchInfo(researchInfo);
        //未解锁
        if (unlockLevel == 0)
        {
            ui_UIViewBaseResearchItem_MaskUIView.ShowMask();
            //未解锁占位图标仍在 UI 图集中
            IconHandler.Instance.SetUIIcon("ui_unlock_1", ui_Icon);
        }
        else
        {
            ui_UIViewBaseResearchItem_MaskUIView.HideMask();
            SetIcon(researchInfo.icon_res);
            
            //全解锁
            if (unlockLevel == researchInfo.level_max)
            {
                ColorUtility.TryParseHtmlString("#5D19D4", out Color targetColor);
                //ColorUtility.TryParseHtmlString("#DDC420", out Color targetColor);
                ui_Board.color = targetColor;
                ui_Icon.color = targetColor;
            }
            //还有未解锁
            else
            {
                ColorUtility.TryParseHtmlString("#FFFFFF", out Color targetColor);
                ui_Board.color = targetColor;
                ui_Icon.color = targetColor;
            }
        }
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    public void SetIcon(string iconRes)
    {
        IconHandler.Instance.SetResearchIcon(iconRes, ui_Icon);
    }

    /// <summary>
    /// 点击购买
    /// </summary>
    public void OnClickForPay()
    {
        //先检测魔晶够不够
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        int level = userUnlock.GetUnlockResearchLevelByResearchInfo(researchInfo);
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
            //设施解锁(会触发建筑出现动画与镜头切换)才需延迟0.5秒让粒子先展示再切镜头；其他解锁立即刷新
            float delayComplete = ScenePrefabForBase.IsBuildingShowUnlock(researchInfo.unlock_id) ? 0.5f : 0f;
            //先播放节点解锁动画，待动画播完后再提交解锁(AddUnlock 会触发设施出现与镜头切换)，
            //避免设施镜头切换/出现动画与节点解锁动画相互冲突
            AnimForUnlock(() =>
            {
                //添加解锁ID
                userUnlock.AddUnlock(researchInfo.unlock_id, level + 1);
                //立即保存数据(扣费与解锁落盘)
                GameDataHandler.Instance.manager.SaveUserData();
                //刷新数据(整页重建，重画连线)
                var targetUI = UIHandler.Instance.GetUI<UIBaseResearch>();
                targetUI.InitResearchItems(targetUI.researchInfoType);
            }, delayComplete);
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 动画解锁
    /// 先播放节点解锁动画(放大+抖动)，动画结束后播放粒子特效；
    /// 若 delayComplete>0(设施解锁)则延迟该秒数让粒子先展示再解锁屏幕并回调，否则立即解锁并回调。
    /// 由调用方在回调中提交解锁数据并刷新页面(从而在动画后再触发设施出现/镜头切换)
    /// </summary>
    /// <param name="actionComplete">解锁动画播放完成后的回调(提交解锁/刷新页面)</param>
    /// <param name="delayComplete">回调前的延迟秒数；仅设施解锁需要(让粒子展示后再切设施镜头)，其他解锁传0立即回调</param>
    public void AnimForUnlock(Action actionComplete = null, float delayComplete = 0f)
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
            //播放粒子特效
            var targetUI = UIHandler.Instance.GetUI<UIBaseResearch>();
            targetUI.AnimForShowUnlockEffect(transform.position);
            if (delayComplete > 0)
            {
                //设施解锁:延迟让粒子展示后再解锁屏幕并回调，延迟期间保持锁屏不可操作，
                //之后再提交解锁与刷新(触发设施出现/镜头切换)
                DOVirtual.DelayedCall(delayComplete, () =>
                {
                    UIHandler.Instance.HideScreenLock();
                    actionComplete?.Invoke();
                });
            }
            else
            {
                //非设施解锁:立即解锁屏幕并回调刷新
                UIHandler.Instance.HideScreenLock();
                actionComplete?.Invoke();
            }
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