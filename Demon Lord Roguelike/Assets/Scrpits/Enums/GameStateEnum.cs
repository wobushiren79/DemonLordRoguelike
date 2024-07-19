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

//卡片状态
public enum CardStateEnum
{
    None = 0,//空
    FightIdle = 101,//待机
    FightSelect = 102,//选择
    Fighting = 103,//上场战斗
    FightRest = 104,//休息
}

//动画-生物
public enum AnimationCreatureStateEnum
{
    Idle,
    Walk,
    Attack,
    Dead,
}


//游戏战斗预制状态
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//拾取检测中，
    Droping = 2,//拾取中
}