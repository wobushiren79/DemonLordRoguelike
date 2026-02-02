using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;


public partial class UIViewBuffShowItem : BaseUIView
{
    public BuffBean buffData;
    public BuffInfoBean buffInfo;
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(BuffBean buffData)
    {
        this.buffData = buffData;
        this.buffInfo = BuffInfoCfg.GetItemData(buffData.id);

        var rarityEnum = buffInfo.GetRarity();
        var rarityInfo = RarityInfoCfg.GetItemData(rarityEnum);
        ColorUtility.TryParseHtmlString(rarityInfo.buff_color, out Color buffColor);

        //设置名字
        ui_BuffName.text = $"{buffInfo.name_language}";
        ui_BuffName.color = buffColor;
        //设置背景颜色
        ui_UIViewBuffShowItem_Image.color = buffColor;
        //设置提示弹窗
        Dictionary<TextReplaceEnum, string> dicReplace = new Dictionary<TextReplaceEnum, string>()
        {
            {TextReplaceEnum.Percentage, $"{MathUtil.GetPercentage(buffData.trigger_value_rate, 2)}"},
            {TextReplaceEnum.Time_S, $"{Mathf.FloorToInt(buffData.trigger_time)}"},
        };
        string contentText = TextHandler.Instance.GetTextReplace(buffInfo.content_language, dicReplace);
        ui_UIViewBuffShowItem_PopupButtonCommonView.SetData(contentText, PopupEnum.Text);
    }


}