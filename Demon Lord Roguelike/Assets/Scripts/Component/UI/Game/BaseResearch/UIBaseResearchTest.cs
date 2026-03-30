
using System.Collections.Generic;
using UnityEngine.UI;
using static ExcelUtil;

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
        List<ExcelChangeData> listData = new List<ExcelChangeData>();
        for (int i = 0; i < listResearchItemView.Count; i++)
        {
            var itemResearchItemView = listResearchItemView[i];
            long researchId=itemResearchItemView.researchInfo.id;
            listData.Add(new ExcelChangeData(researchId,"position_x",$"{(int)itemResearchItemView.rectTransform.anchoredPosition.x}"));
            listData.Add(new ExcelChangeData(researchId,"position_y",$"{(int)itemResearchItemView.rectTransform.anchoredPosition.y}"));
        }
        ExcelUtil.SetExcelData("Assets/Data/Excel/excel_research_info[研究信息].xlsx", "ResearchInfo", listData);
    }
}