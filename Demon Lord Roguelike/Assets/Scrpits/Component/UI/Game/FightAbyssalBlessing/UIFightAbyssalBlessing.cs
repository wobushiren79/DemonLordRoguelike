

using System;
using UnityEngine.UI;

public partial class UIFightAbyssalBlessing : BaseUIComponent
{
    //选择回调
    public Action<AbyssalBlessingInfoBean> actionForSelect;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData()
    {
        var allData = AbyssalBlessingInfoCfg.GetAllData();
        int showNum = 3;
        for (int i = 0; i < ui_AbyssalBlessingList.childCount; i++)
        {
            var itemView = ui_AbyssalBlessingList.GetChild(i);
            if (i < showNum)
            {
                itemView.gameObject.SetActive(true);
                UIViewFightAbyssalBlessingItem targetView = itemView.GetComponent<UIViewFightAbyssalBlessingItem>();

                var itemData = allData.GetRandomData();
                targetView.SetData(itemData);
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_SkipBtn)
        {
            OnClickForSkip();
        }
    }

    /// <summary>
    /// 点击选择
    /// </summary>
    public void OnClickForSelect(AbyssalBlessingInfoBean abyssalBlessingInfo)
    {
        actionForSelect?.Invoke(abyssalBlessingInfo);
        actionForSelect = null;
    }
    
        /// <summary>
    /// 点击选择
    /// </summary>
    public void OnClickForSkip()
    {
        actionForSelect?.Invoke(null);
        actionForSelect = null;
    }
}