using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventsInfo
{
    public static string Coin_Change = "Coin_Change";
    public static string Magic_Change = "Magic_Change";

    public static string UIViewCreatureCardItem_SelectKeep = "UIViewCreatureCardItem_SelectKeep";//卡片避让
    public static string UIViewCreatureCardItem_ShowDetails = "UIViewCreatureCardItem_ShowDetails";//展示卡片详情
    public static string UIViewCreatureCardItem_HideDetails = "UIViewCreatureCardItem_HideDetails";//隐藏卡片详情

    public static string UIViewCreatureCardItem_OnPointerEnter = "UIViewCreatureCardItem_OnPointerEnter";//卡片进入
    public static string UIViewCreatureCardItem_OnPointerExit = "UIViewCreatureCardItem_OnPointerExit";//卡片离开

    public static string UIViewCreatureCardItem_OnClickSelect = "UIViewCreatureCardItem_OnClickSelect";//点击选择

    public static string GameFightLogic_SelectCard = "GameFightLogic_SelectCard";//卡片选择
    public static string GameFightLogic_UnSelectCard = "GameFightLogic_UnSelectCard";//取消卡片选择
    public static string GameFightLogic_PutCard = "GameFightLogic_PutCard";//放置卡片选择    
    public static string GameFightLogic_RefreshCard = "GameFightLogic_RefreshCard";//刷新卡片  

    public static string GameFightLogic_CreatureDeadStart = "GameFightLogic_CreatureDeadStart";//生物开始死亡  
    public static string GameFightLogic_CreatureDeadEnd = "GameFightLogic_CreatureDeadEnd";//生物结束死亡

    public static string Toast_NoEnoughCreateMagic = "Toast_NoEnoughCreateMagic";//没有足够的创建魔力

    #region 扭蛋机
    public static string GashaponMachine_ClickBreak = "GashaponMachine_ClickBreak";//点击破碎 
    public static string GashaponMachine_ClickNext = "GashaponMachine_ClickNext";//点击下一个
    public static string GashaponMachine_ClickReset = "GashaponMachine_ClickReset";//点击重置
    public static string GashaponMachine_ClickEnd = "GashaponMachine_ClickEnd";//点击结束
    #endregion
}
