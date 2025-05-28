using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffInfoBean
{
    protected Color colorBody = Color.white;

    public Color GetBodyColor()
    {
        if (color_body.IsNull())
        {
            return Color.white;
        }
        else
        {
            if (colorBody == Color.white)
            {
                ColorUtility.TryParseHtmlString($"{color_body}", out Color targetColor);
                colorBody = targetColor;
            }
            return colorBody;
        }
    }
}

public partial class BuffInfoCfg
{
}
