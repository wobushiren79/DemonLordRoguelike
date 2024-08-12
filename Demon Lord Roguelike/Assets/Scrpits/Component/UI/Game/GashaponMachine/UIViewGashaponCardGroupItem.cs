using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIViewGashaponCardGroupItem : BaseUIView
{
    public CardGroupInfoBean cardGroupInfo;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CardGroupInfoBean cardGroupInfo)
    {
        this.cardGroupInfo = cardGroupInfo;
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelectState(bool isSelect)
    {
        if (isSelect)
        {
            ui_ContentShow.localScale = Vector3.one * 1.2f;
        }
        else
        {
            ui_ContentShow.localScale = Vector3.one;
        }
    }
}
