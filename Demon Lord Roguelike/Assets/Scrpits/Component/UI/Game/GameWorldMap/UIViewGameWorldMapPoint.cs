using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewGameWorldMapPoint : BaseUIView
{
    protected GameWorldMapDetailsBean gameWorldMapDetails;
    protected Vector2 currentMapPosition;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewGameWorldMapPoint)
        {
            OnClickForSelect();
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(GameWorldMapDetailsBean gameWorldMapDetails, Vector2 currentMapPosition, RectTransform mapContent)
    {
        this.gameWorldMapDetails = gameWorldMapDetails;
        this.currentMapPosition = currentMapPosition;

        SetPosition(mapContent.rect.width, mapContent.rect.height, gameWorldMapDetails.mapLength, gameWorldMapDetails.mapIndexNum, gameWorldMapDetails.mapPosition);
        //����item״̬
        SetState(gameWorldMapDetails.mapPosition, currentMapPosition);
        //���õ�ͼ����
        SetMapType(gameWorldMapDetails.mapType, gameWorldMapDetails.mapPosition, currentMapPosition);
    }

    /// <summary>
    /// ���õ�ͼ����
    /// </summary>
    public void SetMapType(int mapType, Vector2 itemPosition, Vector2 currentMapPosition)
    {
        ui_Icon.color = Color.white;
        if (itemPosition.x == currentMapPosition.x + 1)
        {
            var mapTypeInfo = GameWorldMapTypeInfoCfg.GetItemData(mapType);
            IconHandler.Instance.manager.GetUISpriteByName(mapTypeInfo.icon_res, (sprite) =>
            {
                ui_Icon.sprite = sprite;
            });
        }
        else if (itemPosition.x < currentMapPosition.x + 1)
        {
            var mapTypeInfo = GameWorldMapTypeInfoCfg.GetItemData(mapType);
            IconHandler.Instance.manager.GetUISpriteByName(mapTypeInfo.icon_res, (sprite) =>
            {
                ui_Icon.sprite = sprite;
                if (itemPosition.x != 0)
                {
                    ui_Icon.color = Color.gray;
                }
            });
        }
        else
        {
            IconHandler.Instance.GetUnKnowSprite((sprite) =>
            {
                ui_Icon.sprite = sprite;
            });
        }
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
    public void SetPosition(float sizeMapW, float sizeMapH, int mapLength, int mapIndexNum, Vector2 mapPosition)
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

    /// <summary>
    /// ���ѡ��
    /// </summary>
    public void OnClickForSelect()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(502);
        dialogData.actionSubmit = (view, data) =>
        {
            FightBean fightData = new FightBean();
            WorldHandler.Instance.EnterGameForFightScene(fightData);
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
}
