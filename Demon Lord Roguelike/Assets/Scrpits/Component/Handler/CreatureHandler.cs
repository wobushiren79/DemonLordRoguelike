using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : BaseHandler<CreatureHandler, CreatureManager>
{
    /// <summary>
    ///  创建防御生物
    /// </summary>
    public void CreateDefCreature(int creatureId, Action<GameObject> actionForComplete)
    {
        GetCreatureObj(creatureId, (targetObj) =>
        {
            actionForComplete?.Invoke(targetObj);
        });
    }

    /// <summary>
    /// 创建进攻生物
    /// </summary>
    public void CreateAttCreature(float curTimeProgress, FightAttCreateDetailsBean fightAttCreateData)
    {
        List<FightAttCreateDetailsTimePointBean> listTimePoint = fightAttCreateData.timePointForCreatures;
        FightAttCreateDetailsTimePointBean targetTimePoint = null;
        for (int f = 0; f < listTimePoint.Count; f++)
        {
            var itemTimePoint = listTimePoint[f];
            if (curTimeProgress >= itemTimePoint.startTimeProgress && curTimeProgress < itemTimePoint.endTimeProgress)
            {
                targetTimePoint = itemTimePoint;
                break;
            }
        }
        if (targetTimePoint == null)
            return;
        //一次创建的数量
        int numCreature = fightAttCreateData.createNum;
        for (int i = 0; i < numCreature; i++)
        {
            int randomCreatureIndex = UnityEngine.Random.Range(0, targetTimePoint.creatureIds.Count);
            int targetCreatureId = targetTimePoint.creatureIds[randomCreatureIndex];
            CreateAttCreature(targetCreatureId, (targetObj) =>
            {

            });
        }
    }

    /// <summary>
    /// 创建进攻生物
    /// </summary>
    /// <param name="targetRoad">目标进攻路线 0为随机</param>
    public void CreateAttCreature(int creatureId, Action<GameObject> actionForComplete, int targetRoad = 0)
    {
        GetCreatureObj(creatureId, (targetObj) =>
        {
            //随机生成某一路
            if (targetRoad == 0)
            {
                targetRoad = UnityEngine.Random.Range(1, 7);
            }
            targetObj.transform.position = new Vector3(10f, 0, -targetRoad);

            //创建战斗生物
            FightCreatureBean fightCreatureData = new FightCreatureBean(creatureId);
            fightCreatureData.positionZCurrent = targetRoad;

            GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity();
            gameFightCreatureEntity.fightCreatureData = fightCreatureData;
            gameFightCreatureEntity.creatureObj = targetObj;
            gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIAttCreatureEntity>(actionBeforeStart: (targetEntity) =>
            {
                targetEntity.InitData(gameFightCreatureEntity);
            });

            var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            gameLogic.fightData.listAttCreatureEntity.Add(gameFightCreatureEntity);
            actionForComplete?.Invoke(targetObj);
        });
    }

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
    public void RemoveCreatureObj(GameObject targetObj, CreatureTypeEnum creatureType)
    {
        if (targetObj == null)
            return;
        Queue<GameObject> targetPool = null;
        switch (creatureType)
        {
            case CreatureTypeEnum.FightDef:
                targetPool = manager.poolForCreatureDef;
                break;
            case CreatureTypeEnum.FightAtt:
                targetPool = manager.poolForCreatureAtt;
                break;
        }
        manager.DestoryCreature(targetPool, targetObj);
    }

    /// <summary>
    /// 移除生物实例
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureEntity(GameFightCreatureEntity targetEntity, CreatureTypeEnum creatureType)
    {
        if (targetEntity == null)
            return;
        if (targetEntity.creatureObj != null)
        {
            Queue<GameObject> targetPool = null;
            switch (creatureType)
            {
                case CreatureTypeEnum.FightDef:
                    targetPool = manager.poolForCreatureDef;
                    break;
                case CreatureTypeEnum.FightAtt:
                    targetPool = manager.poolForCreatureAtt;
                    break;
            }
            manager.DestoryCreature(targetPool, targetEntity.creatureObj);
        }
        if (targetEntity.aiEntity != null)
        {
            AIHandler.Instance.RemoveAIEntity(targetEntity.aiEntity);
        }
    }
}
