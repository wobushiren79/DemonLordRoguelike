using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FightBean
{
    public GameFightTypeEnum gameFightType;//游戏模式

    public float gameTime = 0;//游戏时间
    public float gameSpeed = 1;//游戏速度

    public int fightSceneId;//战斗场景Id;

    public float timeUpdateForAttackCreate = 0;//更新时间-怪物生成
    public float timeUpdateTargetForAttackCreate = 0;//更新目标时间-怪物生成

    public float timeUpdateForFightCreature = 0;//更新目标时间-生物
    public float timeUpdateTargetForFightCreature = 0.1f;//更新目标时间-生物

    public int currentMagic;//当前魔力值

    public int sceneRoadNumMax = 6;//路线数量最大值
    public int sceneRoadNum = 6;//场景路线数量
    public int sceneRoadLength = 10;//场景路线长度
    //进攻数据
    public FightAttackBean fightAttackData;

    //所有卡片防御生物数据
    public DictionaryList<string, CreatureBean> dlDefenseCreatureData = new DictionaryList<string, CreatureBean>();

    //所有的进攻实例
    public DictionaryList<string, GameFightCreatureEntity> dlAttackCreatureEntity = new DictionaryList<string, GameFightCreatureEntity>();
    //所有的防守实例
    public DictionaryList<string, GameFightCreatureEntity> dlDefenseCreatureEntity = new DictionaryList<string, GameFightCreatureEntity>();

    //防守核心数据
    public FightCreatureBean fightDefenseCoreData;
    //防守方核心生物实例
    public GameFightCreatureEntity fightDefenseCoreCreature;

    //战斗数据记录
    public FightRecordsBean fightRecordsData = new FightRecordsBean();

    /// <summary>
    /// 检测是否还拥有进攻生物
    /// </summary>
    public bool CheckHasAttackCreature()
    {
        if (dlAttackCreatureEntity.List.Count > 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        //还原生物数据
        for (int i = 0; i < dlDefenseCreatureData.List.Count; i++)
        {
            var itemCreatureData = dlDefenseCreatureData.List[i];
            itemCreatureData.ClearTempData();
        }
        //删除生物实例
        for(int i = 0; i < dlDefenseCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlDefenseCreatureEntity.List[i];
            if (itemCreature != null && itemCreature.creatureObj != null)
            {
                GameObject.DestroyImmediate(itemCreature.creatureObj);
            }
        }
        for(int i = 0; i < dlAttackCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlAttackCreatureEntity.List[i];
            if (itemCreature != null && itemCreature.creatureObj != null)
            {
                GameObject.DestroyImmediate(itemCreature.creatureObj);
            }
        }

        dlDefenseCreatureData.Clear();
        dlDefenseCreatureEntity.Clear();
        dlAttackCreatureEntity.Clear();

        if (fightDefenseCoreCreature != null && fightDefenseCoreCreature.creatureObj != null)
        {
            GameObject.DestroyImmediate(fightDefenseCoreCreature.creatureObj);
        }
        fightDefenseCoreCreature = null;
        fightDefenseCoreData = null;
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
    /// 检测指定位置上是否有防守生物
    /// </summary>
    public bool CheckDefenseCreatureByPos(Vector3Int targetPos)
    {
        for (int i = 0; i < dlDefenseCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlDefenseCreatureEntity.List[i];
            if (itemCreature.fightCreatureData.positionCreate == targetPos)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取位置上的防守生物
    /// </summary>
    public GameFightCreatureEntity GetDefenseCreatureByPos(Vector3Int targetPos)
    {
        for (int i = 0; i < dlDefenseCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlDefenseCreatureEntity.List[i];
            if (itemCreature.fightCreatureData.positionCreate == targetPos)
            {
                return itemCreature;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取路径上的防守生物
    /// </summary>
    public List<GameFightCreatureEntity> GetDefenseCreatureByRoad(int roadIndex)
    {
        List<GameFightCreatureEntity> listData = new List<GameFightCreatureEntity>();
        for (int i = 0; i < dlDefenseCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlDefenseCreatureEntity.List[i];
            if (itemCreature == null)
                continue;
            if (itemCreature.fightCreatureData.positionCreate.z == roadIndex)
            {
                listData.Add(itemCreature);
            }
        }
        return listData;
    }

    /// <summary>
    /// 移除位置上的防守生物
    /// </summary>
    public void RemoveDefenseCreatureByPos(Vector3Int targetPos)
    {
        var targetCreature = GetDefenseCreatureByPos(targetPos);
        if(targetCreature == null)
            return;
        dlDefenseCreatureEntity.RemoveByKey(targetCreature.fightCreatureData.creatureData.creatureId);
    }

    /// <summary>
    /// 设置位置上的防守生物
    /// </summary>
    public void AddDefenseCreatureByPos(Vector3Int targetPos, GameFightCreatureEntity targetEntity)
    {
        if (targetEntity == null)
            return;
        targetEntity.fightCreatureData.positionCreate = targetPos;
        targetEntity.fightCreatureData.roadIndex = targetPos.z;
        dlDefenseCreatureEntity.Add(targetEntity.fightCreatureData.creatureData.creatureId, targetEntity);
    }

    /// <summary>
    /// 增加进攻生物
    /// </summary>
    public void AddAttackCreatureByRoad(int road, GameFightCreatureEntity targetEntity)
    {
        targetEntity.fightCreatureData.positionCreate = new Vector3Int(0, 0, road);
        targetEntity.fightCreatureData.roadIndex = road;
        dlAttackCreatureEntity.Add(targetEntity.fightCreatureData.creatureData.creatureId, targetEntity);
    }

    /// <summary>
    /// 移除战斗生物
    /// </summary>
    public void RemoveAttackCreature(GameFightCreatureEntity targetEntity)
    {
        dlAttackCreatureEntity.RemoveByKey(targetEntity.fightCreatureData.creatureData.creatureId);
    }

    /// <summary>
    /// 获取某一路所有的进攻生物
    /// </summary>
    public List<GameFightCreatureEntity> GetAttackCreatureByRoad(int road)
    {
        List<GameFightCreatureEntity> listData = new List<GameFightCreatureEntity>();
        for (int i = 0; i < dlAttackCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlAttackCreatureEntity.List[i];
            if (itemCreature.fightCreatureData.roadIndex == road)
            {
                listData.Add(itemCreature);
            }
        }
        return listData;
    }

    /// <summary>
    /// 通过ID获取某一生物
    /// </summary>
    public GameFightCreatureEntity GetCreatureById(string creatureId, CreatureTypeEnum creatureType = CreatureTypeEnum.None)
    {
        if (creatureType == CreatureTypeEnum.None)
        {
            if (dlAttackCreatureEntity.TryGetValue(creatureId, out var attackCreature))
            {
                return attackCreature;
            }
            if (dlDefenseCreatureEntity.TryGetValue(creatureId, out var defenseCreature))
            {
                return defenseCreature;
            }
            return fightDefenseCoreCreature;
        }
        else if (creatureType == CreatureTypeEnum.FightAttack)
        {
            if (dlAttackCreatureEntity.TryGetValue(creatureId, out var attackCreature))
            {
                return attackCreature;
            }
        }
        else if (creatureType == CreatureTypeEnum.FightDefense)
        {
            if (dlDefenseCreatureEntity.TryGetValue(creatureId, out var defenseCreature))
            {
                return defenseCreature;
            }
        }
        else if (creatureType == CreatureTypeEnum.FightDefenseCore)
        {
            return fightDefenseCoreCreature;
        }
        return null;
    }

    /// <summary>
    /// 通过ID获取生物数据（仅限防御生物）
    /// </summary>
    /// <param name="creatureId"></param>
    /// <returns></returns>
    public CreatureBean GetCreatureDataById(string creatureId)
    {
        if (dlDefenseCreatureData.TryGetValue(creatureId, out CreatureBean targetCreature))
        {
            return targetCreature;
        }
        return null;
    }
}
