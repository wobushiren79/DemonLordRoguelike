using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameStateEnum
{
    None = 0,
    Pre,//准备中
    Gaming,//游戏中
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
}

//动画-生物
public enum AnimationCreatureStateEnum
{
    None,
    Idle,
    Walk,
    Walk2,
    Walk3,
    Attack,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    Attack6,
    Attack7,
    Dead,
    NearDead,
    Hit,//受到攻击
    Jump,//跳跃
    Run,//泡
    Dizzy,//晕眩
}


//游戏战斗预制状态
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//拾取检测中，
    Droping = 2,//拾取中
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