using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightBean
{
    public float gameTime = 0;//游戏时间
    public float gameSpeed = 1;//游戏速度

    public int fightSceneId;//战斗场景Id;

    public float timeUpdateForAttackCreate = 0;//更新时间-怪物生成
    public float timeUpdateTargetForAttackCreate = 0;//更新目标时间-怪物生成

    public float timeUpdateForFightCreature = 0;//更新目标时间-生物
    public float timeUpdateTargetForFightCreature = 0.1f;//更新目标时间-生物

    public int currentMagic;//当前魔力值

    //进攻数据
    public FightAttackBean fightAttackData;

    //所有卡片防御生物数据
    public Dictionary<string, CreatureBean> dicDefCreatureData = new Dictionary<string, CreatureBean>();

    //防守生物位置数据
    public Dictionary<Vector3Int, FightPositionBean> dicFightPosition = new Dictionary<Vector3Int, FightPositionBean>();

    //所有进攻方生物实例
    public Dictionary<int, List<GameFightCreatureEntity>> dicAttackCreatureEntity = new Dictionary<int, List<GameFightCreatureEntity>>();

    //所有生物（包含进攻和防守）
    public Dictionary<string, GameFightCreatureEntity> dicCreatureEntity = new Dictionary<string, GameFightCreatureEntity>();

    //防守核心数据
    public FightCreatureBean fightDefCoreData;
    //防守方核心生物实例
    public GameFightCreatureEntity fightDefCoreCreature;

    //战斗数据记录
    public FightRecordsBean fightRecordsData = new FightRecordsBean();

    /// <summary>
    /// 检测是否还拥有进攻生物
    /// </summary>
    public bool CheckHasAttackCreature()
    {
        bool HasAttackCreature = false;
        foreach (var itemData in dicAttackCreatureEntity)
        {
            if (itemData.Value.Count > 0)
            {
                HasAttackCreature = true;
            }
        }
        return HasAttackCreature;
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        foreach (var itemData in dicDefCreatureData)
        {
            var itemCreature = itemData.Value;
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
        dicDefCreatureData.Clear();
        dicCreatureEntity.Clear();
        dicFightPosition.Clear();
        dicAttackCreatureEntity.Clear();

        if (fightDefCoreCreature != null && fightDefCoreCreature.creatureObj != null)
        {
            GameObject.DestroyImmediate(fightDefCoreCreature.creatureObj);
        }
        fightDefCoreCreature = null;
        fightDefCoreData = null;
    }

    /// <summary>
    /// 初始化波数数据
    /// </summary>
    public void InitData()
    {
        timeUpdateForAttackCreate = 0;
        timeUpdateTargetForAttackCreate = 0;
        timeUpdateForFightCreature = 0;
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
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean fightPosition))
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
        if (dicAttackCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
        {
            valueList.Add(targetEntity);
        }
        else
        {
            dicAttackCreatureEntity.Add(road, new List<GameFightCreatureEntity>() { targetEntity });
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
        if (dicAttackCreatureEntity.TryGetValue(targetEntity.fightCreatureData.positionCreate.z, out List<GameFightCreatureEntity> valueList))
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
        if (dicAttackCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
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
        if (dicCreatureEntity.TryGetValue(creatureId, out GameFightCreatureEntity targetCreature))
        {
            return targetCreature;
        }
        return null;
    }
}
