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
    /// 设置数据
    /// </summary>
    public void SetData(GameWorldMapDetailsBean gameWorldMapDetails, Vector2 currentMapPosition, RectTransform mapContent)
    {
        this.gameWorldMapDetails = gameWorldMapDetails;
        this.currentMapPosition = currentMapPosition;

        SetPosition(mapContent.rect.width, mapContent.rect.height, gameWorldMapDetails.mapLength, gameWorldMapDetails.mapIndexNum, gameWorldMapDetails.mapPosition);

        SetState(gameWorldMapDetails.mapPosition, currentMapPosition);

        SetMapType(gameWorldMapDetails.mapType, gameWorldMapDetails.mapPosition, currentMapPosition);
    }

    /// <summary>
    /// 设置地图类型
    /// </summary>
    public void SetMapType(int mapType, Vector2 itemPosition, Vector2 currentMapPosition)
    {
        ui_Icon.color = Color.white;
        if (itemPosition.x == currentMapPosition.x + 1)
        {
            IconHandler.Instance.GetIconSprite(SpriteAtlasType.UI, "", (sprite) =>
            {
                ui_Icon.sprite = sprite;
            });
        }
        else if (itemPosition.x < currentMapPosition.x + 1)
        {
            IconHandler.Instance.GetIconSprite(SpriteAtlasType.UI, "", (sprite) =>
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

    public void SetState(Vector2 itemPosition, Vector2 currentMapPosition)
    {
        if (itemPosition.x == currentMapPosition.x + 1)
        {
            ui_UIViewGameWorldMapPoint.interactable = true;
        }
        else
        {
            ui_UIViewGameWorldMapPoint.interactable = false;
        }
    }

    public void SetPosition(float sizeMapW, float sizeMapH, int mapLength, int mapIndexNum, Vector2 mapPosition)
    {
        int xPointNum = mapLength + 2;
        int yPointNum = mapIndexNum;

        float itemPointW = sizeMapW / (xPointNum - 1);
        float itemPointH = sizeMapH / (yPointNum + 1);


        float xPosition = itemPointW * mapPosition.x - sizeMapW / 2f;
        float yPosition = itemPointH * mapPosition.y - sizeMapH / 2f + itemPointH;
        rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
    }


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
