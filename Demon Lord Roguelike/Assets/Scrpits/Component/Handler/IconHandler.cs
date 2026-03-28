using UnityEngine;
using UnityEngine.UI;
using System;

public partial class IconHandler
{
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

    public void SetItemIcon(string iconName, float rotateZ, SpriteRenderer spriteRenderer, float targetSizeX = 100f, float targetSizeY = 100f)
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

                // 计算缩放比例：取宽高的较小缩放比，确保完整显示在目标区域内
                float scaleX = targetSizeX / spriteSize.x;
                float scaleY = targetSizeY / spriteSize.y;
                float scale = Mathf.Min(scaleX, scaleY);

                // 应用缩放（假设 spriteRenderer 的 transform 是独立的，或使用 lossyScale 计算）
                spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
        });
    }

    public void SetItemIcon(long itemId, Image targetIV)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, targetIV);
    }

    public void SetItemIcon(long itemId, SpriteRenderer spriteRenderer, float targetSizeX = 100f, float targetSizeY = 100f)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, spriteRenderer, targetSizeX, targetSizeY);
    }

    public void SetItemIconForAttackMode(string showSpriteName, SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
            return;
        GetIconSprite(SpriteAtlasTypeEnum.Items, showSpriteName, (sprite) =>
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                //spriteRenderer.transform.eulerAngles = new Vector3(0, 0, rotateZ);
            }
        });
    }

    /// <summary>
    /// 设置UI头像
    /// </summary>
    public void SetUIIcon(string iconName, Image targetIV)
    {
        GetIconSprite(SpriteAtlasTypeEnum.UI, iconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
            }
        });
    }
}
