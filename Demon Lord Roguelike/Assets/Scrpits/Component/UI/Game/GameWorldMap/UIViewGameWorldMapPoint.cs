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
        //设置item状态
        SetState(gameWorldMapDetails.mapPosition, currentMapPosition);
        //设置地图类型
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
    public void SetPosition(float sizeMapW, float sizeMapH, int mapLength, int mapIndexNum, Vector2 mapPosition)
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

    /// <summary>
    /// 点击选中
    /// </summary>
    public void OnClickForSelect()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(502);
        dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        dialogData.actionSubmit = (view, data) =>
        {
            WorldHandler.Instance.ClearWorldData(() =>
            {
                FightBean fightData = new FightBean();
                //打开加载UI
                UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
                //镜头初始化
                CameraHandler.Instance.InitData();
                //环境参数初始化
                VolumeHandler.Instance.InitData(GameSceneTypeEnum.Fight);
                //测试数据
                GameHandler.Instance.StartGameFight(fightData);
            });
        };
        UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);
    }
}
