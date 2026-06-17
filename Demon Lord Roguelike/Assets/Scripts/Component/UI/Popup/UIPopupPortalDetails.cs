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
        var targetData = ((GameWorldInfoBean, GameWorldInfoRandomBean, int))data;
        GameWorldInfoBean gameWorldInfo = targetData.Item1;
        GameWorldInfoRandomBean gameWorldInfoRandom = targetData.Item2;
        //气泡要展示的难度(由调用方传入: 难度详情每个item展示各自难度, 地图传送门item展示当前难度)
        int difficultyLevel = targetData.Item3;

        //征服模式: 按指定难度取该难度预生成的道路/关卡数(各难度在创建时已全部随出);
        //无尽模式无难度概念, 直接用当前字段值
        int roadNum = gameWorldInfoRandom.roadNum;
        int fightNum = gameWorldInfoRandom.fightNum;
        if (gameWorldInfoRandom.gameFightType == GameFightTypeEnum.Conquer)
        {
            GameWorldDifficultyRandomBean difficultyRandom = gameWorldInfoRandom.GetDifficultyRandom(difficultyLevel);
            if (difficultyRandom != null)
            {
                roadNum = difficultyRandom.roadNum;
                fightNum = difficultyRandom.fightNum;
            }
        }

        //设置名字
        SetItemContente(0, TextHandler.Instance.GetTextById(411), $"{gameWorldInfo.name_language}");
        //设置线路数量
        SetItemContente(1, TextHandler.Instance.GetTextById(412), $"{roadNum}");
        //设置战斗关卡(无尽模式不展示关卡数)
        bool isShowFightNum = gameWorldInfoRandom.gameFightType != GameFightTypeEnum.Infinite;
        SetItemContente(2, TextHandler.Instance.GetTextById(413), $"{fightNum}", isShow: isShowFightNum);
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
