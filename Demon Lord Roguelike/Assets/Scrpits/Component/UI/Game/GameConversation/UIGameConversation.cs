

using System;
using UnityEngine.UI;

public partial class UIGameConversation : BaseUIComponent
{
    public CreatureBean creatureData;
    public Action acionForEnd;

    public override void OpenUI()
    {
        base.OpenUI();

    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData,string content, Action acionForEnd)
    {
        this.creatureData = creatureData;
        this.acionForEnd = acionForEnd;
        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetContent(content);
        ui_IconContent.SetData(creatureData, PopupEnum.CreatureCardDetails);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    /// <param name="name"></param>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    public void SetContent(string content)
    {
        ui_TalkText.text = $"{content}";
    }

    /// <summary>
    /// 设置卡片图像
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        //比原始大小放大2倍
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale: 2);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BG)
        {
            OnClickForEnd();
        }
        else if (viewButton == ui_Gift)
        {
            OnClickForGift();
        }
    }

    /// <summary>
    /// 点击结束
    /// </summary>
    public void OnClickForEnd()
    {
        acionForEnd?.Invoke();
    }
    
    /// <summary>
    /// 点击贿赂
    /// </summary>
    public void OnClickForGift()
    {
        DialogBean dialogData = new DialogBean();
        UIHandler.Instance.ShowDialogItemSelect(dialogData);
    }
}