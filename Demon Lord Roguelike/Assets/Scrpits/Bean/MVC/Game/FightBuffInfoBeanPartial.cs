using UnityEngine;

public partial class FightBuffInfoBean
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

public partial class FightBuffInfoCfg
{

}