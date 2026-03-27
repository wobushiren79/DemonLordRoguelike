

using System;
using UnityEngine.UI;

public partial class UIFightAbyssalBlessing : BaseUIComponent
{
    //选择回调
    public Action<AbyssalBlessingInfoBean> actionForSelect;
    //跳过回调
    public Action actionForSkip;
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(Action<AbyssalBlessingInfoBean> actionForSelect = null, Action actionForSkip = null)
    {
        this.actionForSelect = actionForSelect;
        this.actionForSkip = actionForSkip;
        
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
                // 解析有等级的BUFF，展示玩家当前等级的高一级（没有则展示1级）
                BuffInfoBean resolvedBuffInfo = null;
                if (!itemData.buff_ids.IsNull())
                {
                    long[] buffIds = itemData.buff_ids.SplitForArrayLong(',');
                    for (int bi = 0; bi < buffIds.Length; bi++)
                    {
                        BuffInfoBean buffInfo = BuffInfoCfg.GetItemData(buffIds[bi]);
                        if (buffInfo != null && buffInfo.buff_level > 0)
                        {
                            long parentId = buffInfo.buff_parent_id;
                            int currentLevel = BuffHandler.Instance.GetAbyssalBlessingCurrentLevel(parentId);
                            resolvedBuffInfo = BuffInfoCfg.GetBuffByParentAndLevel(parentId, currentLevel + 1);
                            if (resolvedBuffInfo == null)
                                resolvedBuffInfo = BuffInfoCfg.GetBuffByParentAndLevel(parentId, 1);
                            break;
                        }
                    }
                }
                targetView.SetData(itemData, resolvedBuffInfo);
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
        actionForSkip?.Invoke();
        actionForSkip = null;
    }
}