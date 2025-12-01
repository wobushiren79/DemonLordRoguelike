using UnityEngine;
using UnityEngine.UI;

public partial class IconHandler
{
    /// <summary>
    /// 设置皮肤图标
    /// </summary>
    public void SetSkinIcon(string iconName, Image targetIV)
    {
        GetIconSprite(SpriteAtlasType.Skins, iconName, (sprite) =>
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
        GetIconSprite(SpriteAtlasType.Items, iconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
                targetIV.transform.eulerAngles = new Vector3(0, 0, rotateZ);
            }
        });
    }

    public void SetItemIcon(string iconName, float rotateZ, SpriteRenderer spriteRenderer)
    {
        GetIconSprite(SpriteAtlasType.Items, iconName, (sprite) =>
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.transform.eulerAngles = new Vector3(0, 0, rotateZ);
            }
        });
    }

    public void SetItemIcon(long itemId, Image targetIV)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, targetIV);
    }

    public void SetItemIcon(long itemId, SpriteRenderer spriteRenderer)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, spriteRenderer);
    }

    public void SetItemIconForAttackMode(string showSpriteName, SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
            return;
        GetIconSprite(SpriteAtlasType.Items, showSpriteName, (sprite) =>
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
        GetIconSprite(SpriteAtlasType.UI, iconName, (sprite) =>
        {
            if (targetIV != null)
            {
                targetIV.sprite = sprite;
            }
        });
    }
}
