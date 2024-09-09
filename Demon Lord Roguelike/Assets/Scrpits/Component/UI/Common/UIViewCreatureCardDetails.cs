using DG.Tweening;
using UnityEngine;

public partial class UIViewCreatureCardDetails : BaseUIView
{
    public CreatureBean creatureData;

    protected static string pathCardScene = "Assets/LoadResources/Textures/CardScene";//卡片场景路径

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="creatureData"></param>
    public void SetData(CreatureBean creatureData)
    {
        this.creatureData = creatureData;

        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetLevel(creatureData.level);

        int attDamage = creatureData.GetAttDamage();
        int lifeMax = creatureData.GetLife();
        SetAttribute(attDamage, lifeMax);
        SetRarity(creatureData.rarity);
    }

    /// <summary>
    /// 设置稀有度
    /// </summary>
    public void SetRarity(int rarity)
    {
        if (rarity == 0)
            rarity = 1;
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        ColorUtility.TryParseHtmlString(rarityInfo.ui_board_color, out Color boardColor);
        ui_CardBgBoard.color = boardColor;
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置等级
    /// </summary>
    public void SetLevel(int level)
    {
        ui_Level.text = $"{level}";
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    public void SetAttribute(int attDamage, int lifeMax)
    {
        ui_AttributeItemText_Att.text = $"{attDamage}";
        ui_AttributeItemText_Life.text = $"{lifeMax}";
    }

    /// <summary>
    /// 设置图片
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
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
            ui_CardScene.ShowObj(true);
            ui_CardScene.texture = targetSceneText;
        }
        else
        {
            ui_CardScene.ShowObj(false);
        }
    }
}
