using UnityEngine;
using UnityEngine.UI;

public partial class UIViewGameWorldMapPoint : BaseUIView
{

    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(GameWorldMapDetailsBean gameWorldMapDetails, Vector2 currentMapPosition, RectTransform mapContent)
    {
        //���õ�ͼλ��
        SetPosition(mapContent.rect.width, mapContent.rect.height, gameWorldMapDetails.mapLength, gameWorldMapDetails.mapIndexNum, gameWorldMapDetails.mapPosition);

        //����item״̬
        SetState(gameWorldMapDetails.mapPosition, currentMapPosition);
    }

    /// <summary>
    /// ����״̬
    /// </summary>
    public void SetState(Vector2 itemPosition, Vector2 currentMapPosition)
    {
        //ֻ����һ���ܵ��
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
    /// ���õ�ͼλ��
    /// </summary>
    public void SetPosition(float sizeMapW, float sizeMapH,int mapLength,int mapIndexNum,Vector2 mapPosition)
    {
        //��ĵ�ͼ��λ����
        int xPointNum = mapLength + 2;
        int yPointNum = mapIndexNum;
        //��λ��������ұ߽绹����һ�������
        float itemPointW = sizeMapW / (xPointNum - 1);
        float itemPointH = sizeMapH / (yPointNum + 1);

        //���õ�λ����
        float xPosition = itemPointW * mapPosition.x - sizeMapW / 2f;
        float yPosition = itemPointH * mapPosition.y - sizeMapH / 2f + itemPointH;
        rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
    }
}
