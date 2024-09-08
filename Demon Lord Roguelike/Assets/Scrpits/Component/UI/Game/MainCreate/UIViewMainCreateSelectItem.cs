using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewMainCreateSelectItem : BaseUIView
{
    public List<string> listSelect = new List<string>();
    public int indexSelect = 0;
    public Action<UIViewMainCreateSelectItem, int> actionForSelect;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(List<string> listSelect, Action<UIViewMainCreateSelectItem, int> actionForSelect)
    {
        indexSelect = 0;
        this.listSelect = listSelect;
        this.actionForSelect = actionForSelect;
        ChangeSelect(indexSelect);
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
        ChangeSelect(indexSelect - 1);
    }

    public void OnClickForRight()
    {
        ChangeSelect(indexSelect + 1);
    }

    /// <summary>
    /// 改变选择
    /// </summary>
    public void ChangeSelect(int changeSelect)
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
        actionForSelect?.Invoke(this, indexSelect);
    }

}
