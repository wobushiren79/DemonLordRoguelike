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
}