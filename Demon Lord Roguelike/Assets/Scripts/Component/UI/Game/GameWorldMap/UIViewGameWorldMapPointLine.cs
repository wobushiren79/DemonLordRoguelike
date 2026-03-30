using UnityEngine;

public partial class UIViewGameWorldMapPointLine : BaseUIView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(Vector2 lineStartPosition, Vector2 lineEndPosition)
    {
        SetLine(lineStartPosition, lineEndPosition);
    }

    /// <summary>
    /// 设置连线Transform
    /// </summary>
    public void SetLine(Vector2 lineStartPosition, Vector2 lineEndPosition)
    {
        //获取2点数据
        Vector2 centerPosition = (lineStartPosition + lineEndPosition) / 2f;

        float lineLength = Vector2.Distance(lineStartPosition, lineEndPosition);
        // 计算直线AB相对于X轴的倾斜角度
        float lineAngle = VectorUtil.GetAngleForXLine(lineStartPosition, lineEndPosition);

        //设置点位坐标
        rectTransform.anchoredPosition = centerPosition;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localEulerAngles = new Vector3(0, 0, 0);

        ui_Line.rectTransform.anchoredPosition = Vector2.zero;
        ui_Line.rectTransform.sizeDelta = new Vector2(lineLength, 10);
        ui_Line.rectTransform.localEulerAngles = new Vector3(0, 0, lineAngle);
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public void SetState(int state)
    {
        //错过的点
        if (state == 1)
        {
            ui_Line.color = Color.gray;
        }
        //走过的点
        else if (state == 2)
        {
            ui_Line.color = Color.green;
        }
        else
        {
            ColorUtility.TryParseHtmlString("#BD7A49", out Color targetColor);
            ui_Line.color = targetColor;
        }
    }
}
