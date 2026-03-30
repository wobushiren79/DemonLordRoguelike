using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventsInfo
{

    #region 通用
    public const string User_AddUnlock = "User_AddUnlock";//解锁
    public const string Backpack_Crystal_Change = "Backpack_Crystal_Change";//魔晶变化
    public const string Backpack_Reputation_Change = "Backpack_Reputation_Change";//声望变化
    public const string Backpack_Item_Change = "Backpack_Item_Change";//道具变化

    public const string World_EnterGameForBaseScene = "World_EnterGameForBaseScene";//场景改变-基地
    public const string Creature_Rename = "Creature_Rename";//改名
    #endregion

    #region UI
    public const string UIViewCreatureCardItem_SelectKeep = "UIViewCreatureCardItem_SelectKeep";//卡片避让

    public const string UIViewCreatureCardItem_OnPointerEnter = "UIViewCreatureCardItem_OnPointerEnter";//卡片进入
    public const string UIViewCreatureCardItem_OnPointerExit = "UIViewCreatureCardItem_OnPointerExit";//卡片离开
    public const string UIViewCreatureCardItem_OnClickSelect = "UIViewCreatureCardItem_OnClickSelect";//点击选择

    public const string UIViewItemBackpack_OnClickSelect = "UIViewItemBackpack_OnClickSelect";//背包道具点击
    public const string UIViewItemEquip_OnClickSelect = "UIViewItemEquip_OnClickSelect";//装备道具点击
    #endregion


    #region 战斗中
    public const string GameFightLogic_SelectCard = "GameFightLogic_SelectCard";//卡片选择
    public const string GameFightLogic_UnSelectCard = "GameFightLogic_UnSelectCard";//取消卡片选择
    public const string GameFightLogic_PutCard = "GameFightLogic_PutCard";//放置卡片选择    
    public const string GameFightLogic_CreatureChangeState = "GameFightLogic_CreatureChangeState";//生物状态修改

    public const string GameFightLogic_CreatureDeadStart = "GameFightLogic_CreatureDeadStart";//生物开始死亡  
    public const string GameFightLogic_CreatureDeadEnd = "GameFightLogic_CreatureDeadEnd";//生物结束死亡（注意 参数对象会在下一帧回收）
    public const string GameFightLogic_CreatureDeadDropCrystal = "GameFightLogic_CreatureDeadDropCrystal";//生物死亡掉落水晶

    public const string GameFightLogic_EndGame = "GameFightLogic_EndGame";//结束游戏
    public const string GameFightLogic_AddExp = "GameFightLogic_AddExp";//增加经验
    public const string GameFightLogic_DropAddCrystal= "GameFightLogic_DropAddCrystal";//掉落拾取水晶
        
    public const  string GameFightLogic_UnderAttack = "GameFightLogic_UnderAttack";//生物收到攻击
    public const string GameFightLogic_UnderAttack_Dead = "GameFightLogic_UnderAttack_Dead";//生物收到攻击死亡 
    #endregion

    #region BUFF
    public const string Buff_AbyssalBlessingChange = "Buff_AbyssalBlessingChange";//BUFF系统-深渊馈赠变化
    public const string Buff_FightCreatureChange = "Buff_FightCreatureChange";//BUFF系统-生物BUFF改变
    #endregion

    #region 扭蛋机
    public const string GashaponMachine_ClickBreak = "GashaponMachine_ClickBreak";//点击破碎 
    public const string GashaponMachine_ClickNext = "GashaponMachine_ClickNext";//点击下一个
    public const string GashaponMachine_ClickReset = "GashaponMachine_ClickReset";//点击重置
    public const string GashaponMachine_ClickEnd = "GashaponMachine_ClickEnd";//点击结束
    public const string GashaponMachine_ClickShowAll = "GashaponMachine_ClickShowAll";//点击跳过所有
    #endregion

    #region 生物献祭
    public const string CreatureSacrifice_SelectCreature = "CreatureSacrifice_SelectCreature";//生物选择完成
    public const string CreatureSacrifice_SacrificeSuccess = "CreatureSacrifice_SacrificeSuccess";//献祭成功
    public const string CreatureSacrifice_SacrificeFail = "CreatureSacrifice_SacrificeFail";//献祭失败
    #endregion

    #region 生物进阶
    public const string CreatureAscend_AddProgress = "CreatureAscend_AddProgress";//增加进度
    #endregion
}
