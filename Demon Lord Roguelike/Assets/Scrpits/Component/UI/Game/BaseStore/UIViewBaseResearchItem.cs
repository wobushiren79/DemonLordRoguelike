
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Unity.Burst.Intrinsics;
using UnityEngine;

public partial class UIViewBaseResearchItem : BaseUIView
{
    protected ResearchInfoBean researchInfo;
    protected Vector2 itemPosition;
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ResearchInfoBean researchInfo)
    {
        this.researchInfo = researchInfo;
        long[] preResearchIds = researchInfo.GetPreResearchIds();
        itemPosition = new Vector2(researchInfo.position_x, researchInfo.position_y);
        SetPosition(itemPosition);
        SetLine(preResearchIds);
    }

    /// <summary>
    /// 设置位置
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }

    /// <summary>
    /// 设置连线
    /// </summary>
    public void SetLine(long[] preResearchIds)
    {
        //先隐藏所有连线
        for (int i = 0; i < ui_Line.transform.childCount; i++)
        {
            ui_Line.GetChild(i).gameObject.SetActive(false);
        }
        preResearchIds.ForEach((index, value) =>
        {
            var researchInfo = ResearchInfoCfg.GetItemData(value);
            var lineTF = (RectTransform)ui_Line.GetChild(index);
            lineTF.gameObject.SetActive(true);
            Vector2 originalPosition = new Vector2(researchInfo.position_x, researchInfo.position_y);
            //设置线段位置
            Vector2 targetPosition = UGUIUtil.GetRootPosForUI(originalPosition, (RectTransform)transform.parent, ui_Line);
            lineTF.anchoredPosition = targetPosition;
            //设置线段长度
            float lineLength = Vector2.Distance(itemPosition, originalPosition);
            lineTF.sizeDelta = new Vector2(lineLength, 8);
            //设置线段角度
            float angle = VectorUtil.GetAngleForXLine(originalPosition, itemPosition);
            lineTF.transform.eulerAngles = new Vector3(0, 0, angle);
        });
    }
}