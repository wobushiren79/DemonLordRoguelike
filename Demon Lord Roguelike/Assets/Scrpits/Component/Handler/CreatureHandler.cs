using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : BaseHandler<CreatureHandler, CreatureManager>
{
    /// <summary>
    /// 获取一个生物的obj
    /// </summary>
    public void GetCreatureObj(int creatureId, Action<GameObject> actionForComplete)
    {
        manager.LoadCreatureObj(creatureId, (targetObj) =>
        {
            var mainCamera = CameraHandler.Instance.manager.mainCamera;
            targetObj.transform.eulerAngles = mainCamera.transform.eulerAngles;
            actionForComplete?.Invoke(targetObj);
        });
    }

    /// <summary>
    /// 移除生物obj
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureObj(GameObject targetObj)
    {
        if (targetObj == null)
            return;
        manager.DestoryCreature(manager.poolForCreatureDef, targetObj);
    }

    /// <summary>
    /// 移除生物实例
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureEntity(GameFightCreatureEntity targetEntity)
    {
        if (targetEntity == null)
            return;
        if (targetEntity.creatureObj != null)
        {
            manager.DestoryCreature(manager.poolForCreatureDef, targetEntity.creatureObj);
        }
        if (targetEntity.aiEntity != null)
        {
            AIHandler.Instance.RemoveAIEntity(targetEntity.aiEntity);
        }
    }
}
