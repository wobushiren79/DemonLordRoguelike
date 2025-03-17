using NUnit.Framework;
using Spine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class FightBuffBean
{
    public FightBuffInfoBean fightBuffInfo;
    public FightBuffStruct fightBuffStruct;

    public string creatureId;//作用的生物ID
    public float timeUpdate = 0;
    public int triggerNumLeft;//剩下的触发次数

    public FightBuffBean(FightBuffStruct fightBuffStruct,string creatureId)
    {
        fightBuffInfo = FightBuffInfoCfg.GetItemData(fightBuffStruct.id);
        if (fightBuffInfo == null)
        {
            LogUtil.LogError($"buff初始化失败 没有找到creatureId_{creatureId} buffId_{fightBuffStruct.id}");
        }
        this.fightBuffStruct = fightBuffStruct;
        this.creatureId = creatureId;
        this.triggerNumLeft = fightBuffStruct.triggerNum;
    }

    /// <summary>
    /// buff持续时间增加
    /// </summary>
    public void AddBuffTime(float buffTime,out bool isRemove, Action actionForCompleteRemove = null)
    {
        timeUpdate += buffTime;
        isRemove = false;
        //触发式BUFF（指定时间后触发 达到触发次数max之后结束）
        if (fightBuffStruct.triggerNum > 0)
        {
            if (timeUpdate >= fightBuffStruct.triggerTime)
            {
                timeUpdate = 0;
                triggerNumLeft--;
                var targetEntity = GetBuffEntity();
                targetEntity.TriggerBuff(this);

                if (triggerNumLeft <= 0)
                {
                    isRemove = true;
                    RemoveBuff(actionForCompleteRemove);
                }
            }
        }           
        //持续型BUFF（持续指定时间后结束）
        else
        {

            if (timeUpdate >= fightBuffStruct.triggerTime)
            {
                timeUpdate = 0;
                isRemove = true;
                RemoveBuff(actionForCompleteRemove);
            }
        }
    }

    /// <summary>
    /// 移除buff
    /// </summary>
    public void RemoveBuff(Action actionForCompleteRemove = null)
    {
        try
        {
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureTypeEnum.None);
            if (targetCreature != null && targetCreature.fightCreatureData != null)
            {
                targetCreature.fightCreatureData.RemoveBuff(this, actionForCompleteRemove);
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"移除战斗buff失败  {e.ToString()}");
        }
    }

    public FightBuffBaseEntity GetBuffEntity()
    {
       return GetBuffEntity(fightBuffInfo.class_entity);
    }


    //buff的实例类
    public static Dictionary<string, FightBuffBaseEntity> dicBuffEntity = new Dictionary<string, FightBuffBaseEntity>();
    /// <summary>
    /// 获取BUFF实例类
    /// </summary>
    public static FightBuffBaseEntity GetBuffEntity(string entityName)
    {
        string className = $"FightBuffEntityFor{entityName}";
        if (dicBuffEntity.TryGetValue(className, out var targetClass))
        {
            return targetClass;
        }
        else
        {
            targetClass = ReflexUtil.CreateInstance<FightBuffBaseEntity>(className);
            if (targetClass == null)
            {
                return null;
            }
            else
            {
                dicBuffEntity.Add(className, targetClass);
                return targetClass;
            }
        }
    }

    /// <summary>
    /// 获取触发的BUFF
    /// </summary>
    /// <param name="targetBuff"></param>
    /// <param name="creatureId"></param>
    /// <returns></returns>
    public static List<FightBuffBean> GetTriggerFightBuff(FightBuffStruct[] targetBuff,string creatureId)
    {
        List<FightBuffBean> listData = new List<FightBuffBean>();
        for (int i = 0; i < targetBuff.Length; i++)
        {
            var itemBuff = targetBuff[i];
            if (itemBuff.buffOdds > 0)
            {
                var randomOdds = UnityEngine.Random.Range(0f, 1f);
                if (randomOdds >= itemBuff.buffOdds)
                {
                    continue;
                }
            }

            FightBuffBean fightBuff = new FightBuffBean(itemBuff, creatureId);
            listData.Add(fightBuff);
        }
        return listData;
    }
}

public struct FightBuffStruct
{
    public int id;
    public float buffOdds;//buff触发几率
    public int triggerNum;//触发次数
    public float triggerTime;//触发时间

    public float triggerValue;//值
    public float triggerValueRate;//值百分比

    public static FightBuffStruct[] GetData(string targetData)
    {
        var buffArray = targetData.Split('&');
        FightBuffStruct[] fightBuffStructs = new FightBuffStruct[buffArray.Length];
        for (int i = 0; i < buffArray.Length; i++)
        {
            FightBuffStruct targetItemData = GetItemData(buffArray[i]);
            fightBuffStructs[i] = targetItemData;
        }
        return fightBuffStructs;
    }

    public static FightBuffStruct GetItemData(string targetData)
    {
        FightBuffStruct fightBuffStruct = new FightBuffStruct();
        string[] dataArray = targetData.Split(',');
        for (int i = 0; i < dataArray.Length; i++)
        {
            var itemData = dataArray[i];
            var itemDataArray = itemData.Split(":");
            var itemDataName = itemDataArray[0];
            var itemDataValue = itemDataArray[1];
            if (itemDataName.Equals("id"))
            {
                fightBuffStruct.id = int.Parse(itemDataValue);
            }
            else if (itemDataName.Equals("triggerNum"))
            {
                fightBuffStruct.triggerNum = int.Parse(itemDataValue);
            }
            else if (itemDataName.Equals("triggerTime"))
            {
                fightBuffStruct.triggerTime = float.Parse(itemDataValue);
            }
            else if (itemDataName.Equals("triggerValue"))
            {
                fightBuffStruct.triggerValue = float.Parse(itemDataValue);
            }
            else if (itemDataName.Equals("triggerValueRate"))
            {
                fightBuffStruct.triggerValueRate = float.Parse(itemDataValue);
            }
            else if (itemDataName.Equals("buffOdds"))
            {
                fightBuffStruct.buffOdds = float.Parse(itemDataValue);
            }
        }
        return fightBuffStruct;
    }
}