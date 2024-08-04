using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGashaponMachine : BaseUIComponent
{
    protected List<UIViewGashaponCardGroupItem> listView = new List<UIViewGashaponCardGroupItem>();

    //单个卡片所占的比例宽
    protected float itemCardGroupViewW;
    //当前选中的卡住下标
    protected int currentSelectCardGroupIndex = 0;

    public override void Awake()
    {
        base.Awake();
        ui_ScrollView.onValueChanged.RemoveAllListeners();
        ui_ScrollView.onValueChanged.AddListener(OnScrollViewChange);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        currentSelectCardGroupIndex = -1;
        SetCardGroupList();
        OnScrollViewChange(Vector2.zero);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_Content.DestroyAllChild(1);
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BTOne)
        {
            StartGashaponMachine(1);
        }
        else if (viewButton == ui_BTTen)
        {
            StartGashaponMachine(10);
        }
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();

        SetBaseInfo(userData.coin);
    }

    /// <summary>
    /// 开始扭蛋游戏
    /// </summary>
    /// <param name="num"></param>
    public void StartGashaponMachine(int num)
    {
        GashaponMachineBean gashaponMachine = new GashaponMachineBean();
        gashaponMachine.gashaponNum = num;
        GameHandler.Instance.StartGashaponMachine(gashaponMachine);
    }

    /// <summary>
    /// 设置卡组列表
    /// </summary>
    public void SetCardGroupList()
    {
        ui_Content.DestroyAllChild(1);
        listView.Clear();
        var allData = CardGroupInfoCfg.GetAllData();
        foreach (var item in allData)
        {
            var itemData = item.Value;
            GameObject objItem = Instantiate(ui_Content.gameObject, ui_ViewGashaponCardGroupItem.gameObject);
            var itemView = objItem.GetComponent<UIViewGashaponCardGroupItem>();
            listView.Add(itemView);
        }
        if (listView.Count <= 1)
        {
            itemCardGroupViewW = 1;
        }
        else
        {
            itemCardGroupViewW = 1f / (listView.Count - 1);
        }

    }

    /// <summary>
    /// 设置基础信息
    /// </summary>
    public void SetBaseInfo(long coin)
    {
        ui_ViewBaseInfoContent.SetCoinData(coin);
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 滑动监听
    /// </summary>
    public void OnScrollViewChange(Vector2 targetPos)
    {
        int targetIndex = Mathf.FloorToInt((targetPos.x + (itemCardGroupViewW / 2f)) / itemCardGroupViewW);
        if (targetIndex < 0)
            targetIndex = 0;
        if (targetIndex > listView.Count - 1)
            targetIndex = listView.Count - 1;
        //当下标变化时 更新UI
        if (currentSelectCardGroupIndex != targetIndex)
        {
            currentSelectCardGroupIndex = targetIndex;
            for (int i = 0; i < listView.Count; i++)
            {
                var itemView = listView[i];
                if (i == targetIndex)
                {
                    itemView.SetSelectState(true);
                }
                else
                {
                    itemView.SetSelectState(false);
                }
            }
        }

    }
}
