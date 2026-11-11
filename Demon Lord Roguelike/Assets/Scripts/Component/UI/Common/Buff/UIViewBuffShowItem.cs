using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;


public partial class UIViewBuffShowItem : BaseUIView
{
    public BuffBean buffData;
    public BuffInfoBean buffInfo;

    #region 数据绑定

    /// <summary>
    /// 设置 BUFF 描述和提示，并为整条满值的稀有度词条启用扫光和数字标金。
    /// </summary>
    /// <param name="buffData">显示的 BUFF 数据。</param>
    /// <param name="isRarityBuff">是否为魔物自身的稀有度词条。</param>
    public void SetData(BuffBean buffData, bool isRarityBuff = false)
    {
        this.buffData = buffData;
        this.buffInfo = BuffInfoCfg.GetItemData(buffData.id);
        var rarityEnum = buffInfo.GetRarity();
        var rarityInfo = RarityInfoCfg.GetItemData(rarityEnum);
        ColorUtility.TryParseHtmlString(rarityInfo.buff_color, out Color buffColor);

        //设置名字颜色
        ui_BuffName.color = buffColor;
        //设置背景颜色
        ui_UIViewBuffShowItem_Image.color = buffColor;
        //设置提示弹窗
        Dictionary<TextReplaceEnum, string> dicReplace = new Dictionary<TextReplaceEnum, string>()
        {
            {TextReplaceEnum.Percentage, $"{MathUtil.GetPercentage(buffData.trigger_value_rate, 2)}"},
            {TextReplaceEnum.Time_S, $"{Mathf.FloorToInt(buffData.trigger_time)}"},
            {TextReplaceEnum.Value, $"{Mathf.FloorToInt(buffData.trigger_value)}"},
        };
        var preInfo = buffInfo.GetPreInfo();
        if (!preInfo.IsNull())
        {
            foreach (var itemData in preInfo)
            {
                var buffPreInfo = BuffPreInfoCfg.GetItemData(itemData.Key);
                var buffPreEntity = BuffHandler.Instance.manager.GetBuffPreEntity(buffPreInfo);
                if (buffPreEntity is BuffPreEntityForKillNum)
                {         
                    dicReplace.Add(TextReplaceEnum.KillNum, $"{Mathf.FloorToInt(itemData.Value)}");
                }
                else if (buffPreEntity is BuffPreEntityForUnderAttackDamage)
                {
                    dicReplace.Add(TextReplaceEnum.UnderAttackDamage, $"{Mathf.FloorToInt(itemData.Value)}");
                }    
                else if (buffPreEntity is BuffPreEntityForAttackDamage)
                {
                    dicReplace.Add(TextReplaceEnum.AttackDamage, $"{Mathf.FloorToInt(itemData.Value)}");
                }
                else if (buffPreEntity is BuffPreEntityForHPRateLess)
                {
                    dicReplace.Add(TextReplaceEnum.HPRateLess, $"{MathUtil.GetPercentage(itemData.Value, 2)}");
                }
                else if (buffPreEntity is BuffPreEntityForRegainHPReceived)
                {
                    dicReplace.Add(TextReplaceEnum.RegainHPReceived, $"{Mathf.FloorToInt(itemData.Value)}");
                }
                else if (buffPreEntity is BuffPreEntityForRegainHPCast)
                {
                    dicReplace.Add(TextReplaceEnum.RegainHPCast, $"{Mathf.FloorToInt(itemData.Value)}");
                }
                else if (buffPreEntity is BuffPreEntityForOnFieldTime)
                {
                    dicReplace.Add(TextReplaceEnum.OnFieldTime, $"{Mathf.FloorToInt(itemData.Value)}");
                }
            }
        }

        var (isValueMaxRoll, isRateMaxRoll, isFullRoll) = isRarityBuff ? buffData.GetMaxRollFlags() : (false, false, false);
        if (isFullRoll)
        {
            if (isValueMaxRoll)
                dicReplace[TextReplaceEnum.Value] = FormatGoldNumber(dicReplace[TextReplaceEnum.Value]);
            if (isRateMaxRoll)
                dicReplace[TextReplaceEnum.Percentage] = FormatGoldNumber(dicReplace[TextReplaceEnum.Percentage]);
        }

        string contentText = TextHandler.Instance.GetTextReplace(buffInfo.content_language, dicReplace);
        //名字栏改为显示效果详情，前面拼接 ">" 前缀
        ui_BuffName.text = $"> {contentText}";
        showBuffDescriptionShine = isFullRoll;
        if (isFullRoll && isActiveAndEnabled)
            ApplyBuffDescriptionShine();
        else if (!isFullRoll)
            RestoreBuffDescriptionStyle();
        //提示弹窗改为显示名字
        ui_UIViewBuffShowItem_PopupButtonCommonView.SetData(buffInfo.name_language, PopupEnum.Text);
    }

    /// <summary>
    /// 为数值占位符的替换文本添加 TMP 金色标签。
    /// </summary>
    private static string FormatGoldNumber(string number)
    {
        return $"<color=#FFD36C>{number}</color>";
    }

    #endregion

    #region 满值词条扫光

    private const float BuffShineSweepDuration = 0.6f;
    private const float BuffShineCycleDuration = 1.8f;
    private const float BuffShineCoreLeadingWidth = 0.05f;
    private const float BuffShineCoreTrailingWidth = 0.095f;
    private const float BuffShineTailWidth = 0.38f;
    private const float BuffShineTextureScaleX = BuffShineSweepDuration / BuffShineCycleDuration;

    private static readonly Color BuffShineTailColor = new Color(1f, 0.74f, 0.25f, 1f);
    private static readonly int BuffFaceTextureId = Shader.PropertyToID("_FaceTex");
    private static readonly int BuffFaceUvSpeedXId = Shader.PropertyToID("_FaceUVSpeedX");
    private static readonly Dictionary<(Material source, Color32 tint), (Material material, Texture2D texture)> buffDescriptionShineStyles =
        new Dictionary<(Material source, Color32 tint), (Material material, Texture2D texture)>();
    private static int buffDescriptionShineStyleUsers;

    private Material buffDescriptionSourceMaterial;
    private TextureMappingOptions buffDescriptionSourceMapping;
    private bool showBuffDescriptionShine;
    private bool usesBuffDescriptionShineStyles;

    /// <summary>
    /// 行显示后应用已绑定的扫光，兼容在隐藏面板内提前绑定。
    /// </summary>
    public override void OnEnable()
    {
        base.OnEnable();
        if (showBuffDescriptionShine)
            ApplyBuffDescriptionShine();
    }

    /// <summary>
    /// 应用按行映射的扫光，所有 BUFF 行共享同源材质及字面色的副本。
    /// </summary>
    private void ApplyBuffDescriptionShine()
    {
        Material source = buffDescriptionSourceMaterial != null ? buffDescriptionSourceMaterial : ui_BuffName.fontSharedMaterial;
        if (source == null || !source.HasProperty(BuffFaceTextureId)
            || !source.HasProperty(BuffFaceUvSpeedXId) || !source.HasProperty(ShaderUtilities.ID_FaceColor))
        {
            return;
        }

        Color32 tint = ui_BuffName.color;
        var styleKey = (source, tint);
        if (!buffDescriptionShineStyles.TryGetValue(styleKey, out var shineStyle))
        {
            Color faceTint = (Color)tint * source.GetColor(ShaderUtilities.ID_FaceColor);
            Texture2D texture = CreateBuffDescriptionShineTexture(faceTint);
            shineStyle = (CreateBuffDescriptionShineMaterial(source, texture), texture);
            buffDescriptionShineStyles.Add(styleKey, shineStyle);
        }

        if (buffDescriptionSourceMaterial == null)
        {
            buffDescriptionSourceMaterial = source;
            buffDescriptionSourceMapping = ui_BuffName.horizontalMapping;
        }
        if (!usesBuffDescriptionShineStyles)
        {
            usesBuffDescriptionShineStyles = true;
            buffDescriptionShineStyleUsers++;
        }

        ui_BuffName.horizontalMapping = TextureMappingOptions.Line;
        ui_BuffName.fontSharedMaterial = shineStyle.material;
    }

    /// <summary>
    /// 复用行切到普通词条时恢复该行自己的材质和文字映射。
    /// </summary>
    private void RestoreBuffDescriptionStyle()
    {
        if (buffDescriptionSourceMaterial == null)
            return;

        ui_BuffName.fontSharedMaterial = buffDescriptionSourceMaterial;
        ui_BuffName.horizontalMapping = buffDescriptionSourceMapping;
        buffDescriptionSourceMaterial = null;
    }

    /// <summary>
    /// 配置 TMP 原生字面滚动与细金色光晕。
    /// </summary>
    private static Material CreateBuffDescriptionShineMaterial(Material source, Texture2D texture)
    {
        var material = new Material(source) { hideFlags = HideFlags.DontSave };
        material.SetTexture(BuffFaceTextureId, texture);
        // 按扫过时长占循环周期的比例缩放 UV，负速度使亮带从左向右滚动。
        material.SetTextureScale(BuffFaceTextureId, new Vector2(BuffShineTextureScaleX, 1f));
        material.SetFloat(BuffFaceUvSpeedXId, -1f / BuffShineCycleDuration);

        if (material.HasProperty(ShaderUtilities.ID_GlowColor))
        {
            material.SetColor(ShaderUtilities.ID_GlowColor, new Color(1f, 0.74f, 0.32f, 0.35f));
            material.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
            material.SetFloat(ShaderUtilities.ID_GlowInner, 0.035f);
            material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.09f);
            material.SetFloat(ShaderUtilities.ID_GlowPower, 0.7f);
            material.EnableKeyword(ShaderUtilities.Keyword_Glow);
        }

        return material;
    }

    /// <summary>
    /// 创建循环贴图，按底色反推 TMP 乘色所需的白亮芯和金色拖尾。
    /// </summary>
    private static Texture2D CreateBuffDescriptionShineTexture(Color faceTint)
    {
        const int width = 256;
        var texture = new Texture2D(width, 1, TextureFormat.RGBAHalf, false, true)
        {
            name = "BUFF 描述扫光",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        Color coreMultiplier = GetBuffShineColorMultiplier(Color.white, faceTint);
        Color tailMultiplier = GetBuffShineColorMultiplier(BuffShineTailColor, faceTint);
        var pixels = new Color[width];
        for (int x = 0; x < width; x++)
        {
            float offsetFromCore = ((x + 0.5f) / width - 0.5f) / BuffShineTextureScaleX;
            bool isTrailing = offsetFromCore < 0f;
            float distanceFromCore = Mathf.Abs(offsetFromCore);
            float coreWidth = isTrailing ? BuffShineCoreTrailingWidth : BuffShineCoreLeadingWidth;
            float core = 1f - Mathf.SmoothStep(0f, 1f, distanceFromCore / coreWidth);

            // 亮芯向右移动，金色拖尾占用左侧的采样区间。
            Color colorUnderCore = Color.white;
            if (isTrailing)
            {
                float tailPosition = (distanceFromCore - BuffShineCoreTrailingWidth)
                    / (BuffShineTailWidth - BuffShineCoreTrailingWidth);
                float tail = 1f - Mathf.SmoothStep(0f, 1f, tailPosition);
                colorUnderCore = Color.Lerp(Color.white, tailMultiplier, tail);
            }
            pixels[x] = Color.Lerp(colorUnderCore, coreMultiplier, core);
        }
        texture.SetPixels(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return texture;
    }

    /// <summary>
    /// 计算字面 RGB 到达高光目标色所需的乘数，保持字面透明度。
    /// </summary>
    private static Color GetBuffShineColorMultiplier(Color target, Color faceTint)
    {
        const float minChannel = 1f / byte.MaxValue;
        return new Color(
            target.r / Mathf.Max(faceTint.r, minChannel),
            target.g / Mathf.Max(faceTint.g, minChannel),
            target.b / Mathf.Max(faceTint.b, minChannel),
            1f);
    }

    /// <summary>
    /// 最后一个使用过扫光的行销毁时，释放所有共享材质和贴图。
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!usesBuffDescriptionShineStyles)
            return;

        buffDescriptionShineStyleUsers--;
        if (buffDescriptionShineStyleUsers > 0)
            return;

        foreach (var shineStyle in buffDescriptionShineStyles.Values)
        {
            Destroy(shineStyle.material);
            Destroy(shineStyle.texture);
        }
        buffDescriptionShineStyles.Clear();
    }

    #endregion
}
