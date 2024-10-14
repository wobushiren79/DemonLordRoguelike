using NUnit.Framework;
using Spine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class FightBuffBean
{
    public FightBuffStruct fightBuffStruct;
    public string creatureId;//作用的生物ID
    public float timeUpdate = 0;
    public int triggerNumLeft;//剩下的触发次数

    protected FightBuffInfoBean fightBuffInfo;

    public FightBuffBean(FightBuffStruct fightBuffStruct,string creatureId)
    {
        fightBuffInfo = FightBuffInfoCfg.GetItemData(fightBuffStruct.id);
        this.fightBuffStruct = fightBuffStruct;
        this.creatureId = creatureId;
        this.triggerNumLeft = fightBuffStruct.triggerNum;
    }

    public void AddBuffTime(float buffTime)
    {
        timeUpdate += buffTime;
        if (fightBuffInfo.time_trigger > 0)
        {
            if (timeUpdate >= fightBuffInfo.time_trigger)
            {
                timeUpdate = 0;
                triggerNumLeft--;
                var targetEntity = FightHandler.Instance.manager.GetBuffEntity(fightBuffInfo.class_entity);
                targetEntity.HandleBuff(this);

                if (triggerNumLeft <= 0)
                {
                    GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                    gameFightLogic.fightData.RemoveFightBuff(this);
                }
            }
        }
        else
        {
            if (timeUpdate >= fightBuffStruct.triggerTime)
            {
                GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                gameFightLogic.fightData.RemoveFightBuff(this);
            }
        }
    }

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

    /// <summary>
    /// 合并buff
    /// </summary>
    public static void CombineBuff(List<FightBuffBean> listBuff, List<FightBuffBean> targetBuffs,
        Action<FightBuffBean> actionForCombineNew = null, Action<FightBuffBean> actionForCombineOld = null)
    {
        for (int f = 0; f < targetBuffs.Count; f++)
        {
            var targetBuff = targetBuffs[f];
            bool hasOldBuff = false;
            for (int i = 0; i < listBuff.Count; i++)
            {
                var itemBuff = listBuff[i];
                if (itemBuff.fightBuffStruct.id == targetBuff.fightBuffStruct.id)
                {
                    hasOldBuff = true;
                    itemBuff.triggerNumLeft = targetBuff.triggerNumLeft;
                    actionForCombineOld?.Invoke(itemBuff);
                    break;
                }
            }
            if (!hasOldBuff)
            {
                listBuff.Add(targetBuff);
                actionForCombineNew?.Invoke(targetBuff);

            }
        }
    }
}

public struct FightBuffStruct
{
    public int id;
    public float buffOdds;//buff触发几率
    public int triggerNum;//触发次数
    public float triggerTime;//触发时间
    public int demage;//伤害

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
            else if (itemDataName.Equals("demage"))
            {
                fightBuffStruct.id = int.Parse(itemDataValue);
            }
            else if (itemDataName.Equals("buffOdds"))
            {
                fightBuffStruct.id = int.Parse(itemDataValue);
            }
        }
        return fightBuffStruct;
    }
}