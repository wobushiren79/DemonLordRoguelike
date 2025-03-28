using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : BaseHandler<CreatureHandler, CreatureManager>
{
    /// <summary>
    /// 生成防御核心生物
    /// </summary>
    public void CreateDefenseCoreCreature(CreatureBean creatureData, Vector3 creaturePos, Action<GameFightCreatureEntity> actionForComplete)
    {
        GetCreatureObj(creatureData.id, (targetObj) =>
        {
            targetObj.transform.position = creaturePos;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            //创建生物
            FightCreatureBean fightCreatureData = gameFightLogic.fightData.fightDefenseCoreData;
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
    ///  创建防御生物
    /// </summary>
    public void CreateDefenseCreature(CreatureBean creatureData, Action<GameObject> actionForComplete)
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
    /// 创建进攻生物
    /// </summary>
    public void CreateAttackCreature(FightAttackDetailsBean fightAttackDetails,int roadNum)
    {
        //一次创建的数量
        int numCreature = fightAttackDetails.creatureIds.Count;
        for (int i = 0; i < numCreature; i++)
        {
            var creatureId = fightAttackDetails.creatureIds[i];
            CreateAttackCreature(creatureId, roadNum, (targetObj) =>
            {

            });
        }
        UIHandler.Instance.RefreshUI();
    }

    /// <summary>
    /// 创建进攻生物
    /// </summary>
    /// <param name="targetRoad">目标进攻路线 0为随机</param>
    public void CreateAttackCreature(int creatureId,int roadNum, Action<GameObject> actionForComplete, int targetRoad = 0)
    {
        GetCreatureObj(creatureId, (targetObj) =>
        {
            //随机生成某一路
            if (targetRoad == 0)
            {
                targetRoad = UnityEngine.Random.Range(1, roadNum + 1);
            }
            targetObj.transform.position = new Vector3(12f, 0, targetRoad);

            //创建战斗生物
            FightCreatureBean fightCreatureData = new FightCreatureBean(creatureId);
            fightCreatureData.creatureData.AddSkinForBase();
            fightCreatureData.positionCreate = new Vector3Int(0, 0, targetRoad);
            GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);
            gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIAttCreatureEntity>(actionBeforeStart: (targetEntity) =>
            {
                targetEntity.InitData(gameFightCreatureEntity);
            });

            var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            gameLogic.fightData.AddAttackCreatureByRoad(targetRoad, gameFightCreatureEntity);
            actionForComplete?.Invoke(targetObj);
        });
    }

    /// <summary>
    /// 获取一个生物的obj
    /// </summary>
    public void GetCreatureObj(long creatureId, Action<GameObject> actionForComplete)
    {
        manager.LoadCreatureObj(creatureId, (targetObj) =>
        {
            var mainCamera = CameraHandler.Instance.manager.mainCamera;
            Transform rendererTF = targetObj.transform.Find("Spine");
            Transform lifeShowTF = targetObj.transform.Find("LifeShow");
            if (rendererTF != null)
            {
                //rendererTF.eulerAngles = Vector3.zero;
                rendererTF.eulerAngles = mainCamera.transform.eulerAngles;
                //如果没有加载过spine 则加载一次 
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
                //lifeShowTF.eulerAngles = Vector3.zero;
                lifeShowTF.eulerAngles = mainCamera.transform.eulerAngles;
                lifeShowTF.ShowObj(false);
            }
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
    /// 移除生物实例
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveCreatureEntity(GameFightCreatureEntity targetEntity, CreatureTypeEnum creatureType)
    {
        if (targetEntity == null)
            return;
        //清理动画
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
        //如果是防守生物 还需要移除位置信息 和还原卡片
        if (creatureType == CreatureTypeEnum.FightDefense)
        {
            gameFightLogic.fightData.RemoveDefenseCreatureByPos(targetEntity.fightCreatureData.positionCreate);
            targetEntity.fightCreatureData.creatureData.creatureState = CreatureStateEnum.Rest;
            targetEntity.fightCreatureData.ResetData();
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureChangeState, targetEntity.fightCreatureData.creatureData.creatureId, CreatureStateEnum.Rest);
        }
        else if (creatureType == CreatureTypeEnum.FightAttack)
        {
            gameFightLogic.fightData.RemoveAttackCreature(targetEntity);
        }

    }
}
