using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameUIUtil
{
    public static string pathCardScene = "Assets/LoadResources/Textures/CardScene";//卡片场景路径

    #region 卡片图标尺寸等比适配
    /// <summary>大卡(详情UI)图标容器 IconContent 标准高度(实测 300x450,ui_Icon pivot=(0.5,0))——ui_data_b/幻化 ui_show_data 的校准基准</summary>
    public const float cardContentHeightForB = 450f;
    /// <summary>小卡图标容器 IconContent 标准高度(实测 92x142,ui_Icon pivot=(0.5,0))——ui_data_s/幻化 show_data 的校准基准</summary>
    public const float cardContentHeightForS = 142f;

    /// <summary>
    /// 卡片图标尺寸等比适配:把按标准卡片容器(大卡/小卡 IconContent)校准的 scale/pos 应用到任意高度容器内的目标节点——
    /// scale/pos 同乘「目标显示框高/标准容器高」系数,还原与卡片一致的显示比例(脚底贴底语义,如 UIMainLoad 容器 340≠450 时修正显示)。
    /// 目标节点要求与卡片 ui_Icon 同 pivot(0.5,0)+父容器中心锚定,且不在 LayoutGroup 控制下(anchoredPosition 会被覆写)。
    /// 显示框高取父容器 rect.height(自身 sizeDelta 可能被 SetCreatureUIForDetails 防裁切逻辑改写,父容器才是真实显示区域)。
    /// </summary>
    /// <param name="targetUI">目标 spine 节点(其父容器=显示框)</param>
    /// <param name="scale">尺寸配置缩放(ui_data_b/ui_data_s 或幻化尺寸键解析值)</param>
    /// <param name="pos">尺寸配置偏移</param>
    /// <param name="cardContentHeight">校准基准容器高(<see cref="cardContentHeightForB"/>/<see cref="cardContentHeightForS"/>)</param>
    public static void ApplyCardIconSizeFit(RectTransform targetUI, float scale, Vector2 pos, float cardContentHeight)
    {
        float fitScale = ((RectTransform)targetUI.parent).rect.height / cardContentHeight;
        targetUI.localScale = Vector3.one * scale * fitScale;
        targetUI.anchoredPosition = pos * fitScale;
    }

    /// <summary>
    /// 卡片图标尺寸等比适配(尺寸串便捷版):sizeData 格式「scale;x,y」,解析失败=缩放1零偏移(仅乘系数)。
    /// </summary>
    public static void ApplyCardIconSizeFit(RectTransform targetUI, string sizeData, float cardContentHeight)
    {
        CreatureBean.ParseTransformSizeData(sizeData, out float scale, out Vector2 pos);
        ApplyCardIconSizeFit(targetUI, scale, pos, cardContentHeight);
    }
    #endregion

    #region 颜色工具
    /// <summary>自定义材质实例名称，用于标记已克隆的材质副本</summary>
    private const string customMatName = "MatCustom";

    /// <summary>
    /// 设置渐变颜色，支持单色和双色渐变
    /// 单色格式: "#B9B9B9"，仍通过材质属性应用，保持渲染一致
    /// 双色格式: "#B9B9B9,#B9B9B1"，通过材质的 _StartColor / _EndColor 属性设置渐变
    /// 注意：内部会自动实例化材质副本，避免污染共享材质资源
    /// </summary>
    public static void SetGradientColor(Graphic graphic, string colorStr)
    {
        if (graphic == null || colorStr.IsNull())
            return;

        // 实例化独立材质副本，避免修改共享材质资源影响其他控件
        Material mat = graphic.material;
        if (mat != null && mat.name != customMatName)
        {
            mat = new Material(mat) { name = customMatName };
            graphic.material = mat;
        }

        string[] colors = colorStr.Split(',');
        if (colors.Length >= 2)
        {
            ColorUtility.TryParseHtmlString(colors[0].Trim(), out Color startColor);
            ColorUtility.TryParseHtmlString(colors[1].Trim(), out Color endColor);
            mat.SetColor("_StartColor", startColor);
            mat.SetColor("_EndColor", endColor);
        }
        else
        {
            ColorUtility.TryParseHtmlString(colorStr.Trim(), out Color color);
            mat.SetColor("_StartColor", color);
            mat.SetColor("_EndColor", color);
        }
        graphic.color = Color.white;
    }
    #endregion

    /// <summary>
    /// 设置生物简易UI
    /// </summary>
    public static void SetCreatureUIForSimple(SkeletonGraphic ui_Icon, CreatureBean creatureData, float scale = 1)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_Icon, creatureData, isNeedWeapon: false);
        //定格待机动画第一帧:直接停在 setup pose 会把动画内才隐藏的部件(特效槽/备用附件等)全显示出来;无 idle 动画的骨架保持原姿势
        SpineHandler.Instance.SetAnimFirstFrame(ui_Icon, SpineAnimationStateEnum.Idle, creatureData);
        ui_Icon.ShowObj(true);
        //设置UI大小和坐标：幻化药自带 show 小卡尺寸(other_data 的 show_data 键,按默认展示骨架高度校准)优先:原生物 ui_data_s 按原骨架校准,不适用于 Mod 骨架
        if (creatureData.GetTransformShowData(out float transformChessScale, out Vector2 transformChessPos))
        {
            ui_Icon.transform.localScale = Vector3.one * transformChessScale * scale;
            ui_Icon.rectTransform.anchoredPosition = transformChessPos * scale;
        }
        else
        {
            creatureData.creatureModel.ChangeUISizeForS(ui_Icon.rectTransform, scale);
        }
    }

    /// <summary>
    /// 设置生物详细UI
    /// </summary>
    public static void SetCreatureUIForDetails(SkeletonGraphic ui_Icon, RawImage ui_Scene, CreatureBean creatureData,
        float customUISize = 0, float customUIPosOffsetX = 0, float customUIPosOffsetY = 0)
    {
        //设置spine(内部已应用幻化整骨替换——若生物处于幻化状态,此处的骨架已是幻化资源)
        CreatureHandler.Instance.SetCreatureData(ui_Icon, creatureData, isUIShow: true);
        //如果装备了肖像道具 使用肖像资源替换spine(Portrait 优先级最高:在幻化之后再覆盖,卸下 Portrait 后幻化自动显现)
        bool hasPortrait = false;
        ItemBean portraitItem = creatureData.GetEquip(ItemTypeEnum.Portrait);
        if (portraitItem != null)
        {
            ItemsInfoBean portraitItemInfo = ItemsInfoCfg.GetItemData(portraitItem.itemId);
            if (portraitItemInfo != null && !portraitItemInfo.other_data.IsNull())
            {
                SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, portraitItemInfo.other_data);
                hasPortrait = true;
            }
        }
        //播放动画
        string transformUIShowIdleAnim = hasPortrait ? null : creatureData.GetTransformUIShowIdleAnim();
        if (!transformUIShowIdleAnim.IsNull())
        {
            //幻化药 ui_show 骨架配置了替代待机动画(other_data 的 ui_show_idle_anim 键):框架层按名直播,绕过 GetAnimNameAppoint 防 show 段 idle_anim/原 anim_idle 误用到 ui_show 骨架
            SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, true, animNameAppoint: transformUIShowIdleAnim);
        }
        else if (!creatureData.GetTransformUIShowSpineRes().IsNull())
        {
            //详情UI显示 ui_show 幻化骨架但无替代动画键(或 Portrait 场景):不指定动画名,交框架按该骨架实际动画列表候选解析
            SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, true);
        }
        else
        {
            //非 ui_show 幻化场景(原生物/仅 show 段药详情UI显示 show 骨架):原链路(含 show 段 idle_anim 替代与 anim_idle 配置)
            SpineHandler.Instance.PlayAnim(ui_Icon, SpineAnimationStateEnum.Idle, creatureData, true);
        }
        ui_Icon.ShowObj(true);
        //设置UI大小和坐标
        if (creatureData.GetTransformUIShowData(out float transformUIScale, out Vector2 transformUIPos))
        {
            //幻化药自带 ui_show_spine 尺寸(other_data 的 ui_show_data 键,按 UIShow 骨架高度校准)优先:原生物 ui_data_b 按原骨架校准,不适用于 Mod 高清骨架
            ui_Icon.transform.localScale = Vector3.one * transformUIScale;
            ui_Icon.rectTransform.anchoredPosition = transformUIPos;
        }
        else
        {
            creatureData.creatureModel.ChangeUISizeForB(ui_Icon.rectTransform);
        }
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