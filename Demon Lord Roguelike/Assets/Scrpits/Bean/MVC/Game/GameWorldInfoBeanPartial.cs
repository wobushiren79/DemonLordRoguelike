
using UnityEngine;

public partial class GameWorldInfoBean
{
    /// <summary>
    /// 获取地图坐标
    /// </summary>
    /// <returns></returns>
    public Vector2 GetMapPosition()
    {
        if (map_pos.IsNull())
        {
            return Vector2.zero;
        }
        var posArray = map_pos.SplitForArrayFloat(',');
        return new Vector2(posArray[0], posArray[1]);
    }
}

public partial class GameWorldInfoCfg
{
}
