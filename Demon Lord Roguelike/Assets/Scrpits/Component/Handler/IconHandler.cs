using UnityEngine;
using UnityEngine.UI;

public partial class IconHandler
{

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

    public void SetItemIcon(long itemId, Image targetIV)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, itemInfo.icon_rotate_z, targetIV);
    }
}
