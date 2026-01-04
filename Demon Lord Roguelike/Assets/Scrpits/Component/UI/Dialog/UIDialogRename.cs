

public partial class UIDialogRename : DialogView
{
    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        DialogRenameBean dialogRenameData = dialogData as DialogRenameBean;
        SetInputHint(dialogRenameData.inputHint);
        SetInputContent(dialogRenameData.inputContent, dialogRenameData.characterLimit);
        UGUIUtil.RefreshUISize(ui_ContentPro.transform);
        UGUIUtil.RefreshUISize(ui_ContentShow.transform);
        ui_Input.onValueChanged.RemoveAllListeners();
        ui_Input.onValueChanged.AddListener(OnValueChanged);
    }

    public void SetInputHint(string hint)
    {
        ui_Placeholder.text = hint;
    }

    public void SetInputContent(string content, int textLimit)
    {

        ui_Input.text = content;
        ui_Input.characterLimit = textLimit;
    }

    public void OnValueChanged(string str)
    {
        DialogRenameBean dialogRenameData = dialogData as DialogRenameBean;
        dialogRenameData.inputContent = str;
    }
}