using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
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

    public int fightSceneId;//战斗场景Id;

    public float timeUpdateForAttCreate = 0;//更新时间-怪物生成
    public float timeUpdateTargetForAttCreate = 0;//更新目标时间-怪物生成

    public int currentMagic;//当前魔力值
    public FightAttCreateDetailsBean currentFightAttCreateDetails;//当前进攻数据

    public List<CreatureBean> listDefCreatureData = new List<CreatureBean>();//当前可用防御生物数据

    public Dictionary<Vector3Int, FightPositionBean> dicFightPosition = new Dictionary<Vector3Int, FightPositionBean>();//放置在场上的生物数据

    public FightAttCreateBean fightAttCreateData;//进攻数据
    public Dictionary<int, List<GameFightCreatureEntity>> dicAttCreatureEntity = new Dictionary<int, List<GameFightCreatureEntity>>();//进攻方的所有生物实例

    public Dictionary<string, GameFightCreatureEntity> dicCreatureEntity = new Dictionary<string, GameFightCreatureEntity>();//所有生物实例

    public FightCreatureBean fightDefCoreData;//防守核心数据
    public GameFightCreatureEntity fightDefCoreCreature;//防守方核心生物实例


    public float timeUpdateForFightBuff = 0;//更新时间-战斗buff
    public float timeUpdateMaxForFightBuff = 0.1f;//更新时间-战斗buff

    public List<FightBuffBean> listBuff = new List<FightBuffBean>();//场上所有的buff
    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < listDefCreatureData.Count; i++)
        {
            var itemCreature = listDefCreatureData[i];
            itemCreature.creatureState = CreatureStateEnum.Idle;
        }

        foreach (var item in dicCreatureEntity)
        {
            var itemValue = item.Value;
            if (itemValue != null && itemValue.creatureObj != null)
            {
                GameObject.DestroyImmediate(itemValue.creatureObj);
            }
        }
        dicCreatureEntity.Clear();
        dicFightPosition.Clear();
        dicAttCreatureEntity.Clear();

        if (fightDefCoreCreature != null && fightDefCoreCreature.creatureObj != null)
        {
            GameObject.DestroyImmediate(fightDefCoreCreature.creatureObj);
        }
        fightDefCoreCreature = null;
        fightDefCoreData = null;

        timeUpdateForFightBuff = 0;
        listBuff.Clear();
    }

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
    /// 获取所有buff
    /// </summary>
    /// <returns></returns>
    public List<FightBuffBean> GetAllBuff()
    {
        return listBuff;
    }

    /// <summary>
    /// 获取进攻生物 进攻波次初始化数据
    /// </summary>
    public void GetAttCreatureInitData(out int fightNum)
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
        EventHandler.Instance.TriggerEvent(EventsInfo.Magic_Change);
    }

    /// <summary>
    /// 检测指定战斗位置上是否有生物
    /// </summary>
    public bool CheckFightPositionHasCreature(Vector3Int targetPos)
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
    /// 移除战斗位置数据
    /// </summary>
    public void RemoveFightPosition(Vector3Int targetPos)
    {
        if (dicFightPosition.TryGetValue(targetPos,out FightPositionBean fightPosition))
        {
            dicFightPosition.Remove(targetPos);

            if (fightPosition.creatureMain != null && dicCreatureEntity.ContainsKey(fightPosition.creatureMain.fightCreatureData.creatureData.creatureId))
            {
                dicCreatureEntity.Remove(fightPosition.creatureMain.fightCreatureData.creatureData.creatureId);
            }
            if (fightPosition.creatureAssist != null && dicCreatureEntity.ContainsKey(fightPosition.creatureAssist.fightCreatureData.creatureData.creatureId))
            {
                dicCreatureEntity.Remove(fightPosition.creatureAssist.fightCreatureData.creatureData.creatureId);
            }
        }
    }

    /// <summary>
    /// 设置战斗位置数据
    /// </summary>
    public void SetFightPosition(Vector3Int targetPos, GameFightCreatureEntity fightCreature)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            targetPositionData.creatureMain = fightCreature;
            targetPositionData.creatureMain.fightCreatureData.positionCreate = targetPos;
        }
        else
        {
            FightPositionBean newPositionData = new FightPositionBean();
            newPositionData.creatureMain = fightCreature;
            newPositionData.creatureMain.fightCreatureData.positionCreate = targetPos;
            dicFightPosition.Add(targetPos, newPositionData);
        }

        if (!dicCreatureEntity.ContainsKey(fightCreature.fightCreatureData.creatureData.creatureId))
        {
            dicCreatureEntity.Add(fightCreature.fightCreatureData.creatureData.creatureId, fightCreature);
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
            Vector3Int targetPosition = new Vector3Int(i, 0, roadIndex);
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


    /// <summary>
    /// 增加战斗生物
    /// </summary>
    public void AddFightAttCreature(int road, GameFightCreatureEntity targetEntity)
    {
        if (dicAttCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
        {
            valueList.Add(targetEntity);
        }
        else
        {
            dicAttCreatureEntity.Add(road, new List<GameFightCreatureEntity>() { targetEntity });
        }

        if (!dicCreatureEntity.ContainsKey(targetEntity.fightCreatureData.creatureData.creatureId))
        {
            dicCreatureEntity.Add(targetEntity.fightCreatureData.creatureData.creatureId, targetEntity);
        }
    }

    /// <summary>
    /// 移除战斗生物
    /// </summary>
    public void RemoveFightAttCreature(GameFightCreatureEntity targetEntity)
    {
        if (dicAttCreatureEntity.TryGetValue(targetEntity.fightCreatureData.positionCreate.z, out List<GameFightCreatureEntity> valueList))
        {
            valueList.Remove(targetEntity);
        }
        if (dicCreatureEntity.ContainsKey(targetEntity.fightCreatureData.creatureData.creatureId))
        {
            dicCreatureEntity.Remove(targetEntity.fightCreatureData.creatureData.creatureId);
        }
    }

    /// <summary>
    /// 获取某一路所有的进攻生物
    /// </summary>
    /// <param name="road"></param>
    /// <returns></returns>
    public List<GameFightCreatureEntity> GetFightAttCreatureByRoad(int road)
    {
        if (dicAttCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
        {
            return valueList;
        }
        return null;
    }

    /// <summary>
    /// 通过ID获取某一生物
    /// </summary>
    /// <param name="creatureId"></param>
    /// <returns></returns>
    public GameFightCreatureEntity GetFightCreatureById(string creatureId)
    {
        if (dicCreatureEntity.TryGetValue(creatureId,out GameFightCreatureEntity targetCreature))
        {
            return targetCreature;
        }
        return null;
    }

    /// <summary>
    /// 添加一个战斗BUFF
    /// </summary>
    /// <param name="fightBuffData"></param>
    public void AddFightBuff(FightBuffBean fightBuffData)
    {
        listBuff.Add(fightBuffData);
    }
     
    /// <summary>
    /// 移除一个战斗BUFF
    /// </summary>
    public void RemoveFightBuff(FightBuffBean fightBuffData)
    {
        try
        {
            listBuff.Remove(fightBuffData);
            var targetCreature = GetFightCreatureById(fightBuffData.creatureId);
            if (targetCreature != null && targetCreature.fightCreatureData != null && !targetCreature.fightCreatureData.listBuff.IsNull())
            {
                targetCreature.fightCreatureData.listBuff.Remove(fightBuffData);
                targetCreature.fightCreatureData.InitBaseAttribute();
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"移除战斗buff失败  {e.ToString()}");
        }
    }
}
