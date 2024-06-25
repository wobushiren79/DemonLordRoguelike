using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureManager : BaseManager
{
    //所有的模型
    public Dictionary<string, GameObject> dicCreatureModel = new Dictionary<string, GameObject>();
    //所有生物的缓存池
    public Dictionary<CreatureTypeEnum, Queue<GameObject>> dicPoolForCreature= new Dictionary<CreatureTypeEnum, Queue<GameObject>>();

    //生物预览
    public GameObject objCreatureSelectPreview;


    /// <summary>
    /// 获取生物预览
    /// </summary>
    /// <returns></returns>
    public GameObject GetCreaureSelectPreview()
    {
        if (objCreatureSelectPreview == null)
        {
            string resPath = $"{PathInfo.CreaturesPrefabPath}/FightCreature_SelectPreview.prefab";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            objCreatureSelectPreview = Instantiate(gameObject, targetModel);
        }
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        objCreatureSelectPreview.transform.eulerAngles = mainCamera.transform.eulerAngles;
        return objCreatureSelectPreview;
    }

    /// <summary>
    /// 加载一个生物obj
    /// </summary>
    /// <param name="creatureId"></param>
    /// <param name="actionForComplete"></param>
    public void LoadCreatureObj(int creatureId, Action<GameObject> actionForComplete)
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
                case CreatureTypeEnum.FightDef:
                    creatureModelName = "FightCreature_Def_1.prefab";
                    break;
                case CreatureTypeEnum.FightAtt:
                    creatureModelName = "FightCreature_Att_1.prefab";
                    break;
                case CreatureTypeEnum.FightDefCore:
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
    public void DestoryCreature(Queue<GameObject> pool, GameObject targetObj)
    {
        targetObj.SetActive(false);
        pool.Enqueue(targetObj);
    }

}
