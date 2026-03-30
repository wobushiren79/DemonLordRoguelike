

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
        SetIcon(doomCouncilInfo.icon_res);
        ui_UIViewDoomCouncilMainItem_PopupButtonCommonView.SetData(doomCouncilInfo, PopupEnum.DoomCouncilMainDetails);
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    public void SetIcon(string iconRes)
    {
        IconHandler.Instance.SetUIIcon(iconRes, ui_Icon);
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
        string dialogContent;
        //如果是100%通过
        if (doomCouncilInfo.success_rate >= 1)
        {
            dialogContent = string.Format(TextHandler.Instance.GetTextById(53012), doomCouncilInfo.name_language); 
        }
        else
        {
            dialogContent = string.Format(TextHandler.Instance.GetTextById(53002), doomCouncilInfo.name_language); 
        }
        //弹出确认框
        DialogBean dialogData = new DialogBean();
        dialogData.content = dialogContent;
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
            DoomCouncilBean doomCouncilData = new DoomCouncilBean(doomCouncilInfo.id);
            var userTempData = userData.GetUserTempData();
            //如果是100%通过 则直接添加
            if (doomCouncilInfo.success_rate >= 1)
            {
                userTempData.AddDoomCouncil(doomCouncilData);
                return;
            }
            //小于100%则进入议会
            else
            {
                UIHandler.Instance.ShowMask(0.2f, null, async () =>
                {
                    GameHandler.Instance.StartDoomCouncil(doomCouncilData);
                }, false);
            }
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
}