using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightAttCreateBean
{
    public Dictionary<int, FightAttCreateDetailsBean> dicDetailsData;
}

/// <summary>
/// 普通流程数据
/// </summary>
public class FightAttCreateDetailsBean
{
    public int stage;//阶段
    public float timeDuration;//持续时间

    public float createNum;//一次生成的数量
    public float createNumLerpData;//每次生成数量的线性变化

    public float createDelay;//一次生成间隔
    public float createDelayLerpData;//一次生成间隔

    public Dictionary<float, List<int>> creatureIds;//不同时间阶段 需要生成的生物（key为0-1百分比）
    public Dictionary<int, int> creatureEndIds;//最后一波需要生成的生物
}

