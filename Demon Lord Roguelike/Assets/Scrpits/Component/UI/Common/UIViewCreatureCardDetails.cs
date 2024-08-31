using DG.Tweening;
using UnityEngine;

public partial class UIViewCreatureCardDetails : BaseUIView
{
    public CreatureBean creatureData;

    protected static string pathCardScene = "Assets/LoadResources/Textures/CardScene";//��Ƭ����·��

    /// <summary>
    /// ��������
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
    /// ����ϡ�ж�
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
    /// ��������
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// ���õȼ�
    /// </summary>
    public void SetLevel(int level)
    {
        ui_Level.text = $"{level}";
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetAttribute(int attDamage, int lifeMax)
    {
        ui_AttributeItemText_Att.text = $"{attDamage}";
        ui_AttributeItemText_Life.text = $"{lifeMax}";
    }

    /// <summary>
    /// ����ͼƬ
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        var creatureInfo = creatureData.GetCreatureInfo();
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        //���ù�������
        string resName = creatureModel.res_name;

        int skinType = 0;
        if (!creatureModel.ui_show_spine.IsNull())
        {
            resName = creatureModel.ui_show_spine;
            skinType = 1;
        }

        SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, resName);
        string[] skinArray = creatureData.GetSkinArray(skinType);
        //�޸�Ƥ��
        SpineHandler.Instance.ChangeSkeletonSkin(ui_Icon.Skeleton, skinArray);

        SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, true);
        ui_Icon.ShowObj(true);
        //����UI��С������
        if (creatureModel.ui_data_b.IsNull())
        {
            ui_Icon.rectTransform.anchoredPosition = Vector2.zero;
            ui_Icon.rectTransform.localScale = Vector3.one;
        }
        else
        {
            string[] uiDataStr = creatureModel.ui_data_b.Split(';');
            ui_Icon.rectTransform.localScale = Vector3.one * float.Parse(uiDataStr[0]);

            float[] uiDataPosStr = uiDataStr[1].SplitForArrayFloat(',');
            ui_Icon.rectTransform.anchoredPosition = new Vector2(uiDataPosStr[0], uiDataPosStr[1]);
        }

        //���ñ���ͼƬ
        Texture2D targetSceneText = null;
        if (creatureInfo.card_scene.IsNull())
        {
            //���û�б���ͼƬ ʹ��ͨ��
            targetSceneText = IconHandler.Instance.manager.GetTextureSync($"{pathCardScene}/Card_Scene_4.png");
        }
        else
        {
            //����б���ͼƬ ����
            targetSceneText = IconHandler.Instance.manager.GetTextureSync($"{pathCardScene}/{creatureInfo.card_scene}.png");
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
