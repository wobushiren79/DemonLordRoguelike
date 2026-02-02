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
    public void SetCreatureData(SkeletonAnimation skeletonAnimation, CreatureBean creatureData, 
        bool isSetSkeletonDataAsset = true, bool isUIShow = false, bool isNeedWeapon = true, bool isNeedEquip = true)
    {
        SetCreatureData(skeletonAnimation, null, creatureData, isSetSkeletonDataAsset, isUIShow, isNeedWeapon, isNeedEquip);
    }

    /// <summary>
    /// 设置生物数据
    /// </summary>
    public void SetCreatureData(SkeletonGraphic skeletonGraphic, CreatureBean creatureData, 
        bool isSetSkeletonDataAsset = true, bool isUIShow = false, bool isNeedWeapon = true, bool isNeedEquip = true)
    {
        SetCreatureData(null, skeletonGraphic, creatureData, isSetSkeletonDataAsset, isUIShow, isNeedWeapon, isNeedEquip);
    }

    /// <summary>
    /// 设置生物数据
    /// </summary>
    public void SetCreatureData(SkeletonAnimation skeletonAnimation, SkeletonGraphic skeletonGraphic, CreatureBean creatureData, 
    bool isSetSkeletonDataAsset = true, bool isUIShow = false, bool isNeedWeapon = true, bool isNeedEquip = true)
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
        Dictionary<string, SpineSkinBean> skinData = creatureData.GetSkinData(showType: skinType, isNeedWeapon: isNeedWeapon, isNeedEquip: isNeedEquip);
        //设置SkeletonAnimation
        if (skeletonAnimation != null)
        {
            if (isSetSkeletonDataAsset)
            {
                SpineHandler.Instance.SetSkeletonDataAsset(skeletonAnimation, resName);
            }
            //修改皮肤
            SpineHandler.Instance.ChangeSkeletonSkin(skeletonAnimation.skeleton, skinData);
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
            SpineHandler.Instance.ChangeSkeletonSkin(skeletonGraphic.Skeleton, skinData);
        }
    }

    #region 终焉议会议员
    /// <summary>
    /// 生成议会议员
    /// </summary>
    public async Task<GameObject> CreateDoomCouncilCreature(CreatureBean creatureData, Vector3 creaturePos)
    {
        var targetObj = manager.GetDoomCouncilCreatureObj();
        Transform rendererTF = targetObj.transform.Find("Spine");
        SkeletonAnimation skeletonAnimation = rendererTF.GetComponent<SkeletonAnimation>();
        SetCreatureData(skeletonAnimation, creatureData, isNeedWeapon: false);
        targetObj.name = $"Councilor_{creatureData.creatureUUId}";
        //设置议员位置
        targetObj.transform.position = creaturePos;
        //议员播放待机动画
        float animStartTime = UnityEngine.Random.Range(0f, 1f);
        var animData = SpineHandler.Instance.PlayAnim(skeletonAnimation, SpineAnimationStateEnum.Idle, creatureData, true, animStartTime: animStartTime);
        return targetObj;
    }
    #endregion

    #region  防御核心生物
    /// <summary>
    /// 生成防御核心生物
    /// </summary>
    public async Task<FightCreatureEntity> CreateDefenseCoreCreature(CreatureBean creatureData, Vector3 creaturePos)
    {
        var targetObj = GetFightCreatureObj(creatureData.creatureId, CreatureFightTypeEnum.FightDefenseCore);

        targetObj.transform.position = creaturePos;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //创建生物
        FightCreatureBean fightCreatureData = gameFightLogic.fightData.fightDefenseCoreData;
        fightCreatureData.positionCreate = new Vector3Int(0, 0, 0);

        FightCreatureEntity fightCreatureEntity = GetFightCreatureEntity(targetObj, fightCreatureData);
        fightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefenseCoreCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(fightCreatureEntity);
        });
        return fightCreatureEntity;
    }
    #endregion

    #region 防御生物
    /// <summary>
    ///  创建防御生物
    /// </summary>
    public GameObject CreateDefenseCreature(CreatureBean creatureData)
    {
        var targetObj = GetFightCreatureObj(creatureData.creatureId, CreatureFightTypeEnum.FightDefense);

        Transform rendererTF = targetObj.transform.Find("Spine");
        SkeletonAnimation targetSkeletonAnimation = rendererTF.GetComponent<SkeletonAnimation>();
        SetCreatureData(targetSkeletonAnimation, creatureData, isSetSkeletonDataAsset: false);
        return targetObj;
    }

    /// <summary>
    ///  创建防御生物实例
    /// </summary>
    public FightCreatureEntity CreateDefenseCreatureEntity(CreatureBean creatureData, Vector3Int creaturePos)
    {
        var targetObj = CreateDefenseCreature(creatureData);
        return CreateDefenseCreatureEntity(targetObj, creatureData, creaturePos);
    }

    /// <summary>
    ///  创建防御生物实例
    /// </summary>
    public FightCreatureEntity CreateDefenseCreatureEntity(GameObject targetObj, CreatureBean creatureData, Vector3Int creaturePos)
    {
        //创建战斗生物数据
        FightCreatureBean fightCreatureData = GetFightCreatureData(creatureData, CreatureFightTypeEnum.FightDefense);
        fightCreatureData.positionCreate = creaturePos;
        //创建战斗生物
        FightCreatureEntity fightCreatureEntity = GetFightCreatureEntity(targetObj, fightCreatureData);
        //先添加数据
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        gameFightLogic.fightData.AddDefenseCreatureByPos(creaturePos, fightCreatureEntity);
        //再创建AI
        fightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefenseCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(fightCreatureEntity);
        });
        //设置位置
        targetObj.transform.position = creaturePos;
        return fightCreatureEntity;
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
            float npcCreatePosX = 11.5f;
            if (fightAttackDetails.npcCreatePosX != null && i < fightAttackDetails.npcCreatePosX.Count)
            {
                npcCreatePosX = fightAttackDetails.npcCreatePosX[i];
            }
            CreateAttackCreature(npcId, roadNum, createPosX : npcCreatePosX);
        }
        UIHandler.Instance.RefreshUI();
    }

    /// <summary>
    /// 创建进攻生物
    /// </summary>
    /// <param name="targetRoad">目标进攻路线 0为随机</param>
    public GameObject CreateAttackCreature(long npcId, int roadNum, int targetRoad = 0 , float createPosX = 11.5f)
    {
        var npcInfo = NpcInfoCfg.GetItemData(npcId);
        if (npcInfo == null)
        {
            LogUtil.LogError($"CreateAttackCreature失败 没有找到npcId:{npcId})");
            return null;
        }
        var targetObj = GetFightCreatureObj(npcInfo.creature_id, CreatureFightTypeEnum.FightAttack);
        //随机生成某一路
        if (targetRoad == 0)
        {
            targetRoad = UnityEngine.Random.Range(1, roadNum + 1);
        }
        float randomX = UnityEngine.Random.Range(createPosX - 0.25f, createPosX + 0.25f);
        targetObj.transform.position = new Vector3(randomX, 0, targetRoad);

        //创建战斗生物
        FightCreatureBean fightCreatureData = GetFightCreatureData(npcInfo, CreatureFightTypeEnum.FightAttack);
        fightCreatureData.positionCreate = new Vector3Int(0, 0, targetRoad);

        FightCreatureEntity fightCreatureEntity = GetFightCreatureEntity(targetObj, fightCreatureData);
        //先添加数据
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        gameLogic.fightData.AddAttackCreatureByRoad(targetRoad, fightCreatureEntity);

        //再创建数据
        fightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIAttackCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(fightCreatureEntity);
        });
        return targetObj;
    }
    #endregion

    #region 基础
    /// <summary>
    /// 获取战斗生物数据
    /// </summary>
    public FightCreatureBean GetFightCreatureData(long id, CreatureFightTypeEnum creatureFightType)
    {   
        var creatureData = new CreatureBean(id);
        return manager.GetFightCreatureData(creatureData, creatureFightType);
    }

    /// <summary>
    /// 获取战斗生物数据
    /// </summary>
    public FightCreatureBean GetFightCreatureData(NpcInfoBean npcInfo, CreatureFightTypeEnum creatureFightType)
    {
        var creatureData = new CreatureBean(npcInfo);
        return manager.GetFightCreatureData(creatureData, creatureFightType);
    }

    /// <summary>
    /// 获取战斗生物数据
    /// </summary>
    public FightCreatureBean GetFightCreatureData(CreatureBean creatureData, CreatureFightTypeEnum creatureFightType)
    {
        return manager.GetFightCreatureData(creatureData,creatureFightType);
    }    
    /// <summary>
    /// 获取战斗生物Entity
    /// </summary>

    public FightCreatureEntity GetFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        return manager.GetFightCreatureEntity(creatureObj, fightCreatureData);
    }

    /// <summary>
    /// 获取一个生物的obj
    /// </summary>
    public GameObject GetFightCreatureObj(long creatureId, CreatureFightTypeEnum creatureFightType)
    {
        var targetObj = manager.GetFightCreatureObj(creatureId, creatureFightType);
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        //设置层级
        if (!creatureInfo.creature_layer.IsNull())
        {
            targetObj.layer = LayerMask.NameToLayer($"{creatureInfo.creature_layer}");
        }
        else
        {
            switch (creatureFightType)
            {
                case CreatureFightTypeEnum.FightAttack:
                    targetObj.layer = LayerInfo.CreatureAtt;
                    break;
                case CreatureFightTypeEnum.FightDefense:
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
    public void RemoveFightCreatureObj(GameObject targetObj, CreatureFightTypeEnum creatureType)
    {
        manager.RemoveFightCreatureObj(targetObj, creatureType);
    }

    /// <summary>
    /// 移除生物实例
    /// </summary>
    /// <param name="targetObj"></param>
    public void RemoveFightCreatureEntity(FightCreatureEntity targetEntity, CreatureFightTypeEnum creatureType)
    {
        if (targetEntity == null)
            return;
        //清理动画
        targetEntity.ClearAnim();
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //删掉对应的BUFF数据
        BuffHandler.Instance.RemoveFightCreatureBuffs(targetEntity.fightCreatureData.creatureData.creatureUUId);
        //放进缓存
        if (targetEntity.creatureObj != null)
        {
            RemoveFightCreatureObj(targetEntity.creatureObj, creatureType);
        }
        //放进缓存
        if (targetEntity.aiEntity != null)
        {
            AIHandler.Instance.RemoveAIEntity(targetEntity.aiEntity);
        }
        //如果是防守生物 还需要移除位置信息 和还原卡片
        if (creatureType == CreatureFightTypeEnum.FightDefense)
        {
            gameFightLogic.fightData.RemoveDefenseCreatureByPos(targetEntity.fightCreatureData.positionCreate);
            targetEntity.fightCreatureData.creatureData.creatureState = CreatureStateEnum.Rest;
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureChangeState, targetEntity.fightCreatureData.creatureData.creatureUUId, CreatureStateEnum.Rest);
        }
        else if (creatureType == CreatureFightTypeEnum.FightAttack)
        {
            gameFightLogic.fightData.RemoveAttackCreature(targetEntity);
        }
        //放进缓存
        if (targetEntity.fightCreatureData != null)
        {
            manager.RemoveFightCreatureData(targetEntity.fightCreatureData);
        }
        //放进缓存
        manager.RemoveFightCreatureEntity(targetEntity);
    }
    #endregion
}
