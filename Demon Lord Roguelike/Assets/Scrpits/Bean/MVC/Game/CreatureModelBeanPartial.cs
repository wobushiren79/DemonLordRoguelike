using System;
using System.Collections.Generic;
using UnityEngine;
public partial class CreatureModelBean
{
    /// <summary>
    /// ��ȡչʾ��Դ
    /// </summary>
    public void GetShowRes(out string resName,out int skinType)
    {
        skinType = 0;
        if (!ui_show_spine.IsNull())
        {
            resName = ui_show_spine;
            skinType = 1;
        }
        else
        {
            resName = res_name;
        }
    }

    /// <summary>
    /// �ı�UI��С
    /// </summary>
    public void ChangeUISizeForS(RectTransform targetUI)
    {
        //����UI��С������
        if (ui_data_s.IsNull())
        {
            targetUI.anchoredPosition = Vector2.zero;
            targetUI.localScale = Vector3.one;
        }
        else
        {
            string[] uiDataStr = ui_data_s.Split(';');
            targetUI.localScale = Vector3.one * float.Parse(uiDataStr[0]);

            float[] uiDataPosStr = uiDataStr[1].SplitForArrayFloat(',');
            targetUI.anchoredPosition = new Vector2(uiDataPosStr[0], uiDataPosStr[1]);
        }
    }

    /// <summary>
    /// �ı�UI��С
    /// </summary>
    public void ChangeUISizeForB(RectTransform targetUI)
    {
        //����UI��С������
        if (ui_data_b.IsNull())
        {
            targetUI.anchoredPosition = Vector2.zero;
            targetUI.localScale = Vector3.one;
        }
        else
        {
            string[] uiDataStr = ui_data_b.Split(';');
            targetUI.localScale = Vector3.one * float.Parse(uiDataStr[0]);

            float[] uiDataPosStr = uiDataStr[1].SplitForArrayFloat(',');
            targetUI.anchoredPosition = new Vector2(uiDataPosStr[0], uiDataPosStr[1]);
        }
    }
}

public partial class CreatureModelCfg
{


}
