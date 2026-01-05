public class DoomCouncilEntityRename : DoomCouncilBaseEntity
{

    public override bool TriggerFirst()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        UIHandler.Instance.CloseAllUI();
        //给魔物重命名
        if (doomCouncilInfo.class_entity_data.Equals("1"))
        {
            DialogSelectCreatureBean dialogSelectCreatureData = new DialogSelectCreatureBean();
            dialogSelectCreatureData.selectNumMax = 1;
            dialogSelectCreatureData.actionSubmit = (selectView, selectData) =>
            {
                var targetSelectView = selectView as UIDialogSelectCreature;
                if (!targetSelectView.listSelect.IsNull())
                {
                    var targetCreature = targetSelectView.listSelect[0];
                    ShowRenameDialog(targetCreature);
                }
                else
                {
                    BackDoomCouncilMain();
                }
            };
            dialogSelectCreatureData.actionCancel = (view, data) =>
            {
                BackDoomCouncilMain();
            };
            UIHandler.Instance.ShowDialogSelectCreature(dialogSelectCreatureData);
        }
        //给魔王重命名
        else if (doomCouncilInfo.class_entity_data.Equals("2"))
        {
            ShowRenameDialog(userData.selfCreature);
        }
        return true;
    }

    /// <summary>
    /// 展示重命名弹窗
    /// </summary>
    public void ShowRenameDialog(CreatureBean targetCreature)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        DialogRenameBean dialogRenameData = new DialogRenameBean();
        dialogRenameData.inputContent = targetCreature.creatureName;
        dialogRenameData.characterLimit = 10;
        dialogRenameData.actionSubmit = (view, data) =>
        {
            if (doomCouncilInfo.class_entity_data.Equals("1"))
            {

            }
            else if (doomCouncilInfo.class_entity_data.Equals("2"))
            {
                userData.userName = dialogRenameData.inputContent;
            }
            targetCreature.creatureName = dialogRenameData.inputContent;
            //保存数据
            GameDataHandler.Instance.manager.SaveUserData();
            //弹出提示
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000003), 1);
            BackDoomCouncilMain();
        };
        dialogRenameData.actionCancel = (view, data) =>
        {
            BackDoomCouncilMain();
        };
        UIHandler.Instance.ShowDialogRename(dialogRenameData);
    }

    /// <summary>
    /// 返回议会主界面
    /// </summary>
    public void BackDoomCouncilMain()
    {
        //弹出议会UI
        UIDoomCouncilMain doomCouncilMain = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilMain>();
    }

}