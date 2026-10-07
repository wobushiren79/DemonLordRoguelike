using UnityEngine;
using UnityEngine.UI;
using System;

public partial class IconHandler
{
    /// <summary>道具贴图的基准 PPU：世界空间道具 sprite 的观感是按 PPU=100 调试的（100px=1 世界单位）；贴图 PPU 统一改为 16 后，按 当前PPU/基准PPU 反比补偿缩放钉住世界尺寸</summary>
    public const float ItemSpriteBasePPU = 100f;

    /// <summary>
    /// 获取图标（游戏层枚举重载）。内部转字符串后调用框架层 GetIconSprite。
    /// </summary>
    public void GetIconSprite(SpriteAtlasTypeEnum spriteAtlasType, string spriteName, Action<Sprite> callBack)
    {
        GetIconSprite(spriteAtlasType.ToAtlasTag(), spriteName, callBack);
    }

    /// <summary>
    /// 解析图标名称，获取图集类型和实际图标名
    /// 格式：icon_name,AtlasType  例如：icon_001,UI 或 icon_002,Skins
    /// </summary>
    /// <param name="iconName">原始图标名称</param>
    /// <param name="defaultType">默认图集类型</param>
    /// <param name="actualIconName">实际图标名称（去除后缀）</param>
    /// <returns>图集类型</returns>
    private SpriteAtlasTypeEnum ParseIconName(string iconName, SpriteAtlasTypeEnum defaultType, out string actualIconName)
    {
        actualIconName = iconName;
        if (string.IsNullOrEmpty(iconName))
            return defaultType;

        // 查找最后一个逗号的位置
        int commaIndex = iconName.LastIndexOf(',');
        if (commaIndex <= 0 || commaIndex >= iconName.Length - 1)
            return defaultType;

        string suffix = iconName.Substring(commaIndex + 1);
        string nameWithoutSuffix = iconName.Substring(0, commaIndex);

        // 尝试解析后缀为枚举值
        if (Enum.TryParse<SpriteAtlasTypeEnum>(suffix, out var atlasType))
        {
            actualIconName = nameWithoutSuffix;
            return atlasType;
        }

        return defaultType;
    }
    /// <summary>
    /// 设置皮肤图标
    /// </summary>
    public void SetSkinIcon(string iconName, Image targetIV)
    {
        GetIconSprite(SpriteAtlasTypeEnum.Skins, iconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
            }
        });
    }

    /// <summary>
    /// 设置道具图标
    /// </summary>
    public void SetItemIcon(string iconName, float rotateZ, Image targetIV)
    {
        SpriteAtlasTypeEnum atlasType = ParseIconName(iconName, SpriteAtlasTypeEnum.Items, out string actualIconName);
        GetIconSprite(atlasType, actualIconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
                targetIV.transform.eulerAngles = new Vector3(0, 0, rotateZ);
            }
        });
    }

    /// <summary>
    /// 设置道具图标（世界空间 SpriteRenderer 版）：把 sprite 完整缩放进 targetSize×targetSize 的像素框内（contain 取较小缩放比）。
    /// <para>尺寸基准 PPU=100（<see cref="ItemSpriteBasePPU"/>）：世界尺寸 = targetSize÷100，与贴图实际 PPU 解耦——默认 100 即 1 世界单位；
    /// targetSize≤0 表示按 sprite 原始像素尺寸显示（世界尺寸=像素÷100，弹道换图用）。</para>
    /// </summary>
    /// <param name="scaleMul">额外整体缩放（如弹道武器 StartSize），默认 1</param>
    public void SetItemIcon(string iconName, float rotateZ, SpriteRenderer spriteRenderer, float targetSizeX = 100f, float targetSizeY = 100f, float scaleMul = 1f)
    {
        SpriteAtlasTypeEnum atlasType = ParseIconName(iconName, SpriteAtlasTypeEnum.Items, out string actualIconName);
        GetIconSprite(atlasType, actualIconName, (sprite) =>
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, rotateZ);

                // 获取 Sprite 的原始像素尺寸
                Vector2 spriteSize = sprite.rect.size;

                // 计算缩放比例：targetSize>0 时 contain 进目标像素框(取宽高较小比)；≤0 按原始像素尺寸(scale=1)
                float scaleX = targetSizeX > 0 ? targetSizeX / spriteSize.x : 1f;
                float scaleY = targetSizeY > 0 ? targetSizeY / spriteSize.y : 1f;
                float scale = Mathf.Min(scaleX, scaleY);

                // PPU 基准补偿：SpriteRenderer 世界尺寸=像素÷贴图PPU，乘 贴图PPU/基准100 后世界尺寸只由 targetSize 决定、与贴图 PPU 解耦
                float ppuFix = sprite.pixelsPerUnit / ItemSpriteBasePPU;

                // 应用缩放（假设 spriteRenderer 的 transform 是独立的，或使用 lossyScale 计算）
                spriteRenderer.transform.localScale = new Vector3(scale * ppuFix * scaleMul, scale * ppuFix * scaleMul, 1f);
            }
        });
    }

    public void SetItemIcon(long itemId, Image targetIV)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, targetIV);
    }

    public void SetItemIcon(long itemId, SpriteRenderer spriteRenderer, float targetSizeX = 100f, float targetSizeY = 100f, float scaleMul = 1f)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, spriteRenderer, targetSizeX, targetSizeY, scaleMul);
    }

    /// <summary>
    /// 设置UI头像。默认图集为 UI；个别图标借用其他图集（如孕育模式图标已挪入研究图集）
    /// 可通过 "iconName,Research" 等后缀强制指定图集加载。
    /// </summary>
    public void SetUIIcon(string iconName, Image targetIV)
    {
        SpriteAtlasTypeEnum atlasType = ParseIconName(iconName, SpriteAtlasTypeEnum.UI, out string actualIconName);
        GetIconSprite(atlasType, actualIconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
            }
        });
    }

    /// <summary>
    /// 设置深渊馈赠图标
    /// </summary>
    public void SetAbyssalBlessingIcon(string iconName, Image targetIV)
    {
        GetIconSprite(SpriteAtlasTypeEnum.AbyssalBlessing, iconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
            }
        });
    }

    /// <summary>
    /// 设置研究图标。研究图标统一存放于 AtlasForResearch，默认图集为 Research；
    /// 个别研究借用其他图集的图标可通过 "iconName,UI" 等后缀强制指定图集。
    /// </summary>
    public void SetResearchIcon(string iconName, Image targetIV)
    {
        SpriteAtlasTypeEnum atlasType = ParseIconName(iconName, SpriteAtlasTypeEnum.Research, out string actualIconName);
        GetIconSprite(atlasType, actualIconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
            }
        });
    }
}
