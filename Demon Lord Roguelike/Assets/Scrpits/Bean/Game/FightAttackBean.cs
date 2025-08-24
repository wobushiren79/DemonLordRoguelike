using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

public class FightAttackBean
{
    //进攻列表
    public Queue<FightAttackDetailsBean> queueAttackDetails = new Queue<FightAttackDetailsBean>();
    //当前进攻数据
    public FightAttackDetailsBean currentAttackDetail;
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
        timeAttackTotal += fightAttackDetails.timeNextAttack;
    }

    /// <summary>
    /// 获取下一次进攻数据
    /// </summary>
    public FightAttackDetailsBean GetNextAttackDetailData()
    {
        if (queueAttackDetails.Count <= 0)
        {
            return null;
        }
        currentAttackDetail = queueAttackDetails.Dequeue();
        timeAttackCurrent += currentAttackDetail.timeNextAttack;
        return currentAttackDetail;
    }

    /// <summary>
    /// 获取当前进攻数据
    /// </summary>
    /// <returns></returns>
    public FightAttackDetailsBean GetCurrentAttackDetailData()
    {
        return currentAttackDetail;
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
    public float timeNextAttack = 0;
    //进攻生物
    public List<long> npcIds;

    public FightAttackDetailsBean(float timeNextAttack, long creatureId)
    {
        this.timeNextAttack = timeNextAttack;
        npcIds = new List<long>() { creatureId };
    }

        public FightAttackDetailsBean(float timeNextAttack, List<long> npcIds)
    {
        this.timeNextAttack = timeNextAttack;
        this.npcIds = npcIds;
    }
}