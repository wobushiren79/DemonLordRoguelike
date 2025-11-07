

using UnityEngine.UI;

public partial class UIViewDoomCouncilMainItem : BaseUIView
{
    protected DoomCouncilInfoBean doomCouncilInfo;
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(DoomCouncilInfoBean doomCouncilInfo)
    {
        this.doomCouncilInfo = doomCouncilInfo;

        SetContent(doomCouncilInfo.name_language);
        SetCost(doomCouncilInfo.cost_crystal, doomCouncilInfo.cost_reputation);

        ui_UIViewDoomCouncilMainItem_PopupButtonCommonView.SetData(doomCouncilInfo, PopupEnum.DoomCouncilMainDetails);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetContent(string content)
    {
        ui_Content.text = $"{content}";
    }

    /// <summary>
    /// 设置花费
    /// </summary>
    /// <param name="costCrystal">花费的魔晶</param>
    /// <param name="costReputation">花费的声望</param>
    public void SetCost(long costCrystal, long costReputation)
    {
        ui_ReputationContent.text = $"{costReputation}";
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewDoomCouncilMainItem_Button)
        {
            OnClickForSubmit();
        }
    }

    /// <summary>
    /// 点击确定
    /// </summary>
    public void OnClickForSubmit()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //检测是否有足够的魔晶
        if (!userData.CheckHasCrystal(doomCouncilInfo.cost_crystal, true, false))
        {
            return;
        }
        //检测是否有足够的声望
        if (!userData.CheckHasReputation(doomCouncilInfo.cost_reputation, true, false))
        {
            return;
        }
        //弹出确认框
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(53002), doomCouncilInfo.name_language);
        dialogData.actionSubmit = (view, data) =>
        {
            //检测是否有足够的魔晶
            if (!userData.CheckHasCrystal(doomCouncilInfo.cost_crystal, true, true))
            {
                return;
            }
            //检测是否有足够的声望
            if (!userData.CheckHasReputation(doomCouncilInfo.cost_reputation, true, true))
            {
                return;
            }
            UIHandler.Instance.ShowMask(0.2f, null, async () =>
            {
                DoomCouncilBean doomCouncilData = new DoomCouncilBean();
                doomCouncilData.doomCouncilInfo = doomCouncilInfo;
                GameHandler.Instance.StartDoomCouncil(doomCouncilData);
            }, false);
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
}