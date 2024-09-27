using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupProtalDetails : PopupShowCommonView
{

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="data"></param>
    public override void SetData(object data)
    {
        var targetData = ((GameWorldInfoBean, int, int))data;
        GameWorldInfoBean gameWorldInfo = targetData.Item1;
        int difficulty = targetData.Item2;
        int maxLevel = targetData.Item3;

        var difficultyInfo = GameWorldDifficultyInfoCfg.GetItemData(difficulty);

        SetItemContente(0, TextHandler.Instance.GetTextById(2000001), $"{gameWorldInfo.GetName()}");
        SetItemContente(1, TextHandler.Instance.GetTextById(2000002), $"{difficultyInfo.GetName()}");
        SetItemContente(2, TextHandler.Instance.GetTextById(2000003), $"{maxLevel}");
    }

    /// <summary>
    /// 设置单个数据
    /// </summary>
    public void SetItemContente(int index, string title, string content)
    {
        var itemView = transform.GetChild(index);
        TextMeshProUGUI textTitle = itemView.Find("Title").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI textContent = itemView.Find("Content").GetComponent<TextMeshProUGUI>();
        Text textContent2 = itemView.Find("Content").GetComponent<Text>();

        if (textTitle != null)
            textTitle.text = title;
        if(textContent != null)
            textContent.text = content;
        if (textContent2 != null)
            textContent2.text = content;
    }
}
