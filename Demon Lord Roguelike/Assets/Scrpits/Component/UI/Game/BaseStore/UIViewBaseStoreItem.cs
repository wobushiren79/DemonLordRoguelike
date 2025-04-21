
using UnityEngine;

public partial class UIViewBaseStoreItem : BaseUIView
{

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(StoreInfoBean storeInfo)
    {
        SetPosition(new Vector2(storeInfo.position_x, storeInfo.position_y));
    }

    /// <summary>
    /// 设置位置
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }
}