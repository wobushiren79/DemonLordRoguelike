

using System.Collections.Generic;
using UnityEngine.UI;

public partial class UICreatureSacrifice : BaseUIComponent
{
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
        ui_UIViewCreatureCardList.SetData(listCreatureData, CardUseState.CreatureSacrifice, OnCellChangeForBackpackCreature);
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
    public void OnClickForExit()
    {
        GameHandler.Instance.EndCreatureSacrifice();
    }

    public void OnClickForStart()
    {
        if (listCreatureData.IsNull())
        {
            UIHandler.Instance.ToastHint<ToastView>(TextHandler.Instance.GetTextById(61001));
            return;
        }
        //开始献祭
        CreatureSacrificeLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<CreatureSacrificeLogic>();
        gameLogic.StartSacrifice();
    }
    #endregion


    #region 事件
    /// <summary>
    /// 选择
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var limmitData = userData.GetUserLimmitData();
        if (listSelectCreature.Count >= limmitData.sacrificeMax)
        {
            UIHandler.Instance.ToastHint<ToastView>(TextHandler.Instance.GetTextById(61002));
            return;
        }
        var selectCreatureData = selectItemView.cardData.creatureData;
        if (selectItemView.cardData.cardState == CardStateEnum.CreatureSacrificeNoSelect)
        {
            if (!listSelectCreature.Contains(selectCreatureData))
            {
                listSelectCreature.Add(selectCreatureData);
            }
        }
        else
        {
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