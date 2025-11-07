

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public partial class UIDoomCouncilMain : BaseUIComponent
{
    public List<DoomCouncilInfoBean> listShowData = new List<DoomCouncilInfoBean>();

    public override void Awake()
    {
        base.Awake();
        ui_List.AddCellListener(OnCellChange);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_List.SetCellCount(0);
        ui_List.ClearAllCell();
    }

    public override void OpenUI()
    {
        base.OpenUI();
        InitData();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        var arrayData = DoomCouncilInfoCfg.GetAllArrayData();
        listShowData = arrayData.ToList();

        ui_List.SetCellCount(listShowData.Count);
    }

    /// <summary>
    /// 设置列表数据
    /// </summary>
    public void OnCellChange(ScrollGridCell itemCell)
    {
        var itemView = itemCell.GetComponent<UIViewDoomCouncilMainItem>();
        var itemData = listShowData[itemCell.index];
        itemView.SetData(itemData);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }
    
    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }
}