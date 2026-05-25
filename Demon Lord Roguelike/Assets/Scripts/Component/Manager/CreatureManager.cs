using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureManager : BaseManager
{
    //所有的模型
    public Dictionary<string, GameObject> dicCreatureModel = new Dictionary<string, GameObject>();

    //所有生物Obj的缓存池
    public Dictionary<CreatureFightTypeEnum, Queue<GameObject>> dicPoolForFightCreatureObj = new Dictionary<CreatureFightTypeEnum, Queue<GameObject>>();
    //战斗生物entity缓存池
    public Queue<FightCreatureEntity> queuePoolForFightCreatureEntity = new Queue<FightCreatureEntity>();
    //战斗生物数据缓存池
    public Queue<FightCreatureBean> queuePoolForFightCreatureData = new Queue<FightCreatureBean>();
    //生物数据缓存池
    public Queue<CreatureBean> queuePoolForCreatureData = new Queue<CreatureBean>();

    //生物预览
    public GameObject objCreatureSelectPreview;
    public GameObject objCreatureSelectDestory;

    public SkeletonAnimation skeletonAnimationSelectPreview;
    public CreatureBean creatureDataSelectPreview;

    #region 数据清理
    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        //清除预览obj
        if (objCreatureSelectPreview != null)
        {
            DestroyImmediate(objCreatureSelectPreview);
        }

        skeletonAnimationSelectPreview = null;
        creatureDataSelectPreview = null;

        //清除缓存生物obj
        foreach (var itemPool in dicPoolForFightCreatureObj)
        {
            Queue<GameObject> pool = itemPool.Value;
            while (pool.Count > 0)
            {
                var itemObj = pool.Dequeue();
                DestroyImmediate(itemObj);
            }
        }
        dicPoolForFightCreatureObj.Clear();

        //清除缓存战斗生物entity
        queuePoolForFightCreatureEntity.Clear();
        //清除缓存战斗生物数据
        queuePoolForFightCreatureData.Clear();
        //清除缓存生物数据
        queuePoolForCreatureData.Clear();
        //丢弃所有待回收项 (对应的对象池已被清空，再回收会污染状态)
        ClearPendingRecycles();
    }
    #endregion

    #region 获取
    /// <summary>
    /// 获取生物预览
    /// </summary>
    /// <returns></returns>
    public GameObject GetCreatureSelectPreview(CreatureBean creatureData = null)
    {
        if (objCreatureSelectPreview == null)
        {
            string resPath = $"{PathInfo.CreaturesPrefabPath}/FightCreature_SelectPreview.prefab";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            objCreatureSelectPreview = Instantiate(gameObject, targetModel);

            Transform spineTF = objCreatureSelectPreview.transform.Find("Spine");
            skeletonAnimationSelectPreview = spineTF.GetComponent<SkeletonAnimation>();
        }
        CameraHandler.Instance.ChangeAngleForCamera(objCreatureSelectPreview.transform);

        if (creatureData != null)
        {
            if (creatureDataSelectPreview == null || creatureData != creatureDataSelectPreview)
            {
                //设置骨骼数据
                CreatureHandler.Instance.SetCreatureData(skeletonAnimationSelectPreview, creatureData);

                creatureDataSelectPreview = creatureData;
                //修改材质球颜色
                skeletonAnimationSelectPreview.skeleton.A = 0.65f;
                //设置待机动画到第一帧
                SpineHandler.Instance.SetAnimFirstFrame(skeletonAnimationSelectPreview, SpineAnimationStateEnum.Idle, creatureData);
            }
        }
        return objCreatureSelectPreview;
    }

    /// <summary>
    /// 获取选择生物删除预览
    /// </summary>
    /// <returns></returns>
    public GameObject GetCreatureSelectDestroy()
    {
        if (objCreatureSelectDestory == null)
        {
            string resPath = $"{PathInfo.CreaturesPrefabPath}/FightCreature_Destory.prefab";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            objCreatureSelectDestory = Instantiate(gameObject, targetModel);
        }
        return objCreatureSelectDestory;
    }

    /// <summary>
    /// 加载一个议会议员obj
    /// </summary>
    /// <returns></returns>
    public GameObject GetDoomCouncilCreatureObj()
    {
        string creatureModelName = "DoomCouncilCreature_1.prefab";
        string resPath = $"{PathInfo.CreaturesPrefabPath}/{creatureModelName}";
        var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
        if (targetModel == null)
        {
            LogUtil.LogError($"创建生物失败：没有找到资源路径为{resPath}的生物");
            return null;
        }
        var objItem = Instantiate(gameObject, targetModel);
        objItem.SetActive(true);
        return objItem;
    }

    /// <summary>
    /// 加载一个战斗生物obj
    /// </summary>
    public GameObject GetFightCreatureObj(long creatureId, CreatureFightTypeEnum creatureFightType)
    {
        var itemCreatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        if (itemCreatureInfo == null)
        {
            LogUtil.LogError($"创建生物失败：没有找到ID为{creatureId}的生物");
            return null;
        }

        //首先获取缓存池里的物体
        GameObject objItem = null;
        if (dicPoolForFightCreatureObj.TryGetValue(creatureFightType, out Queue<GameObject> poolForCreature))
        {
            if (poolForCreature.Count > 0)
            {
                objItem = poolForCreature.Dequeue();
            }
        }

        //如果没有 则加载创建新的预制
        if (objItem == null)
        {
            string creatureModelName;
            switch (creatureFightType)
            {
                case CreatureFightTypeEnum.FightDefense:
                    creatureModelName = "FightCreature_Def_1.prefab";
                    break;
                case CreatureFightTypeEnum.FightAttack:
                    creatureModelName = "FightCreature_Att_1.prefab";
                    break;
                case CreatureFightTypeEnum.FightDefenseCore:
                    creatureModelName = "FightCreature_DefCore_1.prefab";
                    break;
                default:
                    LogUtil.LogError($"创建生物失败：没有找到creature_type为{itemCreatureInfo.creature_type}的生物");
                    return null;
            }

            string resPath = $"{PathInfo.CreaturesPrefabPath}/{creatureModelName}";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            if (targetModel == null)
            {
                LogUtil.LogError($"创建生物失败：没有找到资源路径为{resPath}的生物");
                return null;
            }
            objItem = Instantiate(gameObject, targetModel);
        }

        objItem.SetActive(true);
        return objItem;
    }

    /// <summary>
    /// 获取战斗生物entity
    /// </summary>
    public FightCreatureEntity GetFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        FightCreatureEntity fightCreatureEntity;
        //留有8个缓存数据
        if (queuePoolForFightCreatureEntity.Count > 0)
        {
            fightCreatureEntity = queuePoolForFightCreatureEntity.Dequeue();
            fightCreatureEntity.SetData(creatureObj, fightCreatureData);
        }
        else
        {
            fightCreatureEntity = new FightCreatureEntity(creatureObj, fightCreatureData);
        }
        return fightCreatureEntity;
    }

    /// <summary>
    /// 获取战斗生物数据
    /// </summary>
    public FightCreatureBean GetFightCreatureData(CreatureBean creatureData, CreatureFightTypeEnum creatureFightType)
    {        
        FightCreatureBean fightCreatureData;
        //留有8个缓存数据
        if (queuePoolForFightCreatureData.Count > 0)
        {
            fightCreatureData = queuePoolForFightCreatureData.Dequeue();
            fightCreatureData.SetData(creatureData, creatureFightType);
        }
        else
        {
            fightCreatureData = new FightCreatureBean(creatureData, creatureFightType);
        }
        return fightCreatureData;
    }

    /// <summary>
    /// 获取生物数据
    /// </summary>
    public CreatureBean GetCreatureData(long creatureId)
    {
        CreatureBean creatureData;
        if (queuePoolForCreatureData.Count > 0)
        {
            creatureData = queuePoolForCreatureData.Dequeue();
            creatureData.SetData(creatureId);
        }
        else
        {
            creatureData = new CreatureBean(creatureId);
        }
        return creatureData;
    }

    /// <summary>
    /// 获取生物数据(NPC)
    /// </summary>
    public CreatureBean GetCreatureData(NpcInfoBean npcInfo)
    {
        CreatureBean creatureData;
        if (queuePoolForCreatureData.Count > 0)
        {
            creatureData = queuePoolForCreatureData.Dequeue();
            creatureData.SetData(npcInfo);
        }
        else
        {
            creatureData = new CreatureBean(npcInfo);
        }
        return creatureData;
    }
    #endregion

    #region 回收
    /// <summary>
    /// 移除生物obj (默认下一帧入池)
    /// </summary>
    public void RemoveFightCreatureObj(GameObject targetObj, CreatureFightTypeEnum creatureType)
    {
        RemoveFightCreatureObj(targetObj, creatureType, RecycleDelay.NextFrame);
    }

    /// <summary>
    /// 移除生物obj
    /// </summary>
    /// <param name="targetObj">要回收的对象</param>
    /// <param name="creatureType">战斗生物类型 (决定入哪个子池)</param>
    /// <param name="delay">回收时机；可用 <see cref="RecycleDelay.Immediate"/> / <see cref="RecycleDelay.NextFrame"/> / <see cref="RecycleDelay.Wait(float)"/></param>
    public void RemoveFightCreatureObj(GameObject targetObj, CreatureFightTypeEnum creatureType, RecycleDelay delay)
    {
        if (targetObj == null)
            return;
        //立即把对象挪出视野，避免本帧最后一次渲染时位置闪现
        targetObj.transform.position = new Vector3(0, -100, 0);
        ScheduleRecycle(() =>
        {
            if (targetObj == null)
                return;
            if (!dicPoolForFightCreatureObj.TryGetValue(creatureType, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                dicPoolForFightCreatureObj.Add(creatureType, pool);
            }
            targetObj.SetActive(false);
            pool.Enqueue(targetObj);
        }, delay);
    }

    /// <summary>
    /// 移除生物entity (默认下一帧入池)
    /// </summary>
    public void RemoveFightCreatureEntity(FightCreatureEntity creatureEntity)
    {
        RemoveFightCreatureEntity(creatureEntity, RecycleDelay.NextFrame);
    }

    /// <summary>
    /// 移除生物entity
    /// </summary>
    /// <param name="creatureEntity">要回收的实例</param>
    /// <param name="delay">回收时机</param>
    public void RemoveFightCreatureEntity(FightCreatureEntity creatureEntity, RecycleDelay delay)
    {
        if (creatureEntity == null)
            return;
        ScheduleRecycle(() => queuePoolForFightCreatureEntity.Enqueue(creatureEntity), delay);
    }

    /// <summary>
    /// 移除战斗生物数据 (默认下一帧入池)
    /// </summary>
    public void RemoveFightCreatureData(FightCreatureBean creatureData)
    {
        RemoveFightCreatureData(creatureData, RecycleDelay.NextFrame);
    }

    /// <summary>
    /// 移除战斗生物数据
    /// </summary>
    /// <param name="creatureData">要回收的数据</param>
    /// <param name="delay">回收时机</param>
    public void RemoveFightCreatureData(FightCreatureBean creatureData, RecycleDelay delay)
    {
        if (creatureData == null)
            return;
        ScheduleRecycle(() => queuePoolForFightCreatureData.Enqueue(creatureData), delay);
    }

    /// <summary>
    /// 移除生物数据 (默认下一帧入池)
    /// </summary>
    public void RemoveCreatureData(CreatureBean creatureData)
    {
        RemoveCreatureData(creatureData, RecycleDelay.NextFrame);
    }

    /// <summary>
    /// 移除生物数据
    /// </summary>
    /// <param name="creatureData">要回收的数据</param>
    /// <param name="delay">回收时机</param>
    public void RemoveCreatureData(CreatureBean creatureData, RecycleDelay delay)
    {
        if (creatureData == null)
            return;
        ScheduleRecycle(() =>
        {
            creatureData.ClearTempData();
            queuePoolForCreatureData.Enqueue(creatureData);
        }, delay);
    }
    #endregion
}
