
using System.Collections.Generic;
using UnityEngine.UI;

public partial class UIBaseResearch
{
    protected bool isTest = false;//是否是测试

    /// <summary>
    /// 设置测试数据
    /// </summary>
    public void SetDataForTest()
    {
        isTest = true;
        SetData();
        ui_TestSaveBtn.gameObject.SetActive(true);
    }

    /// <summary>
    /// 点击测试
    /// </summary>
    public void OnClickForButtonTest(Button viewButton)
    {
        if (viewButton == ui_TestSaveBtn)
        {
            SaveResearchDataForTest();
        }
    }

    /// <summary>
    /// 保存研究数据
    /// </summary>
    public void SaveResearchDataForTest()
    {
        for (int i = 0; i < listResearchItemView.Count; i++)
        {

        }
    }
}