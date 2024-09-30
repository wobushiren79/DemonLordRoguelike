using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameWorldMap : BaseUIComponent
{
    //��λ�б�
    public Dictionary<string, GameObject> dicMapPoint = new Dictionary<string, GameObject>();



    public override void OpenUI()
    {
        base.OpenUI();
        InitMapData();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ClearMapData();
    }

    /// <summary>
    /// �����ͼ��λ����
    /// </summary>
    public void ClearMapData()
    {
        dicMapPoint.Clear();
        ui_Map.DestroyAllChild(1);
    }

    /// <summary>
    /// ��ʼ����ͼ����
    /// </summary>
    public void InitMapData()
    {
        ClearMapData();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var gameWorldMapData = userData.gameWorldMapData;
        if (gameWorldMapData == null)
        {
            WorldHandler.Instance.EnterGameForBaseScene(userData, true);
            return;
        }
        var mapDetailsData = gameWorldMapData.GetDetailsData();
        //���ɵ�λ
        foreach (var item in mapDetailsData)
        {
            GameWorldMapDetailsBean gameWorldMapDetails = item.Value;
            CreateMapPoint(gameWorldMapDetails, gameWorldMapData.currentMapPosition);
        }
        //��������
        foreach (var item in mapDetailsData)
        {
            GameWorldMapDetailsBean gameWorldMapDetails = item.Value;
            CreateMapPointLine(gameWorldMapDetails, gameWorldMapData.currentMapPosition);
        }
    }

    /// <summary>
    /// ������ͼ��λ
    /// </summary>
    public void CreateMapPoint(GameWorldMapDetailsBean gameWorldMapDetails, Vector2 currentMapPosition)
    {
        //������ͼ��λ
        GameObject objItemPoint = Instantiate(ui_Map.gameObject, ui_UIViewGameWorldMapPoint.gameObject);
        objItemPoint.gameObject.SetActive(true);

        UIViewGameWorldMapPoint itemView = objItemPoint.GetComponent<UIViewGameWorldMapPoint>();
        itemView.SetData(gameWorldMapDetails, currentMapPosition, ui_Map);
        //��¼���е�λobj
        dicMapPoint.Add(gameWorldMapDetails.id, objItemPoint);
    }

    /// <summary>
    /// ������ͼ����
    /// </summary>
    /// <param name="gameWorldMapDetails"></param>
    public void CreateMapPointLine(GameWorldMapDetailsBean gameWorldMapDetails, Vector2 currentMapPosition)
    {
        //ֻ��ʾ��ǰ��ͼλ�õ���һ����֮ǰ������
        if (gameWorldMapDetails.mapPosition.x > currentMapPosition.x)
        {
            return;
        }
        dicMapPoint.TryGetValue(gameWorldMapDetails.id, out GameObject objPointStart);
        Vector2 startPosition = ((RectTransform)objPointStart.transform).anchoredPosition;
        for (int i = 0; i < gameWorldMapDetails.nextIds.Count; i++)
        {
            var itemNextId = gameWorldMapDetails.nextIds[i];
            dicMapPoint.TryGetValue(itemNextId, out GameObject objPointEnd);

            GameObject objItemPointLine = Instantiate(ui_Map.gameObject, ui_UIViewGameWorldMapPointLine.gameObject);
            objItemPointLine.gameObject.SetActive(true);
            objItemPointLine.transform.SetAsFirstSibling();

            UIViewGameWorldMapPointLine itemView = objItemPointLine.GetComponent<UIViewGameWorldMapPointLine>();
            itemView.SetData(startPosition, ((RectTransform)objPointEnd.transform).anchoredPosition);
        }
    }

    /// <summary>
    /// ��ť���
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// ���-�˳�
    /// </summary>
    public void OnClickForExit()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(501);
        dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        dialogData.actionSubmit = (view, data) =>
        {
            UIHandler.Instance.ShowMask(2f,
                () =>
                {
                    AnimForHideUI(2);
                },
                () =>
                {
                    UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                    userData.ClearGameWorldMapData();
                    WorldHandler.Instance.EnterGameForBaseScene(userData, true);
                }, false);
        };
        var targetDialog = UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);
    }

    /// <summary>
    /// ����չ������
    /// </summary>
    public void AnimForShowUI(float animTime)
    {
        ui_MaskClick.ShowObj(true);
        Vector2 targetSizeDelta = new Vector2(1920, 1080);
        ui_Content.sizeDelta = new Vector2(200, 1080);
        ui_Content
            .DOSizeDelta(targetSizeDelta, animTime)
            .OnComplete(() =>
            {
                ui_MaskClick.ShowObj(false);
                ui_Content.sizeDelta = targetSizeDelta;
            });
    }

    /// <summary>
    /// �����ر�
    /// </summary>
    public void AnimForHideUI(float animTime)
    {
        ui_MaskClick.ShowObj(true);
        Vector2 targetSizeDelta = new Vector2(200, 1080);
        ui_Content.sizeDelta = new Vector2(1920, 1080);
        ui_Content
            .DOSizeDelta(targetSizeDelta, animTime)
            .OnComplete(() =>
            {
                ui_MaskClick.ShowObj(false);
                ui_Content.sizeDelta = targetSizeDelta;
            });
    }

}
