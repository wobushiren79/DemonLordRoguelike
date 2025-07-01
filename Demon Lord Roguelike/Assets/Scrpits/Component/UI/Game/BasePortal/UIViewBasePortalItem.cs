using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewBasePortalItem : BaseUIView
{
    protected GameWorldInfoBean gameWorldInfo;
    protected PopupButtonCommonView popupForPortalDetails;
    protected bool isRotate = false;

    protected Vector2 rotateCenter = new Vector2(0, 0); // 指定旋转中心点
    protected float rotateSpeed = 50f;
    protected float rotateRadius = 0; // 旋转半径
    protected float currentRotateAngle = 0f;
    public override void Awake()
    {
        base.Awake();
        popupForPortalDetails = ui_BG.GetComponent<PopupButtonCommonView>();
        popupForPortalDetails.AddListenerForEnter(ActionForClickShowStart);
        popupForPortalDetails.AddListenerForExit(ActionForClickShowEnd);
    }

    public void Update()
    {
        if (isRotate)
        {
            currentRotateAngle += rotateSpeed * Time.deltaTime;
            // 计算新位置
            float x = rotateCenter.x + rotateRadius * Mathf.Cos(currentRotateAngle * Mathf.Deg2Rad);
            float y = rotateCenter.y + rotateRadius * Mathf.Sin(currentRotateAngle * Mathf.Deg2Rad);
            //更新地图位置
            SetMapPosition(new Vector2(x, y));
        }
    }

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
    public void SetData(GameWorldInfoBean gameWorldInfo, GameWorldInfoRandomBean gameWorldInfoRandom)
    {
        this.gameWorldInfo = gameWorldInfo;
        //设置地图位置
        SetMapPosition(gameWorldInfoRandom.uiPosition);
        //设置名字
        string targetName = gameWorldInfo.GetName();
        SetName(targetName);
        //设置图标
        SetIcon(gameWorldInfo.icon_res);
        //初始化弹窗
        popupForPortalDetails.SetData((gameWorldInfo, gameWorldInfoRandom), PopupEnum.PortalDetails);

        // 计算初始角度和半径
        Vector2 offset = rectTransform.anchoredPosition - rotateCenter;
        rotateRadius = offset.magnitude;
        currentRotateAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        //随机旋转速度
        rotateSpeed = Random.Range(10f, 50f);
        isRotate = true;
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
    public void SetIcon(string iconRes)
    {
        if (iconRes.IsNull())
        {
            int seed = Random.Range(0, int.MaxValue);
            CreateToolsForPlanetTextureBean createData = new CreateToolsForPlanetTextureBean(seed);
            var planetTex = CreateTools.CreatePlanetTexture(createData);
            ui_Icon.texture = planetTex;
            ui_Icon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = name;
    }
    #region 按钮点击
    /// <summary>
    /// 点击-进入世界
    /// </summary>
    public void OnClickForEnterWorld()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(401), gameWorldInfo.GetName());
        float animTimeForShowMask = 2f;
        float animTimeForHideMask = 1f;
        dialogData.actionSubmit = ((view, data) =>
        {
            UIHandler.Instance.ShowMask(animTimeForShowMask, null, () =>
            {
                WorldHandler.Instance.ClearWorldData(() =>
                {
                    UIHandler.Instance.HideMask(animTimeForHideMask,
                        () =>
                        {
                            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                            GameWorldMapBean gameWorldMapData = GameHandler.Instance.CreateGameWorldMapData(gameWorldInfo.id);

                            userData.gameWorldMapData = gameWorldMapData;
                            var mapUI = UIHandler.Instance.OpenUI<UIGameWorldMap>(layer: 0);
                            mapUI.AnimForShowUI(animTimeForHideMask);
                        },
                        null);
                }, false);
            }, true);
        });
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }
    #endregion

    #region  回调
    public void ActionForClickShowStart(PopupButtonCommonView popupButtonCommonView)
    {
        isRotate = false;
    }
    public void ActionForClickShowEnd(PopupButtonCommonView popupButtonCommonView)
    {
        isRotate = true;
    }
    #endregion
}
