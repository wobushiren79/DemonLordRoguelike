using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class GameFightLogic : BaseGameLogic
{
    //战斗数据
    public FightBean fightData;

    public GameObject selectCreatureDestory;    //选择删除生物
    public GameObject selectCreature;    //选择的生物
    public UIViewCreatureCardItem selectCreatureCard;//选中生物卡片
    public Vector3Int selectTargetPos;    //选择的位置

    #region  重写方法
    /// <summary>
    /// 准备游戏
    /// </summary>
    public override async void PreGame()
    {
        base.PreGame();
        //注册事件
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_CreatureDeadEnd, EventForGameFightLogicCreatureDeadEnd);
        //发送事件通知
        RegisterEvent<string, string>(EventsInfo.Buff_FightCreatureChange, EventForBuffFightCreatureChange);

        //设置战斗场景视角
        await CameraHandler.Instance.InitFightSceneCamera();
        //设置战斗场景视角之后
        await PreGameForAfterInitFightSceneCamera();

        //加载战斗场景
        await WorldHandler.Instance.LoadFightScene(fightData);
        //加载战斗场景之后
        await PreGameForAfterLoadFightScene();

        //延迟0.1秒 防止一些镜头的1，2帧误差
        await new WaitForSeconds(0.1f);

        //加载核心（魔王）实例
        Vector3 creaturePos = new Vector3(0, 0, fightData.sceneRoadNum / 2f + 0.5f);
        var defCoreCreatureEntity = await CreatureHandler.Instance.CreateDefenseCoreCreature(fightData.fightDefenseCoreData.creatureData, creaturePos);
        //设置魔王核心
        fightData.fightDefenseCoreCreature = defCoreCreatureEntity;
        //开启战斗控制
        GameControlHandler.Instance.SetFightControl();
        //关闭LoadingUI
        var uiFightMain = UIHandler.Instance.OpenUIAndCloseOther<UIFightMain>();
        uiFightMain.InitData();
        //开始游戏
        StartGame();
    }

    /// <summary>
    /// 准备游戏-设置战斗场景视角之后
    /// </summary>
    public virtual async Task PreGameForAfterInitFightSceneCamera()
    {

    }

    /// <summary>
    /// 准备游戏-加载战斗场景之后
    /// </summary>
    public virtual async Task PreGameForAfterLoadFightScene()
    {

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
        //更新BUFF
        BuffHandler.Instance.UpdateData(updateTime);
    }

    public override void EndGame()
    {
        base.EndGame();
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_EndGame, fightData.gameFightType);
    }

    /// <summary>
    /// 清理游戏
    /// </summary>
    public override async Task ClearGame()
    {
        await base.ClearGame();
        ClearSelectData(true);
        //清理战斗数据
        fightData.ClearEntity();
        //生物清理
        CreatureHandler.Instance.manager.Clear();
        //战场清理
        FightHandler.Instance.manager.Clear();
        //AI清理
        AIHandler.Instance.manager.Clear();
        //Buff清理
        BuffHandler.Instance.manager.ClearFightCreatureBuff();
        //清理战斗场景
        await WorldHandler.Instance.UnLoadScene(GameSceneTypeEnum.Fight);
        //清理缓存
        System.GC.Collect();
    }
    #endregion

    #region 清理数据
    /// <summary>
    /// 先简单清理数据（AI和选择 防止执行）
    /// </summary>
    public void ClearGameForSimple()
    {
        ClearSelectData(true);
        //AI清理
        AIHandler.Instance.manager.Clear();
        //Buff清理
        BuffHandler.Instance.manager.ClearFightCreatureBuff();
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
                CreatureHandler.Instance.RemoveFightCreatureObj(selectCreature, CreatureFightTypeEnum.FightDefense);
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
    #endregion

    #region 更新
    /// <summary>
    /// 更新-选中物体
    /// </summary>
    public void UpdateGameForSelectCreature(float updateTime)
    {
        //如果有选中的物体
        if (selectCreature != null || selectCreatureDestory != null)
        {
            RayUtil.RayToScreenPointForMousePosition(50, 1 << LayerInfo.Ground, out bool isCollider, out RaycastHit hit);
            if (isCollider && hit.collider != null)
            {
                Vector3 hitPoint = hit.point;

                if (hitPoint.x < 1) hitPoint.x = 1;
                if (hitPoint.x > fightData.sceneRoadLength) hitPoint.x = fightData.sceneRoadLength;
                if (hitPoint.z > fightData.sceneRoadNum) hitPoint.z = fightData.sceneRoadNum;
                if (hitPoint.z < 1) hitPoint.z = 1;
                Vector3Int targetPos = Vector3Int.RoundToInt(hitPoint);
                //如果选择的生物
                if (selectCreature != null)
                {
                    selectCreature.transform.position = hitPoint;
                    GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreatureSelectPreview(selectCreatureCard.cardData.creatureData);
                    objSelectPreivew.gameObject.SetActive(true);
                    objSelectPreivew.transform.position = targetPos;
                }
                //如果选择的是删除生物
                if (selectCreatureDestory != null)
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
            CreatureHandler.Instance.CreateAttackCreature(attackDetailsData, fightData.sceneRoadNum);
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
                itemCreature.Update(fightData.timeUpdateTargetForFightCreature);
            }
            //处理防守生物的buff
            var allDefenseCreature = fightData.dlDefenseCreatureEntity;
            for (int i = 0; i < allDefenseCreature.List.Count; i++)
            {
                var itemCreature = allDefenseCreature.List[i];
                if (itemCreature == null)
                    continue;
                itemCreature.Update(fightData.timeUpdateTargetForFightCreature);
            }

            //处理CD
            var allDefenseCreatureData = fightData.dlDefenseCreatureData;
            for (int i = 0; i < allDefenseCreatureData.List.Count; i++)
            {
                var itemCreatureData = allDefenseCreatureData.List[i];
                if (itemCreatureData.creatureState == CreatureStateEnum.Rest)
                {
                    itemCreatureData.RCDTimeUpdate += fightData.timeUpdateTargetForFightCreature;
                    if (itemCreatureData.RCDTimeUpdate > itemCreatureData.GetRCD())
                    {
                        itemCreatureData.RCDTimeUpdate = 0;
                        itemCreatureData.creatureState = CreatureStateEnum.Idle;
                        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureChangeState, itemCreatureData.creatureUUId, CreatureStateEnum.Idle);
                    }
                }
            }
        }
    }
    #endregion

    #region 选择
    /// <summary>
    /// 选择了删除生物
    /// </summary>
    public void SelectCreatureDestroy()
    {
        //先取消所有选择
        ClearSelectData();
        //设置选择的预制
        selectCreatureDestory = CreatureHandler.Instance.manager.GetCreatureSelectDestroy();
        selectCreatureDestory.gameObject.SetActive(true);
    }

    /// <summary>
    /// 处理删除生物(仅限于防守)
    /// </summary>
    public void SelectCreatureDestoryHandle()
    {
        var targetCreature = fightData.GetDefenseCreatureByPos(selectTargetPos);
        if (targetCreature == null)
            return;
        CreatureHandler.Instance.RemoveFightCreatureEntity(targetCreature, CreatureFightTypeEnum.FightDefense);
    }

    /// <summary>
    /// 选择了一张防御卡
    /// </summary>
    public void SelectCard(UIViewCreatureCardItem targetView)
    {
        //先取消所有选择
        ClearSelectData();
        selectCreatureCard = targetView;
        selectCreature = CreatureHandler.Instance.CreateDefenseCreature(targetView.cardData.creatureData);
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
    public void UnSelectCreatureDestroy()
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
        //设置生物进入战斗状态
        selectCreatureCard.cardData.creatureData.creatureState = CreatureStateEnum.Fight;
        //创建战斗生物数据
        CreatureHandler.Instance.CreateDefenseCreatureEntity(selectCreature, selectCreatureCard.cardData.creatureData, selectTargetPos);
        selectCreature = null;
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_PutCard, selectCreatureCard);
        ClearSelectData();
    }
    #endregion

    #region 检测
    /// <summary>
    /// 检测游戏是否结束
    /// </summary>
    public virtual void CheckGameEnd()
    {
        bool isEnd = false;
        //如果已经没有下一波敌人 并且场上没有敌人
        if (fightData.fightAttackData.queueAttackDetails.Count == 0 && !fightData.CheckHasAttackCreature())
        {
            isEnd = true;
            fightData.gameIsWin = true;
        }
        //如果魔王死了
        if (fightData.fightDefenseCoreCreature.IsDead())
        {
            isEnd = true;
            fightData.gameIsWin = false;
        }
        //有是否结束
        if (isEnd)
        {
            //进入结算状态
            ChangeGameState(GameStateEnum.Settlement);
        }
    }
    #endregion

    #region 事件
    /// <summary>
    /// 角色死亡
    /// </summary>
    public void EventForGameFightLogicCreatureDeadEnd(FightCreatureBean fightCreature)
    {
        //检测一下游戏是否结束
        CheckGameEnd();
    }

    /// <summary>
    /// 战斗生物BUFF改变
    /// </summary>
    /// <param name="applierCreatureId">释放BUFF者</param>
    /// <param name="targetCreatureId">作用对象</param>
    public void EventForBuffFightCreatureChange(string applierCreatureId, string targetCreatureId)
    {
        //刷新一下生物属性
        if (fightData != null)
        {
            FightCreatureEntity FightCreatureEntity = fightData.GetCreatureById(targetCreatureId);
            FightCreatureEntity.fightCreatureData.RefreshBaseAttribute();
        }
    }
    #endregion

    #region 工具

    /// <summary>
    /// 通过鼠标拾取水晶
    /// </summary>
    public void PickupCrystalForMouse(float pickupDistance = 100)
    {
        RayUtil.RayToScreenPointForMousePosition(pickupDistance, 1 << LayerInfo.Drop, out bool isCollider, out RaycastHit hit);
        if (isCollider && hit.collider != null)
        {
            PickupCrystal(hit.collider.gameObject);
        }
    }

    /// <summary>
    /// 通过生物拾取水晶
    /// </summary>
    public void PickupCrystalForCreature(FightCreatureEntity fightCreatureEntity, float pickupRadius)
    {
        //如果该生物已经死亡
        if (fightCreatureEntity == null || fightCreatureEntity.IsDead())
        {
            return;
        }
        Collider[] colliders = RayUtil.OverlapToSphere(fightCreatureEntity.creatureObj.transform.position, pickupRadius, 1 << LayerInfo.Drop);
        if (!colliders.IsNull())
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                PickupCrystal(colliders[i].gameObject);
            }
        }
    }

    /// <summary>
    /// 拾取水晶
    /// </summary>
    public void PickupCrystal(GameObject targetCrystal)
    {
        //如果是正在游戏中
        if (gameState != GameStateEnum.Gaming)
            return;

        var fightDropPrefab = FightHandler.Instance.manager.GetFightPrefab(targetCrystal.name);
        if (fightDropPrefab == null)
            return;
        //设置
        fightDropPrefab.SetState(GameFightPrefabStateEnum.Droping);
        Vector3 targetPos = fightData.fightDefenseCoreCreature.creatureObj.transform.position;
        float moveSpeed = 5;
        float moveTime = Vector3.Distance(targetPos, fightDropPrefab.gameObject.transform.position) / moveSpeed;
        //播放动画
        fightDropPrefab.gameObject.transform
            .DOJump(targetPos + new Vector3(0f, 0.5f, 0.5f), UnityEngine.Random.Range(0, 0.5f), 1, moveTime)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                userData.AddCrystal(fightDropPrefab.valueInt);
                //事件通知
                EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_DropAddCrystal, fightDropPrefab.valueInt);
                //掉落删除
                fightDropPrefab.Destroy();
                //刷新所有打开的UI
                UIHandler.Instance.RefreshUI();
            });
    }
    #endregion
}
