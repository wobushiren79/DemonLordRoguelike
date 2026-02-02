using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FightBean
{
    public GameFightTypeEnum gameFightType;//游戏模式

    public float gameTime = 0;//游戏时间
    public float gameSpeed = 1;//游戏速度

    public long fightSceneId;//战斗场景Id;

    public int fightNum;//当前关卡数
    public int figthNumMax;//最大关卡数

    public float timeUpdateForAttackCreate = 0;//更新时间-怪物生成
    public float timeUpdateTargetForAttackCreate = 0;//更新目标时间-怪物生成

    public float timeUpdateForFightCreature = 0;//更新目标时间-生物
    public float timeUpdateTargetForFightCreature = 0.2f;//更新目标时间-生物

    public int sceneRoadNumMax = 6;//路线数量最大值
    public int sceneRoadNum = 6;//场景路线数量
    public int sceneRoadLength = 10;//场景路线长度
    //进攻数据
    public FightAttackBean fightAttackData;
    //游戏是否胜利
    public bool gameIsWin = false;

    //所有卡片防御生物数据
    public DictionaryList<string, CreatureBean> dlDefenseCreatureData = new DictionaryList<string, CreatureBean>();

    //所有的进攻实例
    public DictionaryList<string, FightCreatureEntity> dlAttackCreatureEntity = new DictionaryList<string, FightCreatureEntity>();
    //所有的防守实例
    public DictionaryList<string, FightCreatureEntity> dlDefenseCreatureEntity = new DictionaryList<string, FightCreatureEntity>();

    //防守核心数据
    public FightCreatureBean fightDefenseCoreData;
    //防守方核心生物实例
    public FightCreatureEntity fightDefenseCoreCreature;

    //战斗数据记录
    public FightRecordsBean fightRecordsData = new FightRecordsBean();

    #region 构造函数
    public FightBean()
    {
        
    }

    /// <summary>
    /// 初始化波数数据
    /// </summary>
    public virtual void InitData()
    {
        timeUpdateForAttackCreate = 0;
        timeUpdateTargetForAttackCreate = 0;
        timeUpdateForFightCreature = 0;
    }
    #endregion

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

    #region  数据清理
    /// <summary>
    /// 清理数据
    /// </summary>
    public void ClearEntity()
    {
        //还原生物数据
        for (int i = 0; i < dlDefenseCreatureData.List.Count; i++)
        {
            var itemCreatureData = dlDefenseCreatureData.List[i];
            itemCreatureData.ClearTempData();
        }
        //删除生物实例
        for (int i = 0; i < dlDefenseCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlDefenseCreatureEntity.List[i];
            if (itemCreature != null && itemCreature.creatureObj != null)
            {
                GameObject.DestroyImmediate(itemCreature.creatureObj);
            }
        }
        for (int i = 0; i < dlAttackCreatureEntity.List.Count; i++)
        {
            var itemCreature = dlAttackCreatureEntity.List[i];
            if (itemCreature != null && itemCreature.creatureObj != null)
            {
                GameObject.DestroyImmediate(itemCreature.creatureObj);
            }
        }

        dlDefenseCreatureEntity.Clear();
        dlAttackCreatureEntity.Clear();

        if (fightDefenseCoreCreature != null && fightDefenseCoreCreature.creatureObj != null)
        {
            GameObject.DestroyImmediate(fightDefenseCoreCreature.creatureObj);
        }
        fightDefenseCoreCreature = null;
    }
    #endregion

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
    public FightCreatureEntity GetDefenseCreatureByPos(Vector3Int targetPos)
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
    public List<FightCreatureEntity> GetDefenseCreatureByRoad(int roadIndex)
    {
        List<FightCreatureEntity> listData = new List<FightCreatureEntity>();
        dlDefenseCreatureEntity.List.ForEach((index, itemCreature) =>
        {
            if (itemCreature == null)
                return;
            if (itemCreature.fightCreatureData.roadIndex == roadIndex)
            {
                listData.Add(itemCreature);
            }
        });
        return listData;
    }

    public List<FightCreatureEntity> GetDefenseCreatureByRoad(List<int> roadIndexs)
    {
        List<FightCreatureEntity> listData = new List<FightCreatureEntity>();
        dlDefenseCreatureEntity.List.ForEach((index, itemCreature) =>
        {
            if (itemCreature == null)
                return;
            if (roadIndexs.Contains(itemCreature.fightCreatureData.roadIndex))
            {
                listData.Add(itemCreature);
            }
        });
        return listData;
    }
    /// <summary>
    /// 移除位置上的防守生物
    /// </summary>
    public void RemoveDefenseCreatureByPos(Vector3Int targetPos)
    {
        var targetCreature = GetDefenseCreatureByPos(targetPos);
        if (targetCreature == null)
            return;
        dlDefenseCreatureEntity.RemoveByKey(targetCreature.fightCreatureData.creatureData.creatureUUId);
    }

    /// <summary>
    /// 设置位置上的防守生物
    /// </summary>
    public void AddDefenseCreatureByPos(Vector3Int targetPos, FightCreatureEntity targetEntity)
    {
        if (targetEntity == null)
            return;
        targetEntity.fightCreatureData.positionCreate = targetPos;
        targetEntity.fightCreatureData.roadIndex = targetPos.z;
        dlDefenseCreatureEntity.Add(targetEntity.fightCreatureData.creatureData.creatureUUId, targetEntity);
    }

    /// <summary>
    /// 增加进攻生物
    /// </summary>
    public void AddAttackCreatureByRoad(int road, FightCreatureEntity targetEntity)
    {
        targetEntity.fightCreatureData.positionCreate = new Vector3Int(0, 0, road);
        targetEntity.fightCreatureData.roadIndex = road;
        dlAttackCreatureEntity.Add(targetEntity.fightCreatureData.creatureData.creatureUUId, targetEntity);
    }

    /// <summary>
    /// 移除战斗生物
    /// </summary>
    public void RemoveAttackCreature(FightCreatureEntity targetEntity)
    {
        dlAttackCreatureEntity.RemoveByKey(targetEntity.fightCreatureData.creatureData.creatureUUId);
    }

    /// <summary>
    /// 获取某一路所有的进攻生物
    /// </summary>
    public List<FightCreatureEntity> GetAttackCreatureByRoad(int road)
    {
        List<FightCreatureEntity> listData = new List<FightCreatureEntity>();
        dlAttackCreatureEntity.List.ForEach((index, itemCreature) =>
        {
            if (itemCreature == null)
                return;
            if (itemCreature.fightCreatureData.roadIndex == road)
            {
                listData.Add(itemCreature);
            }
        });
        return listData;
    }

    /// <summary>
    /// 获取某些路所有的进攻生物
    /// </summary>
    /// <param name="roads"></param>
    /// <returns></returns>
    public List<FightCreatureEntity> GetAttackCreatureByRoad(List<int> roads)
    {
        List<FightCreatureEntity> listData = new List<FightCreatureEntity>();
        dlAttackCreatureEntity.List.ForEach((index, itemCreature) =>
        {
            if (itemCreature == null)
                return;
            if (roads.Contains(itemCreature.fightCreatureData.roadIndex))
            {
                listData.Add(itemCreature);
            }
        });
        return listData;
    }

    /// <summary>
    /// 通过ID获取某一生物
    /// </summary>
    public FightCreatureEntity GetCreatureById(string creatureId, CreatureFightTypeEnum creatureType = CreatureFightTypeEnum.None)
    {
        if (creatureType == CreatureFightTypeEnum.None)
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
        else if (creatureType == CreatureFightTypeEnum.FightAttack)
        {
            if (dlAttackCreatureEntity.TryGetValue(creatureId, out var attackCreature))
            {
                return attackCreature;
            }
        }
        else if (creatureType == CreatureFightTypeEnum.FightDefense)
        {
            if (dlDefenseCreatureEntity.TryGetValue(creatureId, out var defenseCreature))
            {
                return defenseCreature;
            }
        }
        else if (creatureType == CreatureFightTypeEnum.FightDefenseCore)
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
