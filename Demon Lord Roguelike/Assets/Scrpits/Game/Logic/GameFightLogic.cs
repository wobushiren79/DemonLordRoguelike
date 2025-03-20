using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightLogic : BaseGameLogic
{
    //战斗数据
    public FightBean fightData;

    public GameObject selectCreatureDestory;    //选择删除生物
    public GameObject selectCreature;    //选择的生物
    public UIViewCreatureCardItem selectCreatureCard;//选中生物卡片
    public Vector3Int selectTargetPos;    //选择的位置

    /// <summary>
    /// 准备游戏
    /// </summary>
    public override void PreGame()
    {
        base.PreGame();
        //注册事件
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_CreatureDeadEnd, EventForGameFightLogicCreatureDeadEnd);
        //设置战斗场景视角
        CameraHandler.Instance.InitFightSceneCamera(() =>
        {
            //加载战斗场景
            WorldHandler.Instance.LoadFightScene(fightData, async (targetObj) =>
            {
                //延迟0.1秒 防止一些镜头的1，2帧误差
                await new WaitForSeconds(0.1f);
                //加载核心（魔王）实例
                CreatureHandler.Instance.CreateDefCoreCreature(fightData.fightDefenseCoreData.creatureData, (defCoreCreatureEntity) =>
                {
                    //设置魔王核心
                    fightData.fightDefenseCoreCreature = defCoreCreatureEntity;
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
    /// 更新
    /// </summary>
    public override void UpdateGame()
    {
        base.UpdateGame();
        float updateTime = Time.deltaTime * fightData.gameSpeed;
        fightData.gameTime = fightData.gameTime + updateTime;
        UpdateGameForSelectCreature(updateTime);
        UpdateGameForAttackCreate(updateTime);
        UpdateGameForFightCreature(updateTime);
    }

    /// <summary>
    /// 清理游戏
    /// </summary>
    public override void ClearGame()
    {
        base.ClearGame();
        ClearSelectData(true);
        //清理战斗数据
        fightData.Clear();
        //生物清理
        CreatureHandler.Instance.manager.Clear();
        //战场清理
        FightHandler.Instance.manager.Clear();
        //AI清理
        AIHandler.Instance.manager.Clear();
        //清理战斗场景
        WorldHandler.Instance.UnLoadFightScene();
    }

    /// <summary>
    /// 更新-选中物体
    /// </summary>
    public void UpdateGameForSelectCreature(float updateTime)
    {
        //如果有选中的物体
        if (selectCreature != null || selectCreatureDestory != null)
        {
            RayUtil.RayToScreenPointForMousePosition(50, 1 << LayerInfo.Ground, out bool isCollider, out RaycastHit hit, CameraHandler.Instance.manager.mainCamera);
            if (isCollider && hit.collider != null)
            {                
                Vector3 hitPoint = hit.point;
                
                if (hitPoint.x < 1) hitPoint.x = 1;
                if (hitPoint.x > fightData.sceneRoadLength) hitPoint.x = fightData.sceneRoadLength;
                if (hitPoint.z > fightData.sceneRoadNum) hitPoint.z = fightData.sceneRoadNum;
                if (hitPoint.z < 1) hitPoint.z = 1;
                Vector3Int targetPos = Vector3Int.RoundToInt(hitPoint);
                //如果选择的生物
                if(selectCreature != null)
                {                
                    selectCreature.transform.position = hitPoint;
                    GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreatureSelectPreview(selectCreatureCard.cardData.creatureData);
                    objSelectPreivew.gameObject.SetActive(true);
                    objSelectPreivew.transform.position = targetPos;
                }
                //如果选择的是删除生物
                if(selectCreatureDestory != null)
                {
                    selectCreatureDestory.transform.position = targetPos;
                }
                selectTargetPos = targetPos;
            }
        }
    }

    /// <summary>
    /// 更新-进攻方生成
    /// </summary>
    public void UpdateGameForAttackCreate(float updateTime)
    {
        fightData.timeUpdateForAttackCreate += updateTime;
        if (fightData.timeUpdateForAttackCreate > fightData.timeUpdateTargetForAttackCreate)
        {
            fightData.timeUpdateForAttackCreate = 0;
            //生成一次生物
            var attackDetailsData = fightData.fightAttackData.GetNextAttackDetailData();
            if (attackDetailsData == null)
            {
                return;
            }
            fightData.timeUpdateTargetForAttackCreate = attackDetailsData.timeNextAttack;
            CreatureHandler.Instance.CreateAttackCreature(attackDetailsData);
        }
    }

    /// <summary>
    /// 更新-战斗的生物 buff cd时间
    /// </summary>
    public void UpdateGameForFightCreature(float updateTime)
    {
        fightData.timeUpdateForFightCreature += updateTime;
        if (fightData.timeUpdateForFightCreature > fightData.timeUpdateTargetForFightCreature)
        {
            fightData.timeUpdateForFightCreature = 0;
            var allAttackCreature = fightData.dlAttackCreatureEntity;
            //处理进攻生物的buff
            for (int i = 0; i < allAttackCreature.List.Count; i++)
            {
                var itemCreature = allAttackCreature.List[i];
                if (itemCreature == null)
                    continue;
                itemCreature.Update(updateTime);
            }
            //处理防守生物的buff
            var allDefenseCreature = fightData.dlDefenseCreatureEntity;
            for (int i = 0; i < allDefenseCreature.List.Count; i++)
            {
                var itemCreature = allDefenseCreature.List[i];
                if (itemCreature == null)
                    continue;
                itemCreature.Update(updateTime);
            }

            //处理CD
            var allDefenseCreatureData = fightData.dlDefenseCreatureData;
            for (int i = 0; i < allDefenseCreatureData.List.Count; i++)
            {
                var itemCreatureData = allDefenseCreatureData.List[i];
                if (itemCreatureData.creatureState == CreatureStateEnum.Rest)
                {
                    itemCreatureData.creatureStateTimeUpdate += fightData.timeUpdateTargetForFightCreature;
                    if (itemCreatureData.creatureStateTimeUpdate > itemCreatureData.creatureInfo.create_cd)
                    {
                        itemCreatureData.creatureStateTimeUpdate = 0;
                        itemCreatureData.creatureState = CreatureStateEnum.Idle;
                        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureChangeState, itemCreatureData.creatureId,CreatureStateEnum.Idle);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 选择了删除生物
    /// </summary>
    public void SelectCreatureDestory()
    {
        //先取消所有选择
        ClearSelectData();
        //设置选择的预制
        selectCreatureDestory = CreatureHandler.Instance.manager.GetCreatureSelectDestory();
        selectCreatureDestory.gameObject.SetActive(true);
    }

    /// <summary>
    /// 处理删除生物(仅限于防守)
    /// </summary>
    public void SelectCreatureDestoryHandle()
    {
        var targetCreature = fightData.GetDefenseCreatureByPos(selectTargetPos);
        if(targetCreature == null)
            return;
        CreatureHandler.Instance.RemoveCreatureEntity(targetCreature, CreatureTypeEnum.FightDefense);
    }

    /// <summary>
    /// 选择了一张防御卡
    /// </summary>
    public void SelectCard(UIViewCreatureCardItem targetView)
    {
        //先取消所有选择
        ClearSelectData();
        selectCreatureCard = targetView;
        CreatureHandler.Instance.CreateDefCreature(targetView.cardData.creatureData, (targetObj) =>
        {
            selectCreature = targetObj;
        });
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_SelectCard, selectCreatureCard);
    }

    /// <summary>
    /// 取消选择了一张卡
    /// </summary>
    public void UnSelectCard()
    {
        ClearSelectData();
    }

    /// <summary>
    /// 取消选择删除生物
    /// </summary>
    public void UnSelectCreatureDestory()
    {
        ClearSelectData();
    }

    /// <summary>
    /// 放置卡片
    /// </summary>
    public void PutCard()
    {
        if (selectCreature == null)
            return;
        bool checkPosHasCreature = fightData.CheckDefenseCreatureByPos(selectTargetPos);
        if (checkPosHasCreature)
        {
            //已经有生物了
            return;
        }
        // int createMagic = selectCreatureCard.cardData.creatureData.GetCreateMagic();
        // if (fightData.currentMagic < createMagic)
        // {
        //     //魔力不足
        //     EventHandler.Instance.TriggerEvent(EventsInfo.Toast_NoEnoughCreateMagic);
        //     return;
        // }
        //扣除魔力
        //fightData.ChangeMagic(-createMagic);

        //设置生物位置
        selectCreature.transform.position = selectTargetPos;

        //设置生物进入战斗状态
        selectCreatureCard.cardData.creatureData.creatureState = CreatureStateEnum.Fight;

        //创建战斗生物数据
        FightCreatureBean fightCreatureData = new FightCreatureBean(selectCreatureCard.cardData.creatureData);
        fightCreatureData.positionCreate = selectTargetPos;

        //创建战斗生物
        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(selectCreature, fightCreatureData);
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(gameFightCreatureEntity);
        });

        fightData.AddDefenseCreatureByPos(selectTargetPos, gameFightCreatureEntity);
        selectCreature = null;

        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_PutCard, selectCreatureCard);
        ClearSelectData();
    }

    /// <summary>
    /// 清理选择的数据
    /// </summary>
    public void ClearSelectData(bool isDestroyImm = false)
    {
        GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreatureSelectPreview();
        objSelectPreivew.gameObject.SetActive(false);
        //回收生物预制
        if (selectCreature != null)
        {
            if (isDestroyImm)
            {
                GameObject.DestroyImmediate(selectCreature);
            }
            else
            {
                CreatureHandler.Instance.RemoveCreatureObj(selectCreature, CreatureTypeEnum.FightDefense);
            }
        }
        if (selectCreatureCard != null)
        {
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_UnSelectCard, selectCreatureCard);
        }
        if (selectCreatureDestory != null)
        {
            selectCreatureDestory.gameObject.SetActive(false);
        }

        selectCreature = null;
        selectCreatureDestory = null;
        selectCreatureCard = null;
    }

    /// <summary>
    /// 检测游戏是否结束
    /// </summary>
    public void CheckGameEnd()
    {
        //如果已经没有下一波敌人 并且场上没有敌人
        if (fightData.fightAttackData.queueAttackDetails.Count == 0 && !fightData.CheckHasAttackCreature())
        {
            //进入结算状态
            GameHandler.Instance.manager.SetGameState(GameStateEnum.Settlement);
            //打开结算UI
            var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
            uiFightSettlement.SetData(fightData.fightRecordsData);
        }
    }

    /// <summary>
    /// 角色死亡
    /// </summary>
    public void EventForGameFightLogicCreatureDeadEnd(FightCreatureBean fightCreature)
    {
        //检测一下游戏是否结束
        CheckGameEnd();
    }
}
