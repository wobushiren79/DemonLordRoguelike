

public partial class UIPopupText : PopupShowCommonView
{
    public override void SetData(object data)
    {
        string contentText = (string)data;
        SetContentText(contentText);
    }

    /// <summary>
    /// 设置文本内容
    /// </summary>
    public void SetContentText(string contentText)
    {
        ui_ContentText.text = $"{contentText}";
    }
}