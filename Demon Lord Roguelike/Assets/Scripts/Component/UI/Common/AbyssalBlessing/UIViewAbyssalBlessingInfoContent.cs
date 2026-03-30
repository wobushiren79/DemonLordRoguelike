

using System.Collections.Generic;
using UnityEngine;

public partial class UIViewAbyssalBlessingInfoContent : BaseUIView
{
    public List<UIViewAbyssalBlessingInfoContentItem> listItemAbyssalBlessing = new List<UIViewAbyssalBlessingInfoContentItem>();

    public override void Awake()
    {
        base.Awake();
        this.RegisterEvent<AbyssalBlessingEntityBean>(EventsInfo.Buff_AbyssalBlessingChange, EventForAbyssalBlessingChange);
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        RefreshUIData();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    /// <summary>
    /// 刷新UI
    /// </summary>
    public void RefreshUIData()
    {
        var allAbyssalBlessing = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
        //首先隐藏原来的view
        for (int i = 0; i < listItemAbyssalBlessing.Count; i++)
        {
            listItemAbyssalBlessing[i].gameObject.SetActive(false);
        }
        
        if (allAbyssalBlessing.ListKey.Count > 0)
        {
            ui_Content.gameObject.SetActive(true);
        }
        else
        {
            ui_Content.gameObject.SetActive(false);
        }

        for (int i = 0; i < allAbyssalBlessing.ListKey.Count; i++)
        {
            UIViewAbyssalBlessingInfoContentItem targetItemView = null;
            var itemData = allAbyssalBlessing.ListKey[i];
            if (i >= listItemAbyssalBlessing.Count)
            {
                GameObject newObj = Instantiate(ui_Content.gameObject, ui_UIViewAbyssalBlessingInfoContentItem.gameObject);
                targetItemView = newObj.GetComponent<UIViewAbyssalBlessingInfoContentItem>();
                listItemAbyssalBlessing.Add(targetItemView);
            }
            else
            {
                targetItemView = listItemAbyssalBlessing[i];
            }

            targetItemView.SetData(itemData);
            targetItemView.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 事件-深渊馈赠变化
    /// </summary>
    public void EventForAbyssalBlessingChange(AbyssalBlessingEntityBean abyssalBlessingEntityBean)
    {
        if (gameObject.activeSelf == false) return;
        if (gameObject.activeInHierarchy == false) return;
        RefreshUIData();
    }
}