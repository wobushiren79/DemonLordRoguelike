using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightBean
{
    public float gameTime = 0;//游戏时间
    public float gameProgress = 0;//游戏进度
    public float gameSpeed = 1;//游戏速度
    public int gameStage = 0;//游戏波次

    public float timeUpdateForAttCreate = 0;//更新时间-怪物生成
    public float timeUpdateTargetForAttCreate = 0;//更新目标时间-怪物生成

    public int currentMagic;//当前魔力值
    public FightAttCreateDetailsBean currentFightAttCreateDetails;//当前进攻数据

    public List<FightCreatureBean> listDefCreatureData = new List<FightCreatureBean>();//当前可用防御生物数据
    public Dictionary<Vector2Int, FightPositionBean> dicFightPosition = new Dictionary<Vector2Int, FightPositionBean>();//放置在场上的生物数据

    public FightAttCreateBean fightAttCreateData;//进攻数据
    public List<GameFightCreatureEntity> listAttCreatureEntity = new List<GameFightCreatureEntity>();//进攻方的所有生物实例

    /// <summary>
    /// 初始化波数数据
    /// </summary>
    public void InitDataForAttCreateStage(int gameStage)
    {
        this.gameStage = gameStage;
        gameProgress = 0;
        timeUpdateForAttCreate = 0;
        timeUpdateTargetForAttCreate = 0;
        currentFightAttCreateDetails = fightAttCreateData.GetDetailData(gameStage);
        if (currentFightAttCreateDetails != null)
        {
            timeUpdateTargetForAttCreate = currentFightAttCreateDetails.createDelay;
        }
    }

    /// <summary>
    /// 获取进攻生物 进攻波次初始化数据
    /// </summary>
    public void GetAttCreateInitData(out int fightNum)
    {
        fightNum = 0;
        if (fightAttCreateData == null)
            return;
        if (!fightAttCreateData.dicDetailsData.IsNull())
        {
            fightNum = fightAttCreateData.dicDetailsData.Count;
        }
    }

    /// <summary>
    /// 改变魔力
    /// </summary>
    public void ChangeMagic(int changeData)
    {
        currentMagic += changeData;
        if (currentMagic < 0)
            currentMagic = 0;
    }

    /// <summary>
    /// 检测指定战斗位置上是否有生物
    /// </summary>
    public bool CheckFightPositionHasCreature(Vector2Int targetPos)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            if (targetPositionData.creatureMain != null)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 设置战斗位置数据
    /// </summary>
    public void SetFightPosition(Vector2Int targetPos, GameFightCreatureEntity fightCreature)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            targetPositionData.creatureMain = fightCreature;
            targetPositionData.creatureMain.fightCreatureData.positionZCurrent = Mathf.Abs(targetPos.y);
        }
        else
        {
            FightPositionBean newPositionData = new FightPositionBean();
            newPositionData.creatureMain = fightCreature;
            newPositionData.creatureMain.fightCreatureData.positionZCurrent = Mathf.Abs(targetPos.y);
            dicFightPosition.Add(targetPos, newPositionData);
        }
    }

    /// <summary>
    /// 获取战斗位置数据
    /// </summary>
    /// <returns></returns>
    public List<FightPositionBean> GetFightPosition(int roadIndex)
    {
        List<FightPositionBean> listData = new List<FightPositionBean>();
        for (int i = 1; i <= 10; i++)
        {
            Vector2Int targetPosition = new Vector2Int(i, -roadIndex);
            if (dicFightPosition.TryGetValue(targetPosition, out FightPositionBean targetPositionData))
            {
                if (targetPositionData != null)
                {
                    listData.Add(targetPositionData);
                }
            }
        }
        return listData;
    }
}
