using TMPro;
using UnityEngine.UI;
using UnityEngine;


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
        ui_UIViewBuffShowItem_PopupButtonCommonView.SetData(buffInfo.content_language, PopupEnum.Text);
    }


}