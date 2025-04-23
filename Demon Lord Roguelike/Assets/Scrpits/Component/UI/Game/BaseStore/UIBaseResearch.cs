

using System.Collections.Generic;
using System.Linq;

public partial class UIBaseResearch : BaseUIComponent
{
    List<UIViewBaseResearchItem> listResearchItemView = new List<UIViewBaseResearchItem>();
    public override void CloseUI()
    {
        base.CloseUI();
        ClearData(true);
    }


    public override void OpenUI()
    {
        base.OpenUI();
        InitResearchItems(ResearchInfoTypeEnum.Strengthen);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void InitResearchItems(ResearchInfoTypeEnum researchInfoType)
    {        
        ClearData(false);
        List<ResearchInfoBean> listData = ResearchInfoCfg.GetResearchInfoByType(researchInfoType);

        listData.ForEach((index, itemData) =>
        {
            UIViewBaseResearchItem itemView;
            if (index < listResearchItemView.Count)
            {
                 itemView = listResearchItemView[index];
            }
            else
            {
                var newResearchItemObj = Instantiate(ui_Content.gameObject, ui_UIViewBaseResearchItem.gameObject);
                itemView =  newResearchItemObj.GetComponent<UIViewBaseResearchItem>();
            }
            itemView.gameObject.SetActive(true);
            itemView.SetData(itemData);
        });
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    /// <param name="isDestory"></param>
    public void ClearData(bool isDestory)
    {
        listResearchItemView.ForEach((index, itemView) =>
        {
            if (isDestory)
            {
                DestroyImmediate(itemView.gameObject);
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        });
        if (isDestory)
        {
            listResearchItemView.Clear();
        }
    }
}