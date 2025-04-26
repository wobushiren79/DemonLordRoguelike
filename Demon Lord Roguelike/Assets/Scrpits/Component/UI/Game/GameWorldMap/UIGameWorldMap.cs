using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameWorldMap : BaseUIComponent
{
    //点位列表
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
    /// 清除地图点位数据
    /// </summary>
    public void ClearMapData()
    {
        dicMapPoint.Clear();
        ui_Map.DestroyAllChild(1);
    }

    /// <summary>
    /// 初始化地图数据
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
        //生成点位
        foreach (var item in mapDetailsData)
        {
            GameWorldMapDetailsBean gameWorldMapDetails = item.Value;
            CreateMapPoint(gameWorldMapData, gameWorldMapDetails);
        }
        //生成连线
        foreach (var item in mapDetailsData)
        {
            GameWorldMapDetailsBean gameWorldMapDetails = item.Value;
            CreateMapPointLine(gameWorldMapData, gameWorldMapDetails);
        }
    }

    /// <summary>
    /// 创建地图点位
    /// </summary>
    public void CreateMapPoint(GameWorldMapBean gameWorldMapData, GameWorldMapDetailsBean gameWorldMapDetails)
    {
        //创建地图点位
        GameObject objItemPoint = Instantiate(ui_Map.gameObject, ui_UIViewGameWorldMapPoint.gameObject);
        objItemPoint.gameObject.SetActive(true);

        UIViewGameWorldMapPoint itemView = objItemPoint.GetComponent<UIViewGameWorldMapPoint>();
        itemView.SetData(gameWorldMapDetails, gameWorldMapData.currentMapPosition, ui_Map);
        //记录所有点位obj
        dicMapPoint.Add(gameWorldMapDetails.id, objItemPoint);
    }

    /// <summary>
    /// 创建地图连线
    /// </summary>
    /// <param name="gameWorldMapDetails"></param>
    public void CreateMapPointLine(GameWorldMapBean gameWorldMapData, GameWorldMapDetailsBean gameWorldMapDetails)
    {
        //只显示当前地图位置的下一步和之前的连线
        if (gameWorldMapDetails.mapPosition.x > gameWorldMapData.currentMapPosition.x)
        {
            return;
        }
        dicMapPoint.TryGetValue(gameWorldMapDetails.id, out GameObject objPointStart);
        Vector2 startPosition = ((RectTransform)objPointStart.transform).anchoredPosition;
        for (int i = 0; i < gameWorldMapDetails.nextIds.Count; i++)
        {
            var itemNextId = gameWorldMapDetails.nextIds[i];

            dicMapPoint.TryGetValue(itemNextId, out GameObject objPointEnd);
            gameWorldMapData.GetDetailsData().TryGetValue(itemNextId, out GameWorldMapDetailsBean nextGameWorldMapDetails);

            GameObject objItemPointLine = Instantiate(ui_Map.gameObject, ui_UIViewGameWorldMapPointLine.gameObject);
            objItemPointLine.gameObject.SetActive(true);
            objItemPointLine.transform.SetAsFirstSibling();

            UIViewGameWorldMapPointLine itemView = objItemPointLine.GetComponent<UIViewGameWorldMapPointLine>();
            itemView.SetData(startPosition, ((RectTransform)objPointEnd.transform).anchoredPosition);

            //设置连线颜色
            if (gameWorldMapDetails.mapPosition.x == gameWorldMapData.currentMapPosition.x)
            {
                itemView.SetState(0);
            }
            else
            {
                if (nextGameWorldMapDetails != null 
                    && gameWorldMapData.recordMapPosition.Contains(gameWorldMapDetails.mapPosition) 
                    && gameWorldMapData.recordMapPosition.Contains(nextGameWorldMapDetails.mapPosition))
                {
                    itemView.SetState(2);
                }
                else
                {
                    itemView.SetState(1);
                }
            }
        }
    }

    /// <summary>
    /// 按钮点击
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
    /// 点击-退出
    /// </summary>
    public void OnClickForExit()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(501);
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
        var targetDialog = UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 动画展开动画
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
    /// 动画关闭
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
