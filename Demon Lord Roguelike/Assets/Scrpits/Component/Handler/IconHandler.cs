using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;
using UnityEngine.UI;

public partial class IconHandler
{

    /// <summary>
    /// 设置道具图标
    /// </summary>
    public void SetItemIcon(string iconName, Image targetIV)
    {
        GetIconSprite(SpriteAtlasType.Items, iconName, (sprite) =>
        {
            if (targetIV != null)
                targetIV.sprite = sprite;
        });
    }

    public void SetItemIcon(long itemId, Image targetIV)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        SetItemIcon(itemInfo.icon_res, targetIV);
    }
}
