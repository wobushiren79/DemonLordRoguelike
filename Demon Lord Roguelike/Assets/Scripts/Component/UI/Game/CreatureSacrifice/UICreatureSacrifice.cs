

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class UICreatureSacrifice : BaseUIComponent
{
    /// <summary>
    /// 成功率进度条颜色 0%~20% (红)
    /// </summary>
    protected static readonly Color SuccessRateColorVeryLow = ColorUtil.ParseHtmlString("#C0392B");
    /// <summary>
    /// 成功率进度条颜色 20%~40% (橙)
    /// </summary>
    protected static readonly Color SuccessRateColorLow = ColorUtil.ParseHtmlString("#E67E22");
    /// <summary>
    /// 成功率进度条颜色 40%~60% (黄)
    /// </summary>
    protected static readonly Color SuccessRateColorMedium = ColorUtil.ParseHtmlString("#F1C40F");
    /// <summary>
    /// 成功率进度条颜色 60%~80% (浅绿)
    /// </summary>
    protected static readonly Color SuccessRateColorHigh = ColorUtil.ParseHtmlString("#2ECC71");
    /// <summary>
    /// 成功率进度条颜色 80%~100% (蓝)
    /// </summary>
    protected static readonly Color SuccessRateColorVeryHigh = ColorUtil.ParseHtmlString("#3498DB");

    //进度文本动画
    Sequence animForSuccessRateText;
    //当前选择的生物
    public List<CreatureBean> listSelectCreature = new List<CreatureBean>();
    //展示的生物数据
    public List<CreatureBean> listCreatureData = new List<CreatureBean>();

    public override void OpenUI()
    {
        base.OpenUI();
        //关闭基地移动控制(与其它基地子界面一致):避免献祭界面及后续献祭动画期间仍能控制角色移动
        GameControlHandler.Instance.SetBaseControl(false);
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
        //选择上限 = 基础上限 + 献祭祭品数量研究等级
        int sacrificeMax = userData.GetUserUnlockData().GetUnlockSacrificeMax();
        SetLimmitSelect(listSelectCreature.Count, sacrificeMax);
        //按真实公式计算成功率(保底+祭品)
        SetSuccessRate(GetCurrentSuccessRate());
    }

    /// <summary>
    /// 获取当前选择下的献祭成功率(保底 + 祭品,已截顶 0~1)
    /// </summary>
    /// <returns>0~1 的成功率</returns>
    public float GetCurrentSuccessRate()
    {
        CreatureSacrificeLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<CreatureSacrificeLogic>();
        var targetCreature = gameLogic.creatureSacrificeData.targetCreature;
        return CreatureUtil.GetSacrificeSuccessRate(targetCreature, listSelectCreature);
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
        userData.GetUserBackpackCreatureData().listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
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
        //根据成功率区间渐变进度条颜色
        ui_SuccessRateProgress.DOColor(GetSuccessRateColor(successRate), 0.5f);
        //播放文本变化动画
        animForSuccessRateText = AnimUtil.AnimForUINumberChange(animForSuccessRateText, ui_SuccessRateText, long.Parse(ui_SuccessRateText.text.Replace("%", "")), (long)targetPercentage, 0.5f, "{0}%");
        ui_SuccessRateTextTitle.text = $"{TextHandler.Instance.GetTextById(60002)}";
    }

    /// <summary>
    /// 获取成功率对应的进度条颜色(按成功率分5段: 0-20红 20-40橙 40-60黄 60-80浅绿 80-100蓝)
    /// </summary>
    /// <param name="successRate">0~1 的成功率</param>
    /// <returns>该区间对应的进度条颜色</returns>
    public Color GetSuccessRateColor(float successRate)
    {
        if (successRate < 0.2f)
            return SuccessRateColorVeryLow;
        if (successRate < 0.4f)
            return SuccessRateColorLow;
        if (successRate < 0.6f)
            return SuccessRateColorMedium;
        if (successRate < 0.8f)
            return SuccessRateColorHigh;
        return SuccessRateColorVeryHigh;
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
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61001));
            return;
        }
        //当前真实成功率
        float successRate = GetCurrentSuccessRate();
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(61003), $"{MathUtil.GetPercentage(successRate, 2)}%");
        dialogData.actionSubmit = (view, data) =>
        {
            //开始献祭: 先把选中的祭品传给逻辑层,再开始
            CreatureSacrificeLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<CreatureSacrificeLogic>();
            gameLogic.creatureSacrificeData.fodderCreatures = new List<CreatureBean>(listSelectCreature);
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
            //上限检测：基础上限 + 献祭祭品数量研究等级
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            int sacrificeMax = userData.GetUserUnlockData().GetUnlockSacrificeMax();
            if (listSelectCreature.Count >= sacrificeMax)
            {
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61002));
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