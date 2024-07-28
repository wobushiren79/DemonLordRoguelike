using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightLogic : BaseGameLogic
{
    //战斗数据
    public FightBean fightData;

    public GameObject selectCreature;    //选择的生物
    public FightCreatureBean selectCreatureData;//选中生物卡片
    public Vector3Int selectCreaturePutPost;    //选择的生物的放置位置

    /// <summary>
    /// 准备游戏
    /// </summary>
    public override void PreGame()
    {
        base.PreGame();
        //设置战斗场景视角
        CameraHandler.Instance.SetFightSceneCamera(() =>
        {
            //加载战斗场景
            WorldHandler.Instance.LoadFightScene(fightData.fightSceneId, (targetObj) =>
            {
                //加载核心（魔王）实例
                CreatureHandler.Instance.CreateDefCoreCreature((defCoreCreatureEntity) =>
                {
                    //设置魔王核心
                    fightData.fightDefCoreCreature = defCoreCreatureEntity;
                    //开启战斗控制
                    GameControlHandler.Instance.SetFightControl();
                    //关闭LoadingUI
                    var uiFightMain = UIHandler.Instance.OpenUIAndCloseOther<UIFightMain>();
                    uiFightMain.InitData();
                    //开始游戏
                    StartGame();
                });
            });
        });
    }

    /// <summary>
    /// 注册事件
    /// </summary>
    public override void PreGameForRegisterEvent()
    {

    }

    /// <summary>
    /// 更新
    /// </summary>
    public override void UpdateGame()
    {
        base.UpdateGame();
        fightData.gameTime = fightData.gameTime + Time.deltaTime * fightData.gameSpeed;
        UpdateGameForSelectCreature();
        UpdateGameForAttCreate();
    }

    /// <summary>
    /// 更新-选中物体
    /// </summary>
    public void UpdateGameForSelectCreature()
    {
        //如果有选中的物体
        if (selectCreature != null)
        {
            RayUtil.RayToScreenPointForMousePosition(10, 1 << LayerInfo.Ground, out bool isCollider, out RaycastHit hit, CameraHandler.Instance.manager.mainCamera);
            if (isCollider && hit.collider != null)
            {
                GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreaureSelectPreview(selectCreatureData);
                objSelectPreivew.gameObject.SetActive(true);
                Vector3 hitPoint = hit.point;

                if (hitPoint.x < 1) hitPoint.x = 1;
                if (hitPoint.x > 10) hitPoint.x = 10;
                if (hitPoint.z > 6) hitPoint.z = 6;
                if (hitPoint.z < 1) hitPoint.z = 1;

                Vector3Int targetPos = Vector3Int.RoundToInt(hitPoint);
                selectCreature.transform.position = hitPoint;
                objSelectPreivew.transform.position = targetPos;
                selectCreaturePutPost = targetPos;
            }
        }
    }

    /// <summary>
    /// 更新-进攻方生成
    /// </summary>
    public void UpdateGameForAttCreate()
    {
        fightData.timeUpdateForAttCreate += (Time.deltaTime * fightData.gameSpeed);
        if (fightData.timeUpdateForAttCreate > fightData.timeUpdateTargetForAttCreate)
        {
            fightData.timeUpdateForAttCreate = 0;
            //生成一次生物
            CreatureHandler.Instance.CreateAttCreature(fightData.gameProgress, fightData.currentFightAttCreateDetails);
        }
    }

    /// <summary>
    /// 选择了一张防御卡
    /// </summary>
    public void SelectCard(FightCreatureBean fightCreature)
    {
        //如果原来没有选中
        if (selectCreatureData == null)
        {

        }
        //如果原来有选中 需要取消原来的选中物体
        else
        {
            //如果选中的数据是当前的数据 则不做处理
            if (fightCreature == selectCreatureData)
                return;
            ClearSelectData();
        }
        selectCreatureData = fightCreature;
        CreatureHandler.Instance.CreateDefCreature(fightCreature.creatureData, (targetObj) =>
        {
            selectCreature = targetObj;
        });
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_SelectCard, selectCreatureData);
    }

    /// <summary>
    /// 取消选择了一张卡
    /// </summary>
    public void UnSelectCard()
    {
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_UnSelectCard, selectCreatureData);
        ClearSelectData();
    }

    /// <summary>
    /// 放置卡片
    /// </summary>
    public void PutCard()
    {
        if (selectCreature == null)
            return;
        bool checkPosHasMainCreature = fightData.CheckFightPositionHasCreature(selectCreaturePutPost);
        if (checkPosHasMainCreature)
        {
            //已经有生物了
            return;
        }
        int createMagic = selectCreatureData.GetCreateMagic();
        if (fightData.currentMagic < createMagic)
        {
            //魔力不足
            EventHandler.Instance.TriggerEvent(EventsInfo.Toast_NoEnoughCreateMagic);
            return;
        }
        //扣除魔力
        fightData.ChangeMagic(-createMagic);
        //设置生物位置
        selectCreature.transform.position = selectCreaturePutPost;
        //创建战斗生物
        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(selectCreature, selectCreatureData);
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(gameFightCreatureEntity);
        });

        fightData.SetFightPosition(selectCreaturePutPost, gameFightCreatureEntity);
        selectCreature = null;

        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_PutCard, selectCreatureData);
        ClearSelectData();
    }

    /// <summary>
    /// 清理选择的数据
    /// </summary>
    public void ClearSelectData()
    {
        GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreaureSelectPreview();
        objSelectPreivew.gameObject.SetActive(false);
        //回收预制
        if (selectCreature != null)
        {
            CreatureHandler.Instance.RemoveCreatureObj(selectCreature, CreatureTypeEnum.FightDef);
        }
        selectCreature = null;
        selectCreatureData = null;
    }
}
