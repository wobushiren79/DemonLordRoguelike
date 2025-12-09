using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewMainCreateSelectItem : BaseUIView
{
    public List<string> listSelect = new List<string>();
    public int indexSelect = 0;
    public Action<UIViewMainCreateSelectItem, int, bool> actionForSelect;

    public CreatureSkinTypeEnum creatureSkinType;
    public int creatureId;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(List<string> listSelect, Action<UIViewMainCreateSelectItem, int, bool> actionForSelect,int startIndex = 0)
    {
        indexSelect = startIndex;
        this.listSelect = listSelect;
        this.actionForSelect = actionForSelect;
        ChangeSelect(indexSelect, true);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string titleStr)
    {
        ui_Title.text = $"{titleStr}";
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_LeftBtn)
        {
            OnClickForLeft();
        }
        else if (viewButton == ui_RightBtn)
        {
            OnClickForRight();
        }
    }

    public void OnClickForLeft()
    {
        ChangeSelect(indexSelect - 1, false);
    }

    public void OnClickForRight()
    {
        ChangeSelect(indexSelect + 1, false);
    }

    /// <summary>
    /// 改变选择
    /// </summary>
    public void ChangeSelect(int changeSelect, bool isInit)
    {
        if (changeSelect >= listSelect.Count)
        {
            changeSelect = 0;
        }
        if (changeSelect < 0)
        {
            changeSelect = listSelect.Count - 1;
        }
        indexSelect = changeSelect;
        SetTitle($"{listSelect[indexSelect]}");
        actionForSelect?.Invoke(this, indexSelect, isInit);
    }

}
