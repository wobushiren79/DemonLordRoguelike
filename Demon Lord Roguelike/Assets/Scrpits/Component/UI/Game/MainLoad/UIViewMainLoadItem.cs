
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
        if (userData == null)
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
            }
        }
    }

    /// <summary>
    /// 设置生物UI
    /// </summary>
    public void SetCreatureUI(CreatureBean creatureData)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_Icon, creatureData, isUIShow: true);
        //播放动画
        SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, creatureData, true);
        //设置UI大小和坐标
        creatureData.creatureModel.ChangeUISizeForB(ui_Icon.rectTransform);
        ui_Icon.raycastTarget = false;
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
