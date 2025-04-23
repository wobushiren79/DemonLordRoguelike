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

public enum Rarity
{
    N = 1,
    R = 2,
    SR = 3,
    SSR = 4,
    UR = 5,
    L = 6
}

//卡片用途
public enum CardUseState
{
    Show,//展示
    Fight,//战斗
    Lineup,//阵容
    LineupBackpack,//阵容背包
    CreatureManager,//魔物管理
    CreatureSacrifice,//魔物献祭
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
}

//游戏战斗预制状态
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//拾取检测中，
    Droping = 2,//拾取中
}

//游戏场景
public enum GameSceneTypeEnum
{
    None = 0,
    Base = 1,//基地
    Fight = 2,//战斗
}

public enum TestSceneTypeEnum
{
    None = 0,
    FightSceneTest = 1,//战斗场景测试
    CardTest = 2,//卡片效果测试
    Base = 3,//基地测试
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
    Test,//测试模式
    Infinite,//无限模式
    Conquer,//征服模式
}


public enum ResearchInfoTypeEnum
{   
    Building = 1,//设施相关
    Strengthen = 2,//强化相关
    Creature = 3,//生物相关
}
