using UnityEngine;
using UnityEngine.UI;

public partial class UIViewGameWorldMapPoint : BaseUIView
{

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(GameWorldMapDetailsBean gameWorldMapDetails, Vector2 currentMapPosition, RectTransform mapContent)
    {
        //设置地图位置
        SetPosition(mapContent.rect.width, mapContent.rect.height, gameWorldMapDetails.mapLength, gameWorldMapDetails.mapIndexNum, gameWorldMapDetails.mapPosition);

        //设置item状态
        SetState(gameWorldMapDetails.mapPosition, currentMapPosition);
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public void SetState(Vector2 itemPosition, Vector2 currentMapPosition)
    {
        //只有下一步能点击
        if (itemPosition.x == currentMapPosition.x + 1)
        {
            ui_UIViewGameWorldMapPoint.interactable = true;
        }
        else
        {
            ui_UIViewGameWorldMapPoint.interactable = false;
        }
    }

    /// <summary>
    /// 设置地图位置
    /// </summary>
    public void SetPosition(float sizeMapW, float sizeMapH,int mapLength,int mapIndexNum,Vector2 mapPosition)
    {
        //横的地图点位总数
        int xPointNum = mapLength + 2;
        int yPointNum = mapIndexNum;
        //点位间隔（左右边界还会多出一个间隔）
        float itemPointW = sizeMapW / (xPointNum - 1);
        float itemPointH = sizeMapH / (yPointNum + 1);

        //设置点位坐标
        float xPosition = itemPointW * mapPosition.x - sizeMapW / 2f;
        float yPosition = itemPointH * mapPosition.y - sizeMapH / 2f + itemPointH;
        rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
    }
}
