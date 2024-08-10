using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventsInfo
{
    public static string UIViewCreatureCardItem_SelectKeep = "UIViewCreatureCardItem_SelectKeep";//卡片避让
    public static string UIViewCreatureCardItem_ShowDetails = "UIViewCreatureCardItem_ShowDetails";//展示卡片详情
    public static string UIViewCreatureCardItem_HideDetails = "UIViewCreatureCardItem_HideDetails";//隐藏卡片详情

    public static string GameFightLogic_SelectCard = "GameFightLogic_SelectCard";//卡片选择
    public static string GameFightLogic_UnSelectCard = "GameFightLogic_UnSelectCard";//取消卡片选择
    public static string GameFightLogic_PutCard = "GameFightLogic_PutCard";//放置卡片选择    
    public static string GameFightLogic_RefreshCard = "GameFightLogic_RefreshCard";//刷新卡片  

    public static string GameFightLogic_CreatureDead = "GameFightLogic_CreatureDead";//生物死亡  

    public static string Toast_NoEnoughCreateMagic = "Toast_NoEnoughCreateMagic";//没有足够的创建魔力

    #region 扭蛋机
    public static string GashaponMachine_ClickBreak = "GashaponMachine_ClickBreak";//点击破碎 
    #endregion
}
