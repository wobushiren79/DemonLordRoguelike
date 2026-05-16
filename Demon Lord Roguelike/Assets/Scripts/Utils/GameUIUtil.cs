using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameUIUtil
{
    public static string pathCardScene = "Assets/LoadResources/Textures/CardScene";//卡片场景路径

    #region 颜色工具
    /// <summary>
    /// 设置渐变颜色，支持单色和双色渐变
    /// 单色格式: "#B9B9B9"，直接设置 Graphic.color
    /// 双色格式: "#B9B9B9,#B9B9B1"，通过材质的 _StartColor / _EndColor 属性设置渐变
    /// </summary>
    public static void SetGradientColor(Graphic graphic, string colorStr)
    {
        if (graphic == null || colorStr.IsNull())
            return;

        string[] colors = colorStr.Split(',');
        if (colors.Length >= 2)
        {
            ColorUtility.TryParseHtmlString(colors[0].Trim(), out Color startColor);
            ColorUtility.TryParseHtmlString(colors[1].Trim(), out Color endColor);
            var mat = new Material(graphic.material);
            mat.SetColor("_StartColor", startColor);
            mat.SetColor("_EndColor", endColor);
            graphic.material = mat;
        }
        else
        {
            ColorUtility.TryParseHtmlString(colorStr.Trim(), out Color color);
            var mat = new Material(graphic.material);
            mat.SetColor("_StartColor", color);
            mat.SetColor("_EndColor", color);
            graphic.material = mat;
        }
    }
    #endregion

    /// <summary>
    /// 设置生物简易UI
    /// </summary>
    public static void SetCreatureUIForSimple(SkeletonGraphic ui_Icon, CreatureBean creatureData, float scale = 1)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_Icon, creatureData, isNeedWeapon: false);
        ui_Icon.ShowObj(true);
        //设置UI大小和坐标
        creatureData.creatureModel.ChangeUISizeForS(ui_Icon.rectTransform, scale);
    }

    /// <summary>
    /// 设置生物详细UI
    /// </summary>
    public static void SetCreatureUIForDetails(SkeletonGraphic ui_Icon, RawImage ui_Scene, CreatureBean creatureData,
        float customUISize = 0, float customUIPosOffsetX = 0, float customUIPosOffsetY = 0)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_Icon, creatureData, isUIShow: true);
        //如果装备了肖像道具 使用肖像资源替换spine
        ItemBean portraitItem = creatureData.GetEquip(ItemTypeEnum.Portrait);
        if (portraitItem != null)
        {
            ItemsInfoBean portraitItemInfo = ItemsInfoCfg.GetItemData(portraitItem.itemId);
            if (portraitItemInfo != null && !portraitItemInfo.other_data.IsNull())
            {
                SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, portraitItemInfo.other_data);
            }
        }
        //播放动画
        SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, creatureData, true);
        ui_Icon.ShowObj(true);
        //设置UI大小和坐标
        creatureData.creatureModel.ChangeUISizeForB(ui_Icon.rectTransform);
        //自定义UI大小
        if (customUISize > 0)
        {
            ui_Icon.transform.localScale *= customUISize;    
            ui_Icon.rectTransform.anchoredPosition *= customUISize;
        }
        ui_Icon.rectTransform.anchoredPosition += new Vector2(customUIPosOffsetX, customUIPosOffsetY);
        //设置背景图片
        if (ui_Scene != null)
        {
            Texture2D targetSceneText = null;
            if (creatureData.creatureInfo.card_scene.IsNull())
            {
                //如果没有背景图片 使用通用
                targetSceneText = IconHandler.Instance.manager.GetTextureSync($"{pathCardScene}/Card_Scene_4.png");
            }
            else
            {
                //如果有背景图片 加载
                targetSceneText = IconHandler.Instance.manager.GetTextureSync($"{pathCardScene}/{creatureData.creatureInfo.card_scene}.png");
            }
            if (targetSceneText != null)
            {
                ui_Scene.ShowObj(true);
                ui_Scene.texture = targetSceneText;
            }
            else
            {
                ui_Scene.ShowObj(false);
            }
        }

        //等比例设置大小 防止裁切
        Vector2 iconScale = ui_Icon.transform.localScale;
        RectTransform iconRect = (RectTransform)ui_Icon.transform;
        iconRect.sizeDelta = new Vector2(100f / iconScale.x, 100f / iconScale.y);
    }
}