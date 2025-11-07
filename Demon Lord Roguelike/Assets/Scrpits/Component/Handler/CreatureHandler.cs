using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            //设置模型大小
            skeletonAnimation.transform.localScale = Vector3.one * creatureData.creatureModel.size_spine;
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

    #region 终焉议会议员
    /// <summary>
    /// 生成议会议员
    /// </summary>
    public async Task<GameObject> CreateDoomCouncilCreature(CreatureBean creatureData, Vector3 creaturePos)
    {
        var targetObj = manager.LoadDoomCouncilCreatureObj();
        Transform rendererTF = targetObj.transform.Find("Spine");
        SkeletonAnimation skeletonAnimation = rendererTF.GetComponent<SkeletonAnimation>();
        SetCreatureData(skeletonAnimation, creatureData, isNeedWeapon: false);
        targetObj.transform.position = creaturePos;
        return targetObj;
    }
    #endregion

    #region  防御核心生物
    /// <summary>
    /// 生成防御核心生物
    /// </summary>
    public async Task<GameFightCreatureEntity> CreateDefenseCoreCreature(CreatureBean creatureData, Vector3 creaturePos)
    {
        var targetObj = GetFightCreatureObj(creatureData.creatureId);

        targetObj.transform.position = creaturePos;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //创建生物
        FightCreatureBean fightCreatureData = gameFightLogic.fightData.fightDefenseCoreData;
        fightCreatureData.positionCreate = new Vector3Int(-1, 0, 0);

        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefenseCoreCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(gameFightCreatureEntity);
        });
        return gameFightCreatureEntity;
    }
    #endregion

    #region 防御生物
    /// <summary>
    ///  创建防御生物
    /// </summary>
    public GameObject CreateDefenseCreature(CreatureBean creatureData)
    {
        var targetObj = GetFightCreatureObj(creatureData.creatureId);

        Transform rendererTF = targetObj.transform.Find("Spine");
        SkeletonAnimation targetSkeletonAnimation = rendererTF.GetComponent<SkeletonAnimation>();
        SetCreatureData(targetSkeletonAnimation, creatureData, isSetSkeletonDataAsset: false);
        return targetObj;
    }

    /// <summary>
    ///  创建防御生物实例
    /// </summary>
    public GameFightCreatureEntity CreateDefenseCreatureEntity(CreatureBean creatureData, Vector3Int creaturePos)
    {
        var targetObj = CreateDefenseCreature(creatureData);
        return CreateDefenseCreatureEntity(targetObj, creatureData, creaturePos);
    }

    /// <summary>
    ///  创建防御生物实例
    /// </summary>
    public GameFightCreatureEntity CreateDefenseCreatureEntity(GameObject targetObj, CreatureBean creatureData, Vector3Int creaturePos)
    {
        //创建战斗生物数据
        FightCreatureBean fightCreatureData = new FightCreatureBean(creatureData);
        fightCreatureData.positionCreate = creaturePos;
        //创建战斗生物
        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);
        //先添加数据
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        gameFightLogic.fightData.AddDefenseCreatureByPos(creaturePos, gameFightCreatureEntity);
        //再创建AI
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefenseCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(gameFightCreatureEntity);
        });
        //设置位置
        targetObj.transform.position = creaturePos;
        return gameFightCreatureEntity;
    }
    #endregion

    #region  进攻生物
    /// <summary>
    /// 创建进攻生物
    /// </summary>
    public void CreateAttackCreature(FightAttackDetailsBean fightAttackDetails, int roadNum)
    {
        //一次创建的数量
        int numCreature = fightAttackDetails.npcIds.Count;
        for (int i = 0; i < numCreature; i++)
        {
            var npcId = fightAttackDetails.npcIds[i];
            CreateAttackCreature(npcId, roadNum);
        }
        UIHandler.Instance.RefreshUI();
    }

    /// <summary>
    /// 创建进攻生物
    /// </summary>
    /// <param name="targetRoad">目标进攻路线 0为随机</param>
    public GameObject CreateAttackCreature(long npcId, int roadNum, int targetRoad = 0)
    {
        var npcInfo = NpcInfoCfg.GetItemData(npcId);
        var targetObj = GetFightCreatureObj(npcInfo.creature_id);
        //随机生成某一路
        if (targetRoad == 0)
        {
            targetRoad = UnityEngine.Random.Range(1, roadNum + 1);
        }
        float randomX = UnityEngine.Random.Range(11f, 12f);
        targetObj.transform.position = new Vector3(randomX, 0, targetRoad);

        //创建战斗生物
        FightCreatureBean fightCreatureData = new FightCreatureBean(npcInfo);
        fightCreatureData.positionCreate = new Vector3Int(0, 0, targetRoad);

        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(targetObj, fightCreatureData);

        //先添加数据
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        gameLogic.fightData.AddAttackCreatureByRoad(targetRoad, gameFightCreatureEntity);

        //再创建数据
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIAttackCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(gameFightCreatureEntity);
        });
        return targetObj;
    }
    #endregion


    #region 基础
    /// <summary>
    /// 获取一个生物的obj
    /// </summary>
    public GameObject GetFightCreatureObj(long creatureId)
    {
        var targetObj = manager.LoadFightCreatureObj(creatureId);
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
            switch (creatureType)
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
        return targetObj;
    }

    /// <summary>
    /// 移除生物obj
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveFightCreatureObj(GameObject targetObj, CreatureTypeEnum creatureType)
    {
        if (targetObj == null)
            return;
        if (manager.dicPoolForCreature.TryGetValue(creatureType, out Queue<GameObject> poolForCreature))
        {
            manager.DestoryFightCreature(poolForCreature, targetObj);
        }
        else
        {
            Queue<GameObject> newPool = new Queue<GameObject>();
            manager.dicPoolForCreature.Add(creatureType, newPool);
            manager.DestoryFightCreature(newPool, targetObj);
        }
    }

    /// <summary>
    /// 移除生物实例
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveFightCreatureEntity(GameFightCreatureEntity targetEntity, CreatureTypeEnum creatureType)
    {
        if (targetEntity == null)
            return;
        //清理动画
        targetEntity.ClearAnim();
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (targetEntity.creatureObj != null)
        {
            RemoveFightCreatureObj(targetEntity.creatureObj, creatureType);
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
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureChangeState, targetEntity.fightCreatureData.creatureData.creatureUUId, CreatureStateEnum.Rest);
        }
        else if (creatureType == CreatureTypeEnum.FightAttack)
        {
            gameFightLogic.fightData.RemoveAttackCreature(targetEntity);
        }
    }
    #endregion
}
