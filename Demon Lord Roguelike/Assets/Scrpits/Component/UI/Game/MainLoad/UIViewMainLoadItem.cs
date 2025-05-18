
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
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    public void SetData(int userDataIndex, UserDataBean userData)
    {
        this.userData = userData;
        this.userDataIndex = userDataIndex;
        if (userData == null)
        {
            ui_Continue.ShowObj(false);
            ui_Create.ShowObj(true);
        }
        else
        {
            ui_Continue.ShowObj(true);
            ui_Create.ShowObj(false);

            SetCreatureUI(userData.selfCreature);
            SetUserName(userData.userName);
            SetCrystal(userData.crystal);
        }
    }

    /// <summary>
    /// 设置生物UI
    /// </summary>
    public void SetCreatureUI(CreatureBean creatureData)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_Icon, creatureData,isUIShow : true);
        //播放动画
        SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, true);
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
        GameDataHandler.Instance.manager.SetUserData(userData);
        WorldHandler.Instance.EnterGameForBaseScene(userData,false);
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
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(203),userData.userName);
        dialogData.actionSubmit = (dialogView, dialogData) =>
        {
            GameDataHandler.Instance.manager.DeleteUserData(userData);
            UIHandler.Instance.RefreshUI();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
}
