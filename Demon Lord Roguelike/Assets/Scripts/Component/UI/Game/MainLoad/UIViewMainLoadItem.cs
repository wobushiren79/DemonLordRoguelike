
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIViewMainLoadItem : BaseUIView
{
    protected int userDataIndex;
    protected UserDataBean userData;

    public override void Awake()
    {
        base.Awake();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_EnterGame)
        {
            OnClickForEnterGame();
        }
        else if (viewButton == ui_CreateGame)
        {
            OnClickForCreateGame();
        }
        else if (viewButton == ui_Delete)
        {
            OnClickForDelete();
        }
        else if (viewButton == ui_OpenSave)
        {
            OnClickForOpenSave();
        }
        else if (viewButton == ui_UseBackups)
        {
            OnClickForUseBackups();
        }
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    public void SetData(int userDataIndex, UserDataBean userData)
    {
        this.userData = userData;
        this.userDataIndex = userDataIndex;
        ui_Continue.ShowObj(false);
        ui_Create.ShowObj(false);
        ui_Error.ShowObj(false);
        if (userData == null || userData.id == 0)
        {
            ui_Create.ShowObj(true);
        }
        else
        {
            //如果数据损坏
            if (userData.isErrorData)
            {
                ui_Error.ShowObj(true);
            }
            else
            {
                ui_Continue.ShowObj(true);

                SetCreatureUI(userData.selfCreature);
                SetUserName(userData.userName);
                SetCrystal(userData.crystal);
                SetGameTime(userData.gameTime);
            }
        }
    }

    /// <summary>
    /// 设置生物UI(按大卡参数: 复用详情UI显示链, 含幻化ui_show_data尺寸/肖像覆盖/ui_data_b缩放位置/等比sizeDelta防裁切;
    /// 末按容器实际高度走 ApplyCardIconSizeFit 等比修正——本容器 340 高≠大卡标准 450,直接套大卡尺寸会偏大偏位)
    /// </summary>
    public void SetCreatureUI(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForDetails(ui_Icon, null, creatureData);
        ui_Icon.raycastTarget = false;
        //按容器实际高度等比修正尺寸(取值口径同 SetCreatureUIForDetails: 幻化 ui_show_data 优先, 回落原生物 ui_data_b)
        if (creatureData.GetTransformUIShowData(out float transformUIScale, out Vector2 transformUIPos))
        {
            GameUIUtil.ApplyCardIconSizeFit(ui_Icon.rectTransform, transformUIScale, transformUIPos, GameUIUtil.cardContentHeightForB);
        }
        else
        {
            GameUIUtil.ApplyCardIconSizeFit(ui_Icon.rectTransform, creatureData.creatureModel.ui_data_b, GameUIUtil.cardContentHeightForB);
        }
    }

    /// <summary>
    /// 设置用户名字
    /// </summary>
    public void SetUserName(string userName)
    {
        ui_Name.text = userName;
    }

    /// <summary>
    /// 设置金币
    /// </summary>
    public void SetCrystal(long crystal)
    {
        ui_CoinText.text = $"{crystal}";
    }

    /// <summary>
    /// 设置总游戏时间(格式 00:00 小时:分钟, gameTime单位为秒)
    /// </summary>
    public void SetGameTime(long gameTime)
    {
        long h = gameTime / 3600;
        long m = (gameTime % 3600) / 60;
        ui_GameTime.text = $"{h:D2}:{m:D2}";
    }

    /// <summary>
    /// 点击进入游戏
    /// </summary>
    public void OnClickForEnterGame()
    {
        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            GameDataHandler.Instance.manager.SetUserData(userData);
            WorldHandler.Instance.EnterGameForBaseScene(userData, isClearWorld : false, isAnimForBuildingShow : true);
        }, false);
    }

    /// <summary>
    /// 点击创建游戏
    /// </summary>
    public void OnClickForCreateGame()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIMainCreate>();
        targetUI.SetData(userDataIndex);
    }

    /// <summary>
    /// 点击删除存档
    /// </summary>
    public void OnClickForDelete()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(203), userData.userName);
        dialogData.actionSubmit = (dialogView, dialogData) =>
        {
            GameDataHandler.Instance.manager.DeleteUserData(userData);
            UIHandler.Instance.RefreshUI();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 点击打开存档目录
    /// </summary>
    public void OnClickForOpenSave()
    {
        try
        {
            string path = Application.persistentDataPath;
            Process.Start(path);
        }
        catch (Exception e)
        {
            LogUtil.LogError($"打开存档失败 {e.ToString()}");
        }
    }

    /// <summary>
    /// 点击使用备份数据
    /// </summary>
    public void OnClickForUseBackups()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(207);
        dialogData.actionSubmit = (dialogView, data) =>
        {
            // GameDataHandler.Instance.manager.SetUserData(userData);
            // WorldHandler.Instance.EnterGameForBaseScene(userData, false);
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);

    }
}
