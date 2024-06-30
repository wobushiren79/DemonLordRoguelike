using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : BaseHandler<CreatureHandler, CreatureManager>
{
    /// <summary>
    /// ���ɷ�����������
    /// </summary>
    public void CreateDefCoreCreature(Action<GameFightCreatureEntity> actionForComplete)
    {
        int creatureId = 99;
        GetCreatureObj(creatureId, (targetObj) =>
        {
            targetObj.transform.position = new Vector3(-1f, 0, 3.5f);
            //��������
            FightCreatureBean fightCreatureData = new FightCreatureBean(creatureId);
            fightCreatureData.positionCreate = new Vector3Int(-1, 0, 0);

            GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);
            gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefCoreCreatureEntity>(actionBeforeStart: (targetEntity) =>
            {
                targetEntity.InitData(gameFightCreatureEntity);
            });
            actionForComplete?.Invoke(gameFightCreatureEntity);
        });
    }

    /// <summary>
    ///  ������������
    /// </summary>
    public void CreateDefCreature(int creatureId, Action<GameObject> actionForComplete)
    {
        GetCreatureObj(creatureId, (targetObj) =>
        {
            actionForComplete?.Invoke(targetObj);
        });
    }

    /// <summary>
    /// ������������
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
        //һ�δ���������
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
    /// ������������
    /// </summary>
    /// <param name="targetRoad">Ŀ�����·�� 0Ϊ���</param>
    public void CreateAttCreature(int creatureId, Action<GameObject> actionForComplete, int targetRoad = 0)
    {
        GetCreatureObj(creatureId, (targetObj) =>
        {
            //�������ĳһ·
            if (targetRoad == 0)
            {
                targetRoad = UnityEngine.Random.Range(1, 7);
            }
            targetObj.transform.position = new Vector3(10f, 0, targetRoad);

            //����ս������
            FightCreatureBean fightCreatureData = new FightCreatureBean(creatureId);
            fightCreatureData.positionCreate = new Vector3Int(0, 0, targetRoad);
            GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);
            gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIAttCreatureEntity>(actionBeforeStart: (targetEntity) =>
            {
                targetEntity.InitData(gameFightCreatureEntity);
            });

            var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            gameLogic.fightData.AddFightAttCreature(targetRoad, gameFightCreatureEntity);
            actionForComplete?.Invoke(targetObj);
        });
    }

    /// <summary>
    /// ��ȡһ�������obj
    /// </summary>
    public void GetCreatureObj(int creatureId, Action<GameObject> actionForComplete)
    {
        manager.LoadCreatureObj(creatureId, (targetObj) =>
        {
            var mainCamera = CameraHandler.Instance.manager.mainCamera;
            Transform rendererTF = targetObj.transform.Find("Spine");
 
            if (rendererTF != null)
            {
                rendererTF.eulerAngles = mainCamera.transform.eulerAngles;
                //���û�м��ع�spine �����һ��
                if (rendererTF.GetComponent<SkeletonAnimation>() == null)
                {
                    SpineHandler.Instance.SetSkeletonAnimation(rendererTF.gameObject, "Creature/Skeleton/Skeleton_SkeletonData.asset", new string[] { "Base", "Head/Head_20" });
                }
            }
            actionForComplete?.Invoke(targetObj);
        });
    }

    /// <summary>
    /// �Ƴ�����obj
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureObj(GameObject targetObj, CreatureTypeEnum creatureType)
    {
        if (targetObj == null)
            return;
        if (manager.dicPoolForCreature.TryGetValue(creatureType, out Queue<GameObject> poolForCreature))
        {
            manager.DestoryCreature(poolForCreature, targetObj);
        }
        else
        {
            Queue<GameObject> newPool = new Queue<GameObject>();
            manager.dicPoolForCreature.Add(creatureType, newPool);
            manager.DestoryCreature(newPool, targetObj);
        }
    }

    /// <summary>
    /// �Ƴ�����ʵ��
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureEntity(GameFightCreatureEntity targetEntity, CreatureTypeEnum creatureType)
    {
        if (targetEntity == null)
            return;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (targetEntity.creatureObj != null)
        {
            RemoveCreatureObj(targetEntity.creatureObj, creatureType);
        }
        if (targetEntity.aiEntity != null)
        {
            AIHandler.Instance.RemoveAIEntity(targetEntity.aiEntity);
        }
        //����Ƿ������� ����Ҫ�Ƴ�λ����Ϣ �ͻ�ԭ��Ƭ
        if (creatureType == CreatureTypeEnum.FightDef)
        {
            gameFightLogic.fightData.RemoveFightPosition(targetEntity.fightCreatureData.positionCreate);
            targetEntity.fightCreatureData.stateForCard = CardStateEnum.FightIdle;
            targetEntity.fightCreatureData.ResetData();
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_RefreshCard, targetEntity.fightCreatureData);
        }
        else if (creatureType == CreatureTypeEnum.FightAtt)
        {
            gameFightLogic.fightData.RemoveFightAttCreature(targetEntity);
        }

    }
}
