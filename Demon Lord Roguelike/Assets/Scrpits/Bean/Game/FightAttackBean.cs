using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

public class FightAttackBean
{
    //进攻列表
    public Queue<FightAttackDetailsBean> queueAttackDetails = new Queue<FightAttackDetailsBean>();
    //总共进攻时间
    public float timeAttackTotal = 0;
    //已经进行过的进攻时间
    public float timeAttackCurrent = 0;

    /// <summary>
    /// 添加列表
    /// </summary>
    public void AddAttackQueue(FightAttackDetailsBean fightAttackDetails)
    {
        queueAttackDetails.Enqueue(fightAttackDetails);
        timeAttackTotal += fightAttackDetails.timeAttack;
    }

    /// <summary>
    /// 获取下一次进攻数据
    /// </summary>
    public FightAttackDetailsBean GetNextAttackDetailsData()
    {
        if (queueAttackDetails.Count <= 0)
        {
            return null;
        }
        var fightAttackDetails = queueAttackDetails.Dequeue();
        timeAttackCurrent += fightAttackDetails.timeAttack;
        return fightAttackDetails;
    }

    /// <summary>
    /// 获取进攻进度
    /// </summary>
    /// <returns></returns>
    public float GetAttackProgress()
    {
        if (timeAttackTotal == 0)
            return 0;
        return timeAttackCurrent / timeAttackTotal;
    }
}

public class FightAttackDetailsBean
{
    //进攻时间 时间结束后执行下一个进攻数据
    public float timeAttack = 0;

    public List<int> creatureIds;

    public FightAttackDetailsBean(float timeAttack,int creatureId)
    {
        this.timeAttack = timeAttack;
        creatureIds = new List<int>();
        creatureIds.Add(creatureId);
    }
}