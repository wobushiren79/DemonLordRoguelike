using System;
using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public partial class UIHandler
{

    /// <summary>
    /// 通过图集图标名设置自定义光标（游戏层枚举重载）。
    /// </summary>
    public void SetCursorByIconName(SpriteAtlasTypeEnum atlasType, string iconName, Vector2? hotspotOverride = null, int pixelScale = 3)
    {
        SetCursorByIconName(atlasType.ToAtlasTag(), iconName, hotspotOverride, pixelScale);
    }

    /// <summary>
    /// toast提示
    /// </summary>
    /// <param name="hintContent"></param>
    /// <param name="state">0失败 1成功</param>
    public void ToastHintText(string hintContent, int state = 0)
    {
        string iconRes = IconHandler.IconNameUnKnow;
        Color iconColor = Color.white;
        switch (state)
        {
            case 0:
                iconRes = "ui_other_3";
                ColorUtility.TryParseHtmlString("#E32626",out iconColor);
                break;
            case 1:
                iconRes = "ui_other_6";
                ColorUtility.TryParseHtmlString("#25BC29",out iconColor);
                break;
        }
        IconHandler.Instance.GetIconSprite(SpriteAtlasTypeEnum.UI, iconRes, (sprite) =>
        {
            ToastHint<ToastView>(sprite, iconColor, hintContent);
        });
    }

    /// <summary>
    /// 清理所有主界面UI-用于进游戏
    /// </summary>
    public void DestoryAllMainUI()
    {
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            var itemUI =  manager.uiList[i];
            if (itemUI.name.Contains("UIMain"))
            {
                i--;
                itemUI.SetUICloseType(UICloseTypeEnum.Destory);
                itemUI.CloseUI();
            }
        }
    }

    /// <summary>
    /// 展示遮罩UI
    /// </summary>
    public void ShowMask(float maskTime, Action acionForStart, Action acionForComplete, bool isCloseOther)
    {
        UICommonMask maskUI;
        if (isCloseOther)
        {
            maskUI = OpenUIAndCloseOther<UICommonMask>(layer: 99);
        }
        else
        {
            maskUI = OpenUI<UICommonMask>(layer: 99);
        }
        maskUI.StartMask(maskTime, acionForStart, acionForComplete);
    }

    public void HideMask(float maskTime, Action acionForStart, Action acionForComplete, bool isCloseSelf = true)
    {     
        var maskUI = OpenUIAndCloseOther<UICommonMask>();
        if (maskTime <= 0)
        {
            maskUI.CloseUI();
        }
        else
        {
            maskUI.EndMask(maskTime, acionForStart, acionForComplete, isCloseSelf);
        }
    }

    public UIDialogBossShow ShowDialogBossShow(DialogBossShowBean dialogData)
    {
        dialogData.dialogType = DialogEnum.BossShow;
        return ShowDialog<UIDialogBossShow>(dialogData);
    }

    /// <summary>
    /// 生物展示
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogCreatureShow ShowDialogCreatureShow(DialogCreatureShowBean dialogData)
    {
        dialogData.dialogType = DialogEnum.CreatureShow;
        dialogData.isDestroyBG = true;
        return ShowDialog<UIDialogCreatureShow>(dialogData);
    }

    /// <summary>
    /// 生物选择
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogSelectCreature ShowDialogSelectCreature(DialogSelectCreatureBean dialogData)
    {
        dialogData.dialogType = DialogEnum.SelectCreature;
        if (dialogData.content.IsNull())
        {
            dialogData.content = TextHandler.Instance.GetTextById(1005001);
        }
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogSelectCreature>(dialogData);
    }

    /// <summary>
    /// 文本修改
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogRename ShowDialogRename(DialogRenameBean dialogData)
    {
        dialogData.dialogType = DialogEnum.Rename;
        if (dialogData.characterLimit <= 0)
        {
            dialogData.characterLimit = 10;
        }
        dialogData.content = TextHandler.Instance.GetTextById(1004001);   
        dialogData.inputHint = TextHandler.Instance.GetTextById(1004002);
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogRename>(dialogData);
    }

    /// <summary>
    /// 展示颜色选择
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogSelectColor ShowDialogSelectColor(DialogSelectColorBean dialogData)
    {
        dialogData.dialogType = DialogEnum.SelectColor;
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        return ShowDialog<UIDialogSelectColor>(dialogData);
    }

    /// <summary>
    /// 展示道具选择弹窗
    /// </summary>
    public UIDialogSelectItem ShowDialogItemSelect(DialogSelectItemBean dialogData)
    {
        dialogData.dialogType = DialogEnum.SelectItem;
        var dialogView = ShowDialog<UIDialogSelectItem>(dialogData);
        dialogView.InitBackpackItemsData();
        return dialogView;
    }

    /// <summary>
    /// 展示默认普通
    /// </summary>
    public UIDialogNormal ShowDialogNormal(DialogBean dialogData)
    {
        dialogData.dialogType = DialogEnum.Normal;
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogNormal>(dialogData);
    }
    
    /// <summary>
    /// 展示选项提示
    /// </summary>
    public UIDialogSelect ShowDialogSelect(DialogSelectBean dialogData)
    {
        dialogData.dialogType = DialogEnum.Select;
        return ShowDialog<UIDialogSelect>(dialogData);
    }

    /// <summary>
    /// 展示传送门
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogPortalDetails ShowDialogPortalDetails(DialogBean dialogData)
    {
        dialogData.dialogType = DialogEnum.PortalDetails;
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogPortalDetails>(dialogData);
    }

    /// <summary>
    /// 展示排序筛选弹窗(点击背景关闭,确认后通过 actionForConfirm 回传 OrderFilterResultBean:排序键+名字+等级区间+稀有度)
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogOrderFilter ShowDialogOrderFilter(DialogOrderFilterBean dialogData)
    {
        dialogData.dialogType = DialogEnum.OrderFilter;
        //点击背景关闭
        dialogData.isDestroyBG = true;
        return ShowDialog<UIDialogOrderFilter>(dialogData);
    }

    /// <summary>
    /// 展示排序筛选弹窗(便捷重载:直接传参,无需手动构造 Bean)。
    /// 约定:触发该弹窗的筛选/排序按钮,其 PopupButtonCommonView 悬浮详情统一用多语言 2000014(筛选排序)。
    /// 各区段显隐由 listFilterType 推导:含 Name→名字区、Level→等级区、Rarity→稀有度区、含 ItemType→道具类型区、
    /// 含战斗维度(Damage/Kill/DamageReceived/Exp)→战斗区、含 Lineup/Class→其它区;为空则全部显示。
    /// </summary>
    /// <param name="targetButton">触发弹窗的按钮(用于把弹窗内容定位到该按钮处)</param>
    /// <param name="actionForConfirm">确认回调:回传 OrderFilterResultBean(排序键按优先级 index0最高 + 名字模糊 + 等级区间 + 稀有度 + 道具类型)</param>
    /// <param name="listFilterType">开放哪些维度(决定各区段显隐);为空(null)则全部显示</param>
    /// <param name="selectFilterTypes">默认已选中的排序键(按优先级,index0最高);为空则默认无选中</param>
    /// <param name="defaultNameFilter">默认名字筛选(回填名字输入框)</param>
    /// <param name="defaultLevelMin">默认等级下限(0=不限,回填后输入框留空)</param>
    /// <param name="defaultLevelMax">默认等级上限(int.MaxValue=不限,回填后输入框留空)</param>
    /// <param name="defaultRarities">默认选中的稀有度(回填稀有度多选)</param>
    /// <param name="itemTypes">道具类型区可选项(按上下文动态提供,如当前魔物可装备类型;按顺序填充预留项,多余隐藏)。需配合 listFilterType 含 ItemType 才显示</param>
    /// <param name="defaultItemTypes">默认选中的道具类型(回填道具类型多选)</param>
    /// <returns></returns>
    public UIDialogOrderFilter ShowDialogOrderFilter(
        RectTransform targetButton,
        Action<OrderFilterResultBean> actionForConfirm,
        List<OrderFilterTypeEnum> listFilterType = null,
        List<OrderFilterTypeEnum> selectFilterTypes = null,
        string defaultNameFilter = null,
        int defaultLevelMin = 0,
        int defaultLevelMax = int.MaxValue,
        List<RarityEnum> defaultRarities = null,
        List<ItemTypeEnum> itemTypes = null,
        List<ItemTypeEnum> defaultItemTypes = null)
    {
        DialogOrderFilterBean dialogData = new DialogOrderFilterBean
        {
            targetButton = targetButton,
            actionForConfirm = actionForConfirm,
            listFilterType = listFilterType,
            selectFilterTypes = selectFilterTypes,
            defaultNameFilter = defaultNameFilter,
            defaultLevelMin = defaultLevelMin,
            defaultLevelMax = defaultLevelMax,
            defaultRarities = defaultRarities,
            itemTypes = itemTypes,
            defaultItemTypes = defaultItemTypes,
        };
        return ShowDialogOrderFilter(dialogData);
    }
}
