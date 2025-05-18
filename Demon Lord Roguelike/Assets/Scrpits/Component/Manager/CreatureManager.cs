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
        //objCreatureSelectPreview.transform.eulerAngles = Vector3.zero;
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        objCreatureSelectPreview.transform.eulerAngles = mainCamera.transform.eulerAngles;

        if (creatureData != null)
        {
            if (creatureDataSelectPreview == null || creatureData != creatureDataSelectPreview)
            {
                //设置骨骼数据
                CreatureHandler.Instance.SetCreatureData(skeletonAnimationSelectPreview, creatureData, isNeedWeapon: false);

                creatureDataSelectPreview = creatureData;
                //修改材质球颜色
                skeletonAnimationSelectPreview.skeleton.A = 0.65f;

                Transform spineTF = objCreatureSelectPreview.transform.Find("Spine");
                spineTF.transform.localScale = Vector3.one * creatureData.creatureModel.size_spine;
            }
        }
        return objCreatureSelectPreview;
    }

    /// <summary>
    /// 获取选择生物删除
    /// </summary>
    /// <returns></returns>
    public GameObject GetCreatureSelectDestory()
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
    /// 加载一个生物obj
    /// </summary>
    /// <param name="creatureId"></param>
    /// <param name="actionForComplete"></param>
    public void LoadCreatureObj(long creatureId, Action<GameObject> actionForComplete)
    {
        var itemCreatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        if (itemCreatureInfo == null)
        {
            LogUtil.LogError($"创建生物失败：没有找到ID为{creatureId}的生物");
            return;
        }

        CreatureTypeEnum creatureType = itemCreatureInfo.GetCreatureType();
        //首先获取缓存池里的物体
        GameObject objItem = null;
        if (dicPoolForCreature.TryGetValue(creatureType, out Queue<GameObject> poolForCreature))
        {
            objItem = GetCreaureFromPool(poolForCreature);
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
                    return;
            }

            string resPath = $"{PathInfo.CreaturesPrefabPath}/{creatureModelName}";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            if (targetModel == null)
            {
                LogUtil.LogError($"创建生物失败：没有找到资源路径为{resPath}的生物");
                return;
            }
            objItem = Instantiate(gameObject, targetModel);
        }

        objItem.gameObject.SetActive(true);
        actionForComplete?.Invoke(objItem);
    }

    /// <summary>
    /// 从缓存池中获取对象
    /// </summary>
    /// <param name="pool"></param>
    public GameObject GetCreaureFromPool(Queue<GameObject> pool)
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
    public async void DestoryCreature(Queue<GameObject> pool, GameObject targetObj)
    {
        targetObj.transform.position = new Vector3(0, -100, 0);
        //等待1帧防止 当前动作闪现问题
        await new WaitNextFrame();
        targetObj.SetActive(false);
        pool.Enqueue(targetObj);
    }

}
