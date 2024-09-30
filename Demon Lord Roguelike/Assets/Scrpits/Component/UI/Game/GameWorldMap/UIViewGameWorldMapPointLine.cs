using UnityEngine;

public partial class UIViewGameWorldMapPointLine : BaseUIView
{
    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(Vector2 lineStartPosition, Vector2 lineEndPosition)
    {
        SetLine(lineStartPosition, lineEndPosition);
    }

    /// <summary>
    /// ��������Transform
    /// </summary>
    public void SetLine(Vector2 lineStartPosition, Vector2 lineEndPosition)
    {
        //��ȡ2������
        Vector2 centerPosition = (lineStartPosition + lineEndPosition) / 2f;

        float lineLength = Vector2.Distance(lineStartPosition, lineEndPosition);
        // ����ֱ��AB�����X�����б�Ƕ�
        float lineAngle = VectorUtil.GetAngleForXLine(lineStartPosition, lineEndPosition);

        //���õ�λ����
        rectTransform.anchoredPosition = centerPosition;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localEulerAngles = new Vector3(0, 0, 0);

        ui_Line.rectTransform.anchoredPosition = Vector2.zero;
        ui_Line.rectTransform.sizeDelta = new Vector2(lineLength, 10);
        ui_Line.rectTransform.localEulerAngles = new Vector3(0, 0, lineAngle);
    }
}
