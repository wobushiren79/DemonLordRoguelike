
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
        }
    }

    public void SetCreatureUI(CreatureBean creatureData)
    {
        creatureData.creatureModel.GetShowRes(out string resName, out int skinType);

        SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, resName);
        string[] skinArray =  creatureData.GetSkinArray(skinType);
        SpineHandler.Instance.ChangeSkeletonSkin(ui_Icon.Skeleton, skinArray);
        SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, true);
        //设置UI大小和坐标
        creatureData.creatureModel.ChangeUISizeForB(ui_Icon.rectTransform);
    }

    /// <summary>
    /// 点击进入游戏
    /// </summary>
    public void OnClickForEnterGame()
    {

    }

    /// <summary>
    /// 点击创建游戏
    /// </summary>
    public void OnClickForCreateGame()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIMainCreate>();
        targetUI.SetData(userDataIndex);
    }

}
