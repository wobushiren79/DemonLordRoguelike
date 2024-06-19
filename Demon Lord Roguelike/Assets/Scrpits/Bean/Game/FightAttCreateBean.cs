using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightAttCreateBean
{
    public Dictionary<int, FightAttCreateDetailsBean> dicDetailsData;

    public FightAttCreateDetailsBean GetDetailData(int stage)
    {
        if (dicDetailsData.TryGetValue(stage, out FightAttCreateDetailsBean fightAttCreateDetails))
        {
            return fightAttCreateDetails;
        }
        return null;
    }
}

/// <summary>
/// 普通流程数据
/// </summary>
public class FightAttCreateDetailsBean
{
    public int stage;//阶段
    public float timeDuration;//持续时间

    public int createNum;//一次生成的数量

    public float createDelay;//一次生成间隔
    public float createDelayLerpData;//一次生成间隔

    public List<FightAttCreateDetailsTimePointBean> timePointForCreatures;//不同时间阶段 需要生成的生物（key为0-1百分比）
    public Dictionary<int, int> creatureEndIds;//最后一波需要生成的生物
}

public class FightAttCreateDetailsTimePointBean
{
    public float startTimeProgress;
    public float endTimeProgress;
    public List<int> creatureIds;

    public FightAttCreateDetailsTimePointBean(float startTimeProgress,float endTimeProgress,List<int> creatureIds)
    {
        this.startTimeProgress = startTimeProgress;
        this.endTimeProgress = endTimeProgress;
        this.creatureIds = creatureIds;
    }
}

