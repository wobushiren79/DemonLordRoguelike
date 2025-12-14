

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public partial class UICreatureSacrifice : BaseUIComponent
{    
    //进度文本动画
    Sequence animForSuccessRateText;
    //当前选择的生物
    public List<CreatureBean> listSelectCreature = new List<CreatureBean>();
    //展示的生物数据
    public List<CreatureBean> listCreatureData = new List<CreatureBean>();

    public override void OpenUI()
    {
        base.OpenUI();
        InitCreaturekData();
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        RefreshUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewCreatureCardList.CloseUI();
    }

    /// <summary>
    /// 刷新UI
    /// </summary>
    public void RefreshUI()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var limmitData = userData.GetUserLimmitData();
        SetLimmitSelect(listSelectCreature.Count, limmitData.sacrificeMax);
        SetSuccessRate(listSelectCreature.Count * 0.2f);
    }

    /// <summary>
    /// 初始化卡片数据
    /// </summary>
    public void InitCreaturekData()
    {
        CreatureSacrificeLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<CreatureSacrificeLogic>();

        //设置列表
        listSelectCreature.Clear();
        listCreatureData.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            //筛除献祭的生物
            if (creatureData != gameLogic.creatureSacrificeData.targetCreature)
            {
                listCreatureData.Add(creatureData);
            }
        });
        ui_UIViewCreatureCardList.SetData(listCreatureData, CardUseStateEnum.CreatureSacrifice, OnCellChangeForBackpackCreature);
        //设置展示
        ui_UIViewCreatureCardDetails.SetData(gameLogic.creatureSacrificeData.targetCreature);
    }

    /// <summary>
    /// 设置选择上限
    /// </summary>
    public void SetLimmitSelect(int curSelectNum, int maxSelectNum)
    {
        ui_LimmitText.text = $"{curSelectNum}/{maxSelectNum}";
    }

    /// <summary>
    /// 设置成功率
    /// </summary>
    public void SetSuccessRate(float successRate)
    {
        int targetPercentage = (int)MathUtil.GetPercentage(successRate, 2);
        //播放进度动画
        ui_SuccessRateProgress.DOFillAmount(successRate, 0.5f);
        //播放文本变化动画
        animForSuccessRateText = AnimUtil.AnimForUINumberChange(animForSuccessRateText, ui_SuccessRateText, long.Parse(ui_SuccessRateText.text.Replace("%", "")), (long)targetPercentage, 0.5f, "{0}%");
        ui_SuccessRateTextTitle.text = $"{TextHandler.Instance.GetTextById(60002)}";
    }

    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreature(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (listSelectCreature.Contains(itemData))
        {
            itemView.SetCardState(CardStateEnum.CreatureSacrificeSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureSacrificeNoSelect);
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnStart)
        {
            OnClickForStart();
        }
    }


    #region 点击事件
    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        CreatureSacrificeLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<CreatureSacrificeLogic>();
        gameLogic.EndGame();
    }

    /// <summary>
    /// 点击开始
    /// </summary>
    public void OnClickForStart()
    {
        if (listSelectCreature.IsNull())
        {
            UIHandler.Instance.ToastHint<ToastView>(TextHandler.Instance.GetTextById(61001));
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(61003), $"{MathUtil.GetPercentage(0.2f, 2)}%");
        dialogData.actionSubmit = (view, data) =>
        {
            //开始献祭
            CreatureSacrificeLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<CreatureSacrificeLogic>();
            gameLogic.StartSacrifice();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);

    }
    #endregion


    #region 事件
    /// <summary>
    /// 选择
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        if (selectItemView.cardData.cardState == CardStateEnum.CreatureSacrificeNoSelect)
        {
            //上限检测
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            var limmitData = userData.GetUserLimmitData();
            if (listSelectCreature.Count >= limmitData.sacrificeMax)
            {
                UIHandler.Instance.ToastHint<ToastView>(TextHandler.Instance.GetTextById(61002));
                return;
            }
            //添加
            if (!listSelectCreature.Contains(selectCreatureData))
            {
                listSelectCreature.Add(selectCreatureData);
            }
        }
        else
        {
            //删除
            if (listSelectCreature.Contains(selectCreatureData))
            {
                listSelectCreature.Remove(selectCreatureData);
            }
        }
        ui_UIViewCreatureCardList.RefreshAllCard();
        TriggerEvent(EventsInfo.CreatureSacrifice_SelectCreature, listSelectCreature);
        //刷新UI
        RefreshUI();
    }
    #endregion
}