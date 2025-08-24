using NUnit.Framework;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Spine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class BuffEntityBean
{
    public BuffInfoBean buffInfo;
    //buff施加者
    public string giveCreatureId;
    //buff触发者
    public string getCreatureId;

    public float timeUpdate = 0;
    public int triggerNumLeft;//剩下的触发次数

    public BuffEntityBean(string giveCreatureId, string getCreatureId, long buffId)
    {
        buffInfo = BuffInfoCfg.GetItemData(buffId);
        if (buffInfo == null)
        {
            LogUtil.LogError($"buff初始化失败 没有找到giveCreatureId_{giveCreatureId} getCreatureId_{getCreatureId}  buffId_{buffId}");
        }
        this.giveCreatureId = giveCreatureId;
        this.getCreatureId = getCreatureId;
        this.triggerNumLeft = buffInfo.trigger_num;
    }

    /// <summary>
    /// buff持续时间增加
    /// </summary>
    public void AddBuffTime(float buffTime, out bool isRemove, Action actionForCompleteRemove = null)
    {
        timeUpdate += buffTime;
        isRemove = false;
        //触发式BUFF（指定时间后触发 达到触发次数max之后结束）
        if (buffInfo.trigger_num > 0)
        {
            if (timeUpdate >= buffInfo.trigger_time)
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
            //如果是永久存在
            if (buffInfo.trigger_time == -1)
            {

            }
            //如果不是永久存在
            else
            {
                if (timeUpdate >= buffInfo.trigger_time)
                {
                    timeUpdate = 0;
                    isRemove = true;
                    RemoveBuff(actionForCompleteRemove);
                }
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
            var targetCreature = gameFightLogic.fightData.GetCreatureById(getCreatureId, CreatureTypeEnum.None);
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

    /// <summary>
    /// 获取buff实例
    /// </summary>
    public BuffBaseEntity GetBuffEntity()
    {
        return BuffUtil.GetBuffEntity(buffInfo);
    }
}