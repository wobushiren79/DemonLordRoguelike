using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureManager : BaseManager
{
    //所有的模型
    public Dictionary<string, GameObject> dicCreatureModel = new Dictionary<string, GameObject>();
    //所有生物的缓存池
    public Dictionary<CreatureTypeEnum, Queue<GameObject>> dicPoolForCreature = new Dictionary<CreatureTypeEnum, Queue<GameObject>>();

    //生物预览
    public GameObject objCreatureSelectPreview;
    public GameObject objCreatureSelectDestory;

    public SkeletonAnimation skeletonAnimationSelectPreview;
    public CreatureBean creatureDataSelectPreview;

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        if (objCreatureSelectPreview != null)
            DestroyImmediate(objCreatureSelectPreview);
        skeletonAnimationSelectPreview = null;
        creatureDataSelectPreview = null;

        foreach (var itemPool in dicPoolForCreature)
        {
            Queue<GameObject> pool = itemPool.Value;
            while (pool.Count > 0)
            {
                var itemObj = pool.Dequeue();
                DestroyImmediate(itemObj);
            }
        }
        dicPoolForCreature.Clear();
    }

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
                CreatureHandler.Instance.SetCreatureData(skeletonAnimationSelectPreview, creatureData, isNeedWeapon: false);

                creatureDataSelectPreview = creatureData;
                //修改材质球颜色
                skeletonAnimationSelectPreview.skeleton.A = 0.65f;
            }
        }
        return objCreatureSelectPreview;
    }

    /// <summary>
    /// 获取选择生物删除
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
    public GameObject LoadDoomCouncilCreatureObj()
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
    public GameObject LoadFightCreatureObj(long creatureId)
    {
        var itemCreatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        if (itemCreatureInfo == null)
        {
            LogUtil.LogError($"创建生物失败：没有找到ID为{creatureId}的生物");
            return null;
        }

        CreatureTypeEnum creatureType = itemCreatureInfo.GetCreatureType();
        //首先获取缓存池里的物体
        GameObject objItem = null;
        if (dicPoolForCreature.TryGetValue(creatureType, out Queue<GameObject> poolForCreature))
        {
            objItem = GetFightCreaureFromPool(poolForCreature);
        }

        //如果没有 则加载创建新的预制
        if (objItem == null)
        {
            string creatureModelName;
            switch (creatureType)
            {
                case CreatureTypeEnum.FightDefense:
                    creatureModelName = "FightCreature_Def_1.prefab";
                    break;
                case CreatureTypeEnum.FightAttack:
                    creatureModelName = "FightCreature_Att_1.prefab";
                    break;
                case CreatureTypeEnum.FightDefenseCore:
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
    /// 从缓存池中获取对象
    /// </summary>
    /// <param name="pool"></param>
    public GameObject GetFightCreaureFromPool(Queue<GameObject> pool)
    {
        if (pool.Count <= 0)
        {
            return null;
        }
        return pool.Dequeue();
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    public async void DestoryFightCreature(Queue<GameObject> pool, GameObject targetObj)
    {
        targetObj.transform.position = new Vector3(0, -100, 0);
        //等待1帧防止 当前动作闪现问题
        await new WaitNextFrame();
        targetObj.SetActive(false);
        pool.Enqueue(targetObj);
    }

}
