using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : BaseHandler<CreatureHandler, CreatureManager>
{
    /// <summary>
    /// ��ȡһ�������obj
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
    /// �Ƴ�����obj
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureObj(GameObject targetObj)
    {
        if (targetObj == null)
            return;
        manager.DestoryCreature(manager.poolForCreatureDef, targetObj);
    }

    /// <summary>
    /// �Ƴ�����ʵ��
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
