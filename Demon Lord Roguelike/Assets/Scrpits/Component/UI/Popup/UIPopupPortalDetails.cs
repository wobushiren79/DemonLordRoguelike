using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupPortalDetails : PopupShowCommonView
{

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="data"></param>
    public override void SetData(object data)
    {
        var targetData = ((GameWorldInfoBean, GameWorldInfoRandomBean))data;
        GameWorldInfoBean gameWorldInfo = targetData.Item1;
        GameWorldInfoRandomBean gameWorldInfoRandom = targetData.Item2;

        //设置名字
        SetItemContente(0, TextHandler.Instance.GetTextById(411), $"{TextHandler.Instance.GetTextById(gameWorldInfo.name)}");
        //设置线路数量
        SetItemContente(1, TextHandler.Instance.GetTextById(412), $"{gameWorldInfoRandom.roadNum}");
        //设置战斗关卡
        if (gameWorldInfoRandom.gameFightType == GameFightTypeEnum.Infinite)
        {
            SetItemContente(2, TextHandler.Instance.GetTextById(413), $"{gameWorldInfoRandom.fightNum}",isShow:false);
        }
        else
        {
            SetItemContente(2, TextHandler.Instance.GetTextById(413), $"{gameWorldInfoRandom.fightNum}");
        }
    }

    /// <summary>
    /// 设置单个数据
    /// </summary>
    public void SetItemContente(int index, string title, string content,bool isShow=true)
    {
        var itemView = transform.GetChild(index);
        itemView.gameObject.SetActive(isShow);

        Transform tfContent = itemView.Find("Content");

        TextMeshProUGUI textTitle = itemView.Find("Title").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI textContent = tfContent.GetComponent<TextMeshProUGUI>();
        Text textContent2 = tfContent.GetComponent<Text>();

        if (textTitle != null)
            textTitle.text = title;
        if(textContent != null)
            textContent.text = content;
        if (textContent2 != null)
            textContent2.text = content;
    }
}
