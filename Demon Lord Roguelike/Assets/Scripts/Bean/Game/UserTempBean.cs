using NUnit.Framework;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserTempBean
{
    //终焉议会通过的提案
    public List<DoomCouncilBaseEntity> listDoomCouncilEntity = new List<DoomCouncilBaseEntity>();

    //传送门随机数据
    public List<GameWorldInfoRandomBean> listPortalWorldInfoRandomData = new List<GameWorldInfoRandomBean>();

    //传送门刷新已使用次数(剩余 = 刷新研究上限 - 已用; 每刷新一次+1, 通关一次世界归0回满)
    public int portalRefreshUsedNum = 0;

    /// <summary>
    /// 添加一个终焉议会提案
    /// </summary>
    public void AddDoomCouncil(DoomCouncilBean doomCouncilData)
    {
        var doomCouncilInfo = doomCouncilData.doomCouncilInfo;
        DoomCouncilBaseEntity doomCouncilEntity = ReflexUtil.CreateInstance<DoomCouncilBaseEntity>(doomCouncilInfo.class_entity_name);
        doomCouncilEntity.doomCouncilBillId = doomCouncilData.doomCouncilBillId;
        if (!doomCouncilEntity.TriggerFirst())
        {
            listDoomCouncilEntity.Add(doomCouncilEntity);
        }
    }

    /// <summary>
    /// 触发终焉议会提议
    /// </summary>
    public void TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum triggerType, int dataInt = 0)
    {
        if (listDoomCouncilEntity.IsNull())
            return;
        for (int i = 0; i < listDoomCouncilEntity.Count; i++)
        {
            var itemEntity = listDoomCouncilEntity[i];
            bool isEnd = true;
            switch (triggerType)
            {
                case TriggerTypeDoomCouncilEntityEnum.WorldEnterGameForBaseScene:
                    isEnd = itemEntity.TriggerWorldEnterGameForBaseScene();
                    break;
                case TriggerTypeDoomCouncilEntityEnum.GameFightLogicEndGame:
                    isEnd = itemEntity.TriggerGameFightLogicEndGame();
                    break;
                case TriggerTypeDoomCouncilEntityEnum.GameFightLogicDropAddCrystal:
                    isEnd = itemEntity.TriggerGameFightLogicDropAddCrystal(dataInt);
                    break;
                case TriggerTypeDoomCouncilEntityEnum.GameFightLogicAddExp:
                    isEnd = itemEntity.TriggerGameFightLogicAddExp(dataInt);
                    break;
            }
            if (isEnd)
            {
                i--;
                listDoomCouncilEntity.Remove(itemEntity);
            }
        }
    }

    /// <summary>
    /// 获取当前已通过议案对下一场战斗敌人的累计强度倍率
    /// 遍历所有在列议案的 GetEnemyIntensityRate() 连乘(默认1); 供征服模式敌人生成时叠加到 intensityRate
    /// </summary>
    /// <returns>敌人强度累计倍率(无议案时为1)</returns>
    public float GetEnemyIntensityRate()
    {
        float rate = 1f;
        if (listDoomCouncilEntity.IsNull())
            return rate;
        for (int i = 0; i < listDoomCouncilEntity.Count; i++)
        {
            rate *= listDoomCouncilEntity[i].GetEnemyIntensityRate();
        }
        return rate;
    }

    /// <summary>
    /// 增加传送门随机数据
    /// </summary>
    public void AddPortalWorldInfoRandomData(GameWorldInfoRandomBean addData)
    {
        listPortalWorldInfoRandomData.Add(addData);
    }

    /// <summary>
    /// 清理传送门随机数据
    /// </summary>
    public void ClearPortalWorldInfoRandomData()
    {
        listPortalWorldInfoRandomData.Clear();
    }

    /// <summary>
    /// 获取传送门刷新剩余次数
    /// 剩余 = 刷新研究上限(UserUnlockBean.GetUnlockPortalRefreshMax) - 已使用次数(portalRefreshUsedNum), 下限 0
    /// 研究升级使上限+1时剩余自动+1; 未解锁(上限0)恒为0
    /// </summary>
    /// <returns>当前还能刷新的次数</returns>
    public int GetPortalRefreshRemainNum()
    {
        int max = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockPortalRefreshMax();
        int remain = max - portalRefreshUsedNum;
        return remain < 0 ? 0 : remain;
    }

    /// <summary>
    /// 消耗一次传送门刷新次数(已使用次数+1)
    /// </summary>
    public void ReducePortalRefreshNum()
    {
        portalRefreshUsedNum++;
    }

    /// <summary>
    /// 回满传送门刷新次数(已使用次数归0; 通关一次世界时调用)
    /// </summary>
    public void RefillPortalRefreshNum()
    {
        portalRefreshUsedNum = 0;
    }
}