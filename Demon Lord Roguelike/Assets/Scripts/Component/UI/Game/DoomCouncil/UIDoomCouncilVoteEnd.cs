

using UnityEngine;

public partial class UIDoomCouncilVoteEnd : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        ui_VoteEnd_Image.gameObject.SetActive(false);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_VoteEnd_Image.gameObject.SetActive(false);
    }

    /// <summary>
    /// 投票结果展示
    /// </summary>
    public void VoteEndShow(bool isPass)
    {
        ui_VoteEnd_Image.gameObject.SetActive(true);
        if (isPass)
        {
            ColorUtility.TryParseHtmlString($"#1DA9D6", out Color targetColorAye);
            ui_VoteEndText.text = TextHandler.Instance.GetTextById(53008);
            ui_VoteEndText.color = targetColorAye;
            ui_VoteEnd_Image.color = targetColorAye;
            ui_VoteEnd_Image.color = new Color(targetColorAye.r, targetColorAye.g, targetColorAye.b, 0.5f);
        }
        else
        {
            ColorUtility.TryParseHtmlString($"#D61515", out Color targetColorNay);
            ui_VoteEndText.text = TextHandler.Instance.GetTextById(53009);
            ui_VoteEndText.color = targetColorNay;
            ui_VoteEnd_Image.color = new Color(targetColorNay.r, targetColorNay.g, targetColorNay.b, 0.5f);
        }
    }
}