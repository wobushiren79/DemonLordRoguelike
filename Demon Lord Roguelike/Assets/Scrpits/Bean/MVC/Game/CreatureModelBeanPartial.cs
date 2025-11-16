using System;
using System.Collections.Generic;
using UnityEngine;
public partial class CreatureModelBean
{
    /// <summary>
    /// 获取展示资源
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
    /// 改变UI大小
    /// </summary>
    public void ChangeUISizeForS(RectTransform targetUI, float scale = 1)
    {
        //设置UI大小和坐标
        if (ui_data_s.IsNull())
        {
            targetUI.anchoredPosition = Vector2.zero;
            targetUI.localScale = Vector3.one;
        }
        else
        {
            string[] uiDataStr = ui_data_s.Split(';');
            targetUI.localScale = Vector3.one * float.Parse(uiDataStr[0]) * scale;

            Vector2 uiDataPos = uiDataStr[1].SplitForVector2(',') * scale;
            targetUI.anchoredPosition = uiDataPos;
        }
    }

    /// <summary>
    /// 改变UI大小
    /// </summary>
    public void ChangeUISizeForB(RectTransform targetUI)
    {
        //设置UI大小和坐标
        if (ui_data_b.IsNull())
        {
            targetUI.anchoredPosition = Vector2.zero;
            targetUI.localScale = Vector3.one;
        }
        else
        {
            string[] uiDataStr = ui_data_b.Split(';');
            targetUI.localScale = Vector3.one * float.Parse(uiDataStr[0]);

            Vector2 uiDataPos = uiDataStr[1].SplitForVector2(',');
            targetUI.anchoredPosition = uiDataPos;
        }
    }
}

public partial class CreatureModelCfg
{


}
