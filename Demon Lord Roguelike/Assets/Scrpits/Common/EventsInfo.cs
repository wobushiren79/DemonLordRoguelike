using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventsInfo
{
    public static string Backpack_Coin_Change = "Backpack_Coin_Change";
    public static string Backpack_Item_Change = "Backpack_Item_Change";
    public static string Magic_Change = "Magic_Change";

    public static string UIViewCreatureCardItem_SelectKeep = "UIViewCreatureCardItem_SelectKeep";//卡片避让

    public static string UIViewCreatureCardItem_OnPointerEnter = "UIViewCreatureCardItem_OnPointerEnter";//卡片进入
    public static string UIViewCreatureCardItem_OnPointerExit = "UIViewCreatureCardItem_OnPointerExit";//卡片离开
    public static string UIViewCreatureCardItem_OnClickSelect = "UIViewCreatureCardItem_OnClickSelect";//点击选择

    public static string UIViewItemBackpack_OnClickSelect = "UIViewItemBackpack_OnClickSelect";//背包道具点击
    public static string UIViewItemEquip_OnClickSelect = "UIViewItemEquip_OnClickSelect";//装备道具点击

    public static string GameFightLogic_SelectCard = "GameFightLogic_SelectCard";//卡片选择
    public static string GameFightLogic_UnSelectCard = "GameFightLogic_UnSelectCard";//取消卡片选择
    public static string GameFightLogic_PutCard = "GameFightLogic_PutCard";//放置卡片选择    
    public static string GameFightLogic_CreatureChangeState = "GameFightLogic_CreatureChangeState";//生物状态修改

    public static string GameFightLogic_CreatureDeadStart = "GameFightLogic_CreatureDeadStart";//生物开始死亡  
    public static string GameFightLogic_CreatureDeadEnd = "GameFightLogic_CreatureDeadEnd";//生物结束死亡

    public static string Toast_NoEnoughCreateMagic = "Toast_NoEnoughCreateMagic";//没有足够的创建魔力

    #region 扭蛋机
    public static string GashaponMachine_ClickBreak = "GashaponMachine_ClickBreak";//点击破碎 
    public static string GashaponMachine_ClickNext = "GashaponMachine_ClickNext";//点击下一个
    public static string GashaponMachine_ClickReset = "GashaponMachine_ClickReset";//点击重置
    public static string GashaponMachine_ClickEnd = "GashaponMachine_ClickEnd";//点击结束
    #endregion

    #region 生物献祭
    public static string CreatureSacrifice_SelectCreature = "CreatureSacrifice_SelectCreature";//生物选择完成
    public static string CreatureSacrifice_SacrificeSuccess = "CreatureSacrifice_SacrificeSuccess";//献祭成功
    public static string CreatureSacrifice_SacrificeFail = "CreatureSacrifice_SacrificeFail";//献祭失败
    #endregion

    #region 生物进阶
    public static string CreatureAscend_UpdateVat = "CreatureAscend_UpdateVat";//更新Vat
    #endregion
}
