using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 战斗游戏逻辑基类
/// <para>承载一场战斗的完整生命周期：准备(PreGame 加载场景/魔王核心) → 每帧更新(UpdateGame 选择/进攻生成/生物CD/魔王) → 结束(EndGame) → 清理(ClearGame)。</para>
/// <para>同时负责防守卡片的选择/放置/删除、魔晶拾取、结束返回基地等战斗内交互；各战斗模式(征服/终焉议会/无限/测试)通过子类重写差异逻辑。</para>
/// </summary>
public class GameFightLogic : BaseGameLogic
{
    /// <summary>当前战斗的运行时数据（生物、波次、计时器、场景尺寸等）</summary>
    public FightBean fightData;

    /// <summary>删除模式下跟随鼠标的删除标记预制（非空表示处于删除生物模式）</summary>
    public GameObject selectCreatureDestory;
    /// <summary>当前选中、跟随鼠标待放置的防守生物预制</summary>
    public GameObject selectCreature;
    /// <summary>当前选中的防守生物卡片（UI）</summary>
    public UIViewCreatureCardItem selectCreatureCard;
    /// <summary>鼠标射线命中地面后吸附到的格子坐标（放置/删除的目标位置）</summary>
    public Vector3Int selectTargetPos;

    #region  重写方法
    /// <summary>
    /// 准备游戏
    /// </summary>
    public override async void PreGame()
    {
        base.PreGame();
        //注册事件
        RegisterEvent<FightCreatureEntity>(EventsInfo.GameFightLogic_CreatureDeadEnd, EventForGameFightLogicCreatureDeadEnd);
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
        UpdateGameForDefenseCore(updateTime);
        //更新BUFF
        BuffHandler.Instance.UpdateData(updateTime);
    }

    /// <summary>
    /// 结束游戏
    /// <para>触发 GameFightLogic_EndGame 事件并附带当前战斗类型，供各系统(结算/UI/存档等)响应战斗结束。</para>
    /// </summary>
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
        //飘字粒子清理
        EffectHandler.Instance.ClearTextNumEffect();
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
        //在途弹道清理：必须在 AI 清理之前，避免已发射弹道在 AI 回收后仍命中生物触发空引用死亡逻辑
        FightHandler.Instance.manager.ClearAttackModePrefab();
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
        bool wasInDestroyMode = selectCreatureDestory != null;
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

        //退出删除模式时还原光标
        if (wasInDestroyMode)
        {
            UIHandler.Instance.SetCursorDef();
        }
    }
    #endregion

    #region 更新
    /// <summary>
    /// 更新-选中物体跟随鼠标
    /// <para>当处于放置(selectCreature)或删除(selectCreatureDestory)模式时，向地面发射鼠标射线取命中点，</para>
    /// <para>将坐标钳制在战场范围内([1, sceneRoadLength] × [1, sceneRoadNum])，更新待放置生物/删除标记/落点预览的位置，并记录到 selectTargetPos。</para>
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
    /// 更新-进攻方刷怪
    /// <para>按波次计时器累加，到达目标间隔后取下一波进攻明细：无下一波则不再生成；</para>
    /// <para>否则刷新下次生成间隔(timeNextAttack)，若该波携带 bossShowNpcIds 则弹出BOSS特写UI，最后按明细在场景道路上生成进攻生物。</para>
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
            //BOSS出现：弹出BOSS特写UI(仅BOSS首波携带 bossShowNpcIds)
            if (attackDetailsData.bossShowNpcIds != null && attackDetailsData.bossShowNpcIds.Count > 0)
            {
                ShowBossDialog(attackDetailsData.bossShowNpcIds);
            }
            CreatureHandler.Instance.CreateAttackCreature(attackDetailsData, fightData.sceneRoadNum);
        }
    }

    /// <summary>
    /// 更新-战斗生物的 BUFF 与复活CD
    /// <para>按固定间隔(timeUpdateTargetForFightCreature)批量驱动：逐个调用进攻/防守生物实体的 Update 推进其 BUFF 计时；</para>
    /// <para>并处理处于休整(Rest)状态防守生物数据的复活CD(RCD)，到达复活CD后切回待机(Idle)并触发状态改变事件。</para>
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
                    if (itemCreatureData.RCDTimeUpdate > itemCreatureData.GetAttribute(CreatureAttributeTypeEnum.RCD, true))
                    {
                        itemCreatureData.RCDTimeUpdate = 0;
                        itemCreatureData.creatureState = CreatureStateEnum.Idle;
                        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureChangeState, itemCreatureData.creatureUUId, CreatureStateEnum.Idle);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 更新-魔王（防守核心）每帧逻辑
    /// <para>魔王的所有 Update 操作统一放在此方法内。</para>
    /// <para>当前包含：魔力恢复（MP/MPF仅战斗中有效）——MPF=魔力恢复速度（每秒恢复MPF点魔力），恢复上限为魔王的魔力上限MP；每帧恢复后通知魔王预制下的MPShow刷新显示（与防守生物的LifeShow一样的通知方式）。</para>
    /// </summary>
    public void UpdateGameForDefenseCore(float updateTime)
    {
        var coreCreature = fightData.fightDefenseCoreCreature;
        if (coreCreature == null || coreCreature.IsDead())
            return;
        var coreCreatureData = coreCreature.fightCreatureData;
        //每秒恢复MPF点魔力
        float attributeMPF = coreCreatureData.GetAttribute(CreatureAttributeTypeEnum.MPF);
        if (attributeMPF > 0)
        {
            coreCreatureData.ChangeMP(attributeMPF * updateTime, out _, out _);
        }
        //通知更新魔力显示
        coreCreature.RefreshMPShow();
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
        //切换鼠标光标为删除图标
        UIHandler.Instance.SetCursorByIconName(SpriteAtlasTypeEnum.UI, "ui_dead_2", pixelScale: 5);
    }

    /// <summary>
    /// 处理删除生物(仅限于防守)
    /// </summary>
    public void SelectCreatureDestoryHandle()
    {
        var targetCreature = fightData.GetDefenseCreatureByPos(selectTargetPos);
        //如果没有这个生物 或者这个生物正在死亡 则无法处理
        if (targetCreature == null || targetCreature.IsDead())
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
        //检测魔王的魔力是否足够创建该魔物（GetAttribute(CMP)=基础CMP×(1+等级/稀有度增加倍率) 再经自身/稀有度BUFF修正后的召唤魔力消耗）
        int createMP = selectCreatureCard.cardData.creatureData.GetAttributeInt(CreatureAttributeTypeEnum.CMP);
        var coreCreature = fightData.fightDefenseCoreCreature;
        if (coreCreature != null && coreCreature.fightCreatureData.MPCurrent < createMP)
        {
            //魔力不足
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(50006));
            return;
        }
        //扣除创建消耗的魔力 并通知更新魔力显示
        if (coreCreature != null && createMP > 0)
        {
            coreCreature.fightCreatureData.ChangeMP(-createMP, out _, out _);
            coreCreature.RefreshMPShow();
        }
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
    /// <para>胜利条件：再无下一波敌人且场上无进攻生物(gameIsWin=true)；失败条件：魔王(防守核心)死亡(gameIsWin=false)。</para>
    /// <para>任一满足即切换到结算状态(Settlement)。已处于结算/结束状态时直接返回，避免重复触发 ChangeGameState。</para>
    /// </summary>
    public virtual void CheckGameEnd()
    {
        //已进入结算/结束状态时不再重复检测，防止 ChangeGameState 被反复触发
        if (gameState == GameStateEnum.Settlement || gameState == GameStateEnum.End)
            return;

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
    public void EventForGameFightLogicCreatureDeadEnd(FightCreatureEntity fightCreatureEntity)
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
            FightCreatureEntity fightCreatureEntity = fightData.GetCreatureById(targetCreatureId);
            if (fightCreatureEntity ==null )
            {
                LogUtil.LogError($"EventForBuffFightCreatureChange 没有找到生物 targetCreatureId:{targetCreatureId}");
                return;
            }
            fightCreatureEntity.fightCreatureData.RefreshBaseAttribute();
        }
    }
    #endregion

    #region 工具

    /// <summary>
    /// 弹出BOSS特写UI
    /// 在BOSS出现时展示一组BOSS的特写(UIDialogBossShow 内部会短暂放慢时间并自动关闭)
    /// </summary>
    /// <param name="bossNpcIds">本次出现的所有BOSS的npcId</param>
    protected void ShowBossDialog(List<long> bossNpcIds)
    {
        if (bossNpcIds == null || bossNpcIds.Count == 0)
            return;
        DialogBossShowBean dialogBossShowData = new DialogBossShowBean();
        dialogBossShowData.npcIds = bossNpcIds;
        UIHandler.Instance.ShowDialogBossShow(dialogBossShowData);
    }

    /// <summary>
    /// 结束当前战斗并返回基地
    /// <para>由系统界面(UIGameSystem)的"结束战斗"按钮触发，主动放弃当前战斗回到基地。</para>
    /// <para>存盘前清理深渊馈赠、还原阵容生物战斗状态，避免中间状态写入存档。</para>
    /// </summary>
    public virtual void ExitFightAndReturnToBase()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            //清理深渊馈赠数据
            BuffHandler.Instance.manager.ClearAbyssalBlessing();
            //存盘前还原阵容生物战斗状态(Fight/Rest → Idle)，避免中间状态写入存档导致阵容只剩1个
            RestoreDefenseCreatureFightState();
            //保存用户数据
            GameDataHandler.Instance.manager.SaveUserData();
            //返回基地
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }

    /// <summary>
    /// 还原我方出战阵容生物的战斗运行时状态
    /// <para>fightData.dlDefenseCreatureData 内为与玩家存档共享引用的生物 Bean，战斗中其 creatureState 会被改成 Fight/Rest 等中间状态。</para>
    /// <para>必须在战斗结束 SaveUserData() 之前调用，把状态归位到待机(Idle)，否则中间状态会被写进存档，导致回基地后阵容生物按 Idle 过滤后"只剩 1 个"。</para>
    /// </summary>
    protected void RestoreDefenseCreatureFightState()
    {
        var listDefenseCreatureData = fightData?.dlDefenseCreatureData?.List;
        if (listDefenseCreatureData == null)
            return;
        for (int i = 0; i < listDefenseCreatureData.Count; i++)
        {
            listDefenseCreatureData[i]?.ClearFightTempData();
        }
    }

    /// <summary>
    /// 通过鼠标拾取水晶
    /// </summary>
    public void PickupCrystalForMouse(float pickupDistance = 100)
    {
        RayUtil.RayToScreenPointForMousePosition(pickupDistance, 1 << LayerInfo.Drop, out bool isCollider, out RaycastHit hit);
        if (isCollider && hit.collider != null)
        {
            //手动点击拾取魔晶时播放点击音效
            AudioHandler.Instance.PlaySound(AudioEnum.sound_btn_15);
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
                //魔晶回到收集点后播放入账音效
                AudioHandler.Instance.PlaySound(AudioEnum.sound_pay_2);
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
