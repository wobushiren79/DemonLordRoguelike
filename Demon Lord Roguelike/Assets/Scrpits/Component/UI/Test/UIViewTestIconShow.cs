

using System;
using UnityEngine.UI;

public partial class UIViewTestIconShow : BaseUIView
{
    public Action<int, long> actionForOnClick = null;
    public int showType;
    public long showId;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (ui_UIViewTestIconShow == viewButton)
        {
            OnClickForHandler();
        }
    }

    /// <summary>
    /// 点击处理
    /// </summary>
    public void OnClickForHandler()
    {
        if (actionForOnClick != null)
        {
            actionForOnClick(showType, showId);
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetDataForItem(int showType, long showId, string iconRes, string showText)
    {
        this.showId = showId;
        this.showType = showType;
        SetShowText(showText);
        if (iconRes == null)
        {
            ui_ShowIcon.gameObject.SetActive(false);
        }
        else
        {
            ui_ShowIcon.gameObject.SetActive(true);
            IconHandler.Instance.SetItemIcon(iconRes, 0, ui_ShowIcon);
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetDataForSkin(int showType, long showId, string iconRes, string showText)
    {
        this.showId = showId;
        this.showType = showType;
        SetShowText(showText);
        if (iconRes == null)
        {
            ui_ShowIcon.gameObject.SetActive(false);
        }
        else
        {
            ui_ShowIcon.gameObject.SetActive(true);
            IconHandler.Instance.SetSkinIcon(iconRes, ui_ShowIcon);
        }
    }

    public void SetShowText(string showText)
    {
        ui_ShowText.text = showText;
    }
}