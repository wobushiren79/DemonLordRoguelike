using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameDataHandler
{
    public float timeUpdate;
    public float timeUpdateMax = 1f;

    public void Update()
    {
        timeUpdate += Time.deltaTime;
        if (timeUpdate > timeUpdateMax)
        {
            timeUpdate = 0;
            HandleForBaseDataUpdate();
        }
    }

    /// <summary>
    /// 处理基础数据
    /// </summary>
    public void HandleForBaseDataUpdate()
    {
        if (manager.userData != null)
        {
            manager.userData.gameTime += 1;
            HandleForAscendData();
        }
    }

    /// <summary>
    /// 处理进阶数据
    /// </summary>
    public void HandleForAscendData()
    {
        if (manager.userData != null)
        {
            var userAscendData = manager.userData.GetUserAscendData();
            if (userAscendData.dicAscendData.Count > 0)
            {
                foreach (var itemData in userAscendData.dicAscendData)
                {
                    itemData.Value.AddProgress();
                }
                EventHandler.Instance.TriggerEvent(EventsInfo.CreatureAscend_AddProgress);
            }
        }
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void ClearUserData()
    {
        manager.userData = null;
    }
}
