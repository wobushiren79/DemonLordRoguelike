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
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            //��������
            FightCreatureBean fightCreatureData = gameFightLogic.fightData.fightDefCoreData;
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
    public void CreateDefCreature(CreatureBean creatureData, Action<GameObject> actionForComplete)
    {
        GetCreatureObj((int)creatureData.id, (targetObj) =>
        {
            Transform rendererTF = targetObj.transform.Find("Spine");
            SkeletonAnimation targetSkeletonAnimation = rendererTF.GetComponent<SkeletonAnimation>();
            if (targetSkeletonAnimation != null)
            {
                string[] skinArray = creatureData.GetSkinArray();
                SpineHandler.Instance.ChangeSkeletonSkin(targetSkeletonAnimation.skeleton, skinArray);
            }
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
            fightCreatureData.creatureData.AddSkin(2000001);
            fightCreatureData.creatureData.AddSkin(2040001);
            fightCreatureData.creatureData.AddSkin(2900001);
            fightCreatureData.creatureData.AddSkin(2020001);
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
            Transform lifeShowTF = targetObj.transform.Find("LifeShow");
            if (rendererTF != null)
            {
                rendererTF.eulerAngles = mainCamera.transform.eulerAngles;
                //���û�м��ع�spine �����һ�� 
                if (rendererTF.GetComponent<SkeletonAnimation>() == null)
                {
                    var creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
                    var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
                    SpineHandler.Instance.AddSkeletonAnimation(rendererTF.gameObject, creatureModel.res_name);
                    rendererTF.transform.localScale = Vector3.one * creatureModel.size_spine;
                    //var render = rendererTF.GetComponent<MeshRenderer>();
                    //if (render != null)
                    //{
                    //    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    //}
                }
            }
            if (lifeShowTF != null)
            {
                lifeShowTF.eulerAngles = mainCamera.transform.eulerAngles;
                lifeShowTF.ShowObj(false);
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
        //������
        targetEntity.ClearAnim();
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
