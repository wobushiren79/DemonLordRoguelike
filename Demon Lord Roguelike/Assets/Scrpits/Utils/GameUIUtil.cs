using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameUIUtil
{
    public static string pathCardScene = "Assets/LoadResources/Textures/CardScene";//卡片场景路径

    /// <summary>
    /// 设置生物简易UI
    /// </summary>
    public static void SetCreatureUIForSimple(SkeletonGraphic ui_Icon, CreatureBean creatureData)
    {
        //设置骨骼数据
        SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, creatureData.creatureModel.res_name);
        string[] skinArray = creatureData.GetSkinArray(isNeedWeapon:false);
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(ui_Icon.Skeleton, skinArray);
        ui_Icon.ShowObj(true);
        //设置UI大小和坐标
        creatureData.creatureModel.ChangeUISizeForS(ui_Icon.rectTransform);
    }

    /// <summary>
    /// 设置生物详细UI
    /// </summary>
    public static void SetCreatureUIForDetails(SkeletonGraphic ui_Icon, RawImage ui_Scene, CreatureBean creatureData)
    {
        //设置骨骼数据
        creatureData.creatureModel.GetShowRes(out string resName, out int skinType);

        SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, resName);
        string[] skinArray = creatureData.GetSkinArray(skinType);
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(ui_Icon.Skeleton, skinArray);
        //播放动画
        SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, true);
        ui_Icon.ShowObj(true);
        //设置UI大小和坐标
        creatureData.creatureModel.ChangeUISizeForB(ui_Icon.rectTransform);

        //设置背景图片
        if(ui_Scene != null)
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
    }
}