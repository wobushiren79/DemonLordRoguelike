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
    /// ��������
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
    /// ���õ�ͼλ��
    /// </summary>
    /// <param name="targetPos"></param>
    public void SetMapPosition(Vector2 targetPos)
    {
        rectTransform.anchoredPosition = targetPos;
    }

    /// <summary>
    /// ����ͼ��
    /// </summary>
    public void SetIcon()
    {

    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = name;
    }

    /// <summary>
    /// ���-��������
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
