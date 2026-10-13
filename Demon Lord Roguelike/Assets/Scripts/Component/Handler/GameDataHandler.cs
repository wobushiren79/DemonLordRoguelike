using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameDataHandler
{
    public float timeUpdate;
    public float timeUpdateMax = 1f;
    //进阶刷新独立计时器(unscaled):进度按真实时间计算,暂停(timeScale=0)时仍在走,需照常刷新UI/场景水色
    public float timeUpdateAscend;

    public void Update()
    {
        //游戏时长统计保持 scaled 语义(暂停不计入)
        timeUpdate += Time.deltaTime;
        if (timeUpdate > timeUpdateMax)
        {
            timeUpdate = 0;
            if (manager.userData != null)
            {
                manager.userData.gameTime += 1;
            }
        }
        //进阶数据处理走真实时间(unscaled),暂停期间照常
        timeUpdateAscend += Time.unscaledDeltaTime;
        if (timeUpdateAscend > timeUpdateMax)
        {
            timeUpdateAscend = 0;
            HandleForAscendData();
        }
    }

    /// <summary>
    /// 处理进阶数据(真实时间口径):进度由 startTimeTicks 时间戳实时计算,此处只做时钟回拨修正(修正后落盘一次)+广播刷新事件,不逐秒累加、不主动存档
    /// </summary>
    public void HandleForAscendData()
    {
        if (manager.userData != null)
        {
            var userAscendData = manager.userData.GetUserAscendData();
            if (userAscendData.dicAscendData.Count > 0)
            {
                //时钟回拨检测:玩家改本地时间导致开始时间晚于当前时间时,重置起点为当前时间并统一落盘一次
                bool isAnyFixed = false;
                foreach (var itemData in userAscendData.dicAscendData)
                {
                    if (itemData.Value.CheckAndFixTimeRollback())
                    {
                        isAnyFixed = true;
                    }
                }
                if (isAnyFixed)
                {
                    manager.SaveUserData();
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
