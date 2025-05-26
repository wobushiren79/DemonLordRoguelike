using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : BaseHandler<CreatureHandler, CreatureManager>
{
    /// <summary>
    /// 设置生物数据
    /// </summary>
    public void SetCreatureData(SkeletonAnimation skeletonAnimation, CreatureBean creatureData, bool isSetSkeletonDataAsset = true, bool isUIShow = false, bool isNeedWeapon = true)
    {
        SetCreatureData(skeletonAnimation, null, creatureData, isSetSkeletonDataAsset, isUIShow, isNeedWeapon);
    }

    /// <summary>
    /// 设置生物数据
    /// </summary>
    public void SetCreatureData(SkeletonGraphic skeletonGraphic, CreatureBean creatureData, bool isSetSkeletonDataAsset = true, bool isUIShow = false, bool isNeedWeapon = true)
    {
        SetCreatureData(null, skeletonGraphic, creatureData, isSetSkeletonDataAsset, isUIShow, isNeedWeapon);
    }

    /// <summary>
    /// 设置生物数据
    /// </summary>
    public void SetCreatureData(SkeletonAnimation skeletonAnimation, SkeletonGraphic skeletonGraphic, CreatureBean creatureData, bool isSetSkeletonDataAsset = true, bool isUIShow = false, bool isNeedWeapon = true)
    {
        if (creatureData == null)
        {
            LogUtil.LogError("设置spine错误 没有生物数据");
            return;
        }
        string resName = creatureData.creatureModel.res_name;
        int skinType = 0;
        if (isUIShow)
        {
            creatureData.creatureModel.GetShowRes(out resName, out skinType);
        }
        string[] skinArray = creatureData.GetSkinArray(showType: skinType, isNeedWeapon: isNeedWeapon);
        //设置SkeletonAnimation
        if (skeletonAnimation != null)
        {
            if (isSetSkeletonDataAsset)
            {
                SpineHandler.Instance.SetSkeletonDataAsset(skeletonAnimation, resName);
            }
            //修改皮肤
            SpineHandler.Instance.ChangeSkeletonSkin(skeletonAnimation.skeleton, skinArray);
        }
        //设置SkeletonGraphic
        if (skeletonGraphic != null)
        {
            if (isSetSkeletonDataAsset)
            {
                SpineHandler.Instance.SetSkeletonDataAsset(skeletonGraphic, resName);
            }
            //修改皮肤
            SpineHandler.Instance.ChangeSkeletonSkin(skeletonGraphic.Skeleton, skinArray);
        }
    }

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
            SetCreatureData(targetSkeletonAnimation, creatureData, isSetSkeletonDataAsset: false);
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
            float randomX = UnityEngine.Random.Range(11f, 12f);
            targetObj.transform.position = new Vector3(randomX, 0, targetRoad);

            //创建战斗生物
            FightCreatureBean fightCreatureData = new FightCreatureBean(creatureId);
            fightCreatureData.creatureData.AddSkinForBase();
            fightCreatureData.positionCreate = new Vector3Int(0, 0, targetRoad);

            GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);

            //先添加数据
            var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            gameLogic.fightData.AddAttackCreatureByRoad(targetRoad, gameFightCreatureEntity);
            
            //再创建数据
            gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIAttCreatureEntity>(actionBeforeStart: (targetEntity) =>
            {
                targetEntity.InitData(gameFightCreatureEntity);
            });

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
            var creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
            var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
            //设置层级
            if (!creatureInfo.creature_layer.IsNull())
            {
                targetObj.layer = LayerMask.NameToLayer($"{creatureInfo.creature_layer}");
            }
            else
            {
                var creatureType = creatureInfo.GetCreatureType();
                switch(creatureType)
                {
                    case CreatureTypeEnum.FightAttack:
                        targetObj.layer = LayerInfo.CreatureAtt;
                        break;
                    case CreatureTypeEnum.FightDefense:
                        targetObj.layer = LayerInfo.CreatureDef;
                        break;
                }
            }

            Transform rendererTF = targetObj.transform.Find("Spine");
            Transform lifeShowTF = targetObj.transform.Find("LifeShow");
            if (rendererTF != null)
            {
                CameraHandler.Instance.ChangeAngleForCamera(rendererTF);
                //如果没有加载过spine 则加载一次 
                if (rendererTF.GetComponent<SkeletonAnimation>() == null)
                {
                    SpineHandler.Instance.AddSkeletonAnimation(rendererTF.gameObject, creatureModel.res_name);
                    rendererTF.transform.localScale = Vector3.one * creatureModel.size_spine;
                    //var render = rendererTF.GetComponent<MeshRenderer>();
                    //if (render != null)
                    //{
                    //    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    //}
                }

                rendererTF.localPosition = Vector3.zero;
                //设置前后
                if (targetObj.layer == LayerInfo.CreatureDef_Front || targetObj.layer == LayerInfo.CreatureAtt_Front)
                {
                    rendererTF.position = rendererTF.position.AddZ(-0.1f);
                }
                else if (targetObj.layer == LayerInfo.CreatureDef_Front || targetObj.layer == LayerInfo.CreatureAtt_Front)
                {
                    rendererTF.position = rendererTF.position.AddZ(0.1f);
                }
                else
                {

                }
            }
            if (lifeShowTF != null)
            {
                CameraHandler.Instance.ChangeAngleForCamera(lifeShowTF);
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
