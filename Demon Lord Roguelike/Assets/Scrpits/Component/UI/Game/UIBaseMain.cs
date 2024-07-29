using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIBaseMain : BaseUIComponent
{

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
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
    /// 设置基础信息
    /// </summary>
    public void SetBaseInfo(long coin)
    {
        ui_ViewBaseInfoContent.SetCoinData(coin);
    }
}
