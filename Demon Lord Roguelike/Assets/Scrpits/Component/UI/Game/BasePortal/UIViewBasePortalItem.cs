using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewBasePortalItem : BaseUIView
{
    protected GameWorldInfoBean gameWorldInfo;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BG)
        {
            OnClickForEnterWorld();
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="gameWorldInfo"></param>
    public void SetData(GameWorldInfoBean gameWorldInfo)
    {
        this.gameWorldInfo = gameWorldInfo;

        Vector2 targetMapPos = gameWorldInfo.GetMapPosition();
        SetMapPosition(targetMapPos);

        string targetName = gameWorldInfo.GetName();
        SetName(targetName);
    }

    /// <summary>
    /// 设置地图位置
    /// </summary>
    /// <param name="targetPos"></param>
    public void SetMapPosition(Vector2 targetPos)
    {
        rectTransform.anchoredPosition = targetPos;
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    public void SetIcon()
    {

    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = name;
    }

    /// <summary>
    /// 点击-进入世界
    /// </summary>
    public void OnClickForEnterWorld()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(401), gameWorldInfo.GetName());
        dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        dialogData.actionSubmit = ((view, data) =>
        {
            UIHandler.Instance.ShowMask(2f, null, () =>
            {
                WorldHandler.Instance.ClearWorldData(() =>
                {
                    UIHandler.Instance.HideMask(2f,
                        () =>
                        {
                            var mapUI = UIHandler.Instance.OpenUI<UIGameWorldMap>(layer: 0);
                        }, 
                        null);
                },false);
            });
        });
        UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);
    }
}
