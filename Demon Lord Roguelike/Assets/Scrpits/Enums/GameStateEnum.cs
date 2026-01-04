using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameStateEnum
{
    None = 0,
    Pre,//准备中
    Gaming,//游戏中
    Settlement,//结算中
    End,//游戏结束
}

public enum RarityEnum
{
    N = 1,
    R = 2,
    SR = 3,
    SSR = 4,
    UR = 5,
    L = 6
}

//卡片用途
public enum CardUseStateEnum
{
    Show,//展示
    ShowNoPopup,//展示但不弹详情
    Fight,//战斗
    Lineup,//阵容
    LineupBackpack,//阵容背包
    CreatureManager,//魔物管理
    CreatureSacrifice,//魔物献祭

    CreatureAscendTarget,//魔物进阶
    CreatureAscendMaterial,//魔物进阶

    SelectCreature,//生物选择
}

//卡片状态
public enum CardStateEnum
{
    None = 0,//空
    FightIdle = 101,//待机
    FightSelect = 102,//选择
    Fighting = 103,//上场战斗
    FightRest = 104,//休息

    LineupNoSelect = 201,//阵容未选中
    LineupSelect = 202,//阵容选中

    CreatureManagerNoSelect = 301,//生物管理未选中
    CreatureManagerSelect = 302,//生物管理选中

    CreatureSacrificeNoSelect = 401,//生物献祭未选中
    CreatureSacrificeSelect = 402,//生物献祭选中

    CreatureAscendNoSelect = 501,//生物进阶未选中
    CreatureAscendSelect = 502,//生物进阶选中

    SelectCreatureNoSelect = 601,//生物未选中
    SelectCreatureSelect = 602,//生物选中
}

//游戏战斗预制状态
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//拾取检测中，
    Droping = 2,//拾取中
}

//游戏场景(预制)
public enum GameSceneTypeEnum
{
    None = 0,
    BaseMain = 1,//基地主界面
    BaseGaming = 2,//基地游玩中(场景预制以这个为准)
    Fight = 3,//战斗
    RewardSelect = 4,//奖励选择
    DoomCouncil = 5,//终焉议会
}

public enum TestSceneTypeEnum
{
    None = 0,
    FightSceneTest = 1,//战斗场景测试
    CardTest = 2,//卡片效果测试
    Base = 3,//基地测试
    RewardSelect = 4,//奖励选择
    DoomCouncil = 5,//终焉议会
    NpcCreate = 6,//NPC创建
    ResearchUI = 7,//研究ui
}

public enum CinemachineCameraEnum
{
    None,
    Base,
    Fight,
}

//战斗类型
public enum GameFightTypeEnum
{
    None,
    Test,//测试模式
    Infinite,//无限模式
    Conquer,//征服模式
    DoomCouncil,//终焉议会
}

/// <summary>
/// 研究类型
/// </summary>
public enum ResearchInfoTypeEnum
{
    Building = 1,//设施相关
    Strengthen = 2,//强化相关
    Creature = 3,//生物相关
}


/// <summary>
/// 终焉议会议案触发时机
/// </summary>
public enum TriggerTypeDoomCouncilEntityEnum
{
    WorldEnterGameForBaseScene,//进入游戏中 基地场景
    GameFightLogicEndGame,//战斗结束
    GameFightLogicAddExp,//战斗增加经验
    GameFightLogicDropAddCrystal,//战斗掉落增加水晶
}

/// <summary>
/// 重点解锁枚举-用于判断关键模块解锁
/// </summary>
public enum UnlockEnum : long
{
    CreatureVatAdd1 = 100000000,//生物进阶
    CreatureVatAdd2 = 100000001,//生物进阶设置+1;
    CreatureVatAdd3 = 100000002,//生物进阶设置+1;
    CreatureVatAdd4 = 100000003,//生物进阶设置+1;
    CreatureVatAdd5 = 100000004,//生物进阶设置+1;
    CreatureVatAdd6 = 100000005,//生物进阶设置+1;
    Altar = 100100001,//祭坛
    DoomCouncil = 100200001,//终焉议会模块
    PortalShowNum1 = 100300001, //传送门显示数量
    PortalShowNum10 = 1003000010,
    GashaponShowAll = 100400001,//显示所有抽取
    LineupCreature1 = 200000001,//解锁阵容生物上限1
    LineupCreature10 = 200000010,//解锁阵容生物上限10
    Lineup2 = 200100001,//解锁阵容2
    Lineup5 = 200100004,//解锁阵容5
}

/// <summary>
/// 控制物体的交互枚举
/// </summary>
public enum ControlInteractionEnum
{
    None = 0,
    CoreInteraction,//核心交互
    PortalInteraction,//传送门交互
    DoomCouncilInteraction,//终焉议会交互
    DoomCouncilPodium,//终焉议会讲台
    Councilor,//议会成员
}

