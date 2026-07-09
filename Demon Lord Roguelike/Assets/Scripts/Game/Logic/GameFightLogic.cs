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

    /// <summary>魔王自动拾取魔晶的计时器（累加 updateTime，达到间隔即触发一次拾取后扣减）</summary>
    private float demonLordPickCrystalTimer;
    /// <summary>魔王自动拾取魔晶的间隔(秒)缓存；-1 表示未解锁不拾取。战斗中研究不可变，故开战缓存一次避免每帧查表</summary>
    private float demonLordPickCrystalInterval = -1f;
    /// <summary>魔王每次自动拾取的魔晶数量缓存；同上开战缓存一次</summary>
    private int demonLordPickCrystalCount;

    #region  重写方法
    /// <summary>
    /// 准备游戏
    /// </summary>
    public override async void PreGame()
    {
        base.PreGame();
        //注册事件
        RegisterEvent<FightCreatureEntity>(EventsInfo.GameFightLogic_CreatureDeadEnd, EventForGameFightLogicCreatureDeadEnd);
        //新建防守魔物实体时按需重算全体防守属性（供动态属性馈赠"随场上魔物数量缩放"生效）
        RegisterEvent<FightCreatureEntity>(EventsInfo.GameFightLogic_DefenseCreatureCreate, EventForDefenseCreatureCreate);
        //发送事件通知
        RegisterEvent<string, string>(EventsInfo.Buff_FightCreatureChange, EventForBuffFightCreatureChange);
        //深渊馈赠变化时刷新受影响生物属性（防守核心 + 全部防守生物）
        RegisterEvent<AbyssalBlessingEntityBean>(EventsInfo.Buff_AbyssalBlessingChange, EventForAbyssalBlessingChange);

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
        //初始化战斗常量数据(开战设定、整场不变的参数统一缓存,避免每帧查表)
        InitFightConstData();
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

    #region 战斗常量数据
    /// <summary>
    /// 初始化战斗常量数据（开战设定、整场战斗恒定的参数统一在此缓存）
    /// <para>这类参数在战斗过程中不会变化（玩家战斗期间无法研究/改配置），故开战读一次缓存到字段，供每帧或热路径直接使用，避免反复查表/查字典。</para>
    /// <para>约定：后续任何"开战设定、之后恒定"的参数一律在本方法内初始化，不要散落到各处每帧读取。做成 virtual 便于各战斗模式子类追加自己的常量（override 时先调 base.InitFightConstData()）。</para>
    /// </summary>
    public virtual void InitFightConstData()
    {
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        //魔王自动拾取魔晶：拾取间隔(秒,-1=未解锁不拾取) 与 单次拾取数量，供 UpdateGameForDefenseCoreAutoPickCrystal 每帧直接用
        demonLordPickCrystalInterval = userUnlock.GetUnlockDemonLordAutoPickCrystalInterval();
        demonLordPickCrystalCount = userUnlock.GetUnlockDemonLordAutoPickCrystalCount();
        demonLordPickCrystalTimer = 0;
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
    /// 快速推进进攻进度（Quick 按钮）
    /// <para>立即向前推进「总进攻时长 * advanceRate（默认10%）」的时间，并把这段时间内本应生成的进攻波次全部立即生成；</para>
    /// <para>与逐帧刷怪同一套「累加达标即出下一波」步进语义逐波消费推进时间，保证跳跃后正常刷怪状态延续正确；</para>
    /// <para>进攻已到末尾(无剩余波次)则不再推进，进度自然封顶在 100%。返回推进后的最新进攻进度(0~1)。</para>
    /// </summary>
    /// <param name="advanceRate">本次推进占总进攻时长的比例，默认 0.1（10%）</param>
    /// <returns>推进后的最新进攻进度(0~1)</returns>
    public float QuickAdvanceAttackCreate(float advanceRate = 0.1f)
    {
        var fightAttackData = fightData.fightAttackData;
        if (fightAttackData.timeAttackTotal <= 0)
            return fightAttackData.GetAttackProgress();
        //本次要推进的时间 = 总进攻时长 * 比例
        float advanceTime = fightAttackData.timeAttackTotal * advanceRate;
        //逐波消费推进时间：直到消费完，或队列耗尽（进攻到末尾）
        while (advanceTime > 0)
        {
            //距离下一波触发还需的时间
            float needTime = fightData.timeUpdateTargetForAttackCreate - fightData.timeUpdateForAttackCreate;
            //不足以触发下一波：累加剩余时间后结束
            if (advanceTime < needTime)
            {
                fightData.timeUpdateForAttackCreate += advanceTime;
                break;
            }
            //消费到本波触发点并出下一波
            advanceTime -= needTime;
            fightData.timeUpdateForAttackCreate = 0;
            var attackDetailsData = fightAttackData.GetNextAttackDetailData();
            //没有更多波次，进攻已到末尾
            if (attackDetailsData == null)
                break;
            fightData.timeUpdateTargetForAttackCreate = attackDetailsData.timeNextAttack;
            //BOSS出现：弹出BOSS特写UI(仅BOSS首波携带 bossShowNpcIds)
            if (attackDetailsData.bossShowNpcIds != null && attackDetailsData.bossShowNpcIds.Count > 0)
                ShowBossDialog(attackDetailsData.bossShowNpcIds);
            CreatureHandler.Instance.CreateAttackCreature(attackDetailsData, fightData.sceneRoadNum);
        }
        return fightAttackData.GetAttackProgress();
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
    /// <para>当前包含：①魔力恢复（MP/MPF仅战斗中有效）——MPF=魔力恢复速度（每秒恢复MPF点魔力），恢复上限为魔王的魔力上限MP；每帧恢复后通知魔王预制下的MPShow刷新显示（与防守生物的LifeShow一样的通知方式）。②自动拾取魔晶（研究 DemonLordAutoPickCrystal 解锁后按间隔把场上魔晶吸到魔王身上）。</para>
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
        //魔王自动拾取魔晶
        UpdateGameForDefenseCoreAutoPickCrystal(updateTime);
    }

    /// <summary>
    /// 更新-魔王自动拾取魔晶
    /// <para>直接用开战缓存的间隔/数量（InitFightConstData 缓存）：未解锁(间隔<=0)直接返回；否则累加 updateTime，到达间隔即拾取一批场上魔晶并扣减一个间隔（保留超出的余量，避免高速时丢帧）。本方法每帧只做一次浮点比较+累加，无字典查询。</para>
    /// </summary>
    private void UpdateGameForDefenseCoreAutoPickCrystal(float updateTime)
    {
        //未解锁不拾取（缓存值,无每帧查表）
        if (demonLordPickCrystalInterval <= 0)
            return;
        demonLordPickCrystalTimer += updateTime;
        if (demonLordPickCrystalTimer >= demonLordPickCrystalInterval)
        {
            demonLordPickCrystalTimer -= demonLordPickCrystalInterval;
            PickupCrystalForCoreAuto(demonLordPickCrystalCount);
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
        //清除魔物时随机播放一个清扫音效
        AudioHandler.Instance.PlaySoundRandom(AudioEnum.sound_clean_1, AudioEnum.sound_clean_2);
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
        //选择魔物卡牌时播放卡片音效(音量放大 1.5 倍由配置表 volume_scale 控制)
        AudioHandler.Instance.PlaySound(AudioEnum.sound_card_1);
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
        //放置魔物特效：魔王(防守核心)处播放消耗魔力粒子
        if (coreCreature != null && coreCreature.creatureObj != null)
            EffectHandler.Instance.ShowManaEffect(coreCreature.creatureObj.transform.position);
        //放置魔物特效：生成位置播放魔物登场粒子
        EffectHandler.Instance.ShowCreatureShowEffect(selectTargetPos);
        //放置魔物成功时播放按钮音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_btn_19);
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
        //动态属性馈赠(都是兄弟/杀红了眼)：魔物死亡→场上数量减少；敌人死亡→累计击杀增加，两者都需重算全体防守魔物属性
        //先处理死亡带来的属性变化，再检测游戏是否结束(结算可能切状态)
        if (BuffHandler.Instance.HasDynamicRateAbyssalBlessing())
        {
            RefreshAllDefenseCreatureAttribute();
        }
        //检测一下游戏是否结束
        CheckGameEnd();
    }

    /// <summary>
    /// 新建防守魔物实体：动态属性馈赠"随场上魔物数量缩放"(如都是兄弟)时，重算全体防守魔物属性使已在场魔物的加成随 N 增大即时生效
    /// </summary>
    /// <param name="fightCreatureEntity">新建的防守魔物实体(仅作事件参数)</param>
    public void EventForDefenseCreatureCreate(FightCreatureEntity fightCreatureEntity)
    {
        if (BuffHandler.Instance.HasDynamicRateAbyssalBlessing())
        {
            RefreshAllDefenseCreatureAttribute();
        }
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

    /// <summary>
    /// 深渊馈赠变化（增加/升级替换）
    /// <para>属性类馈赠BUFF（含「随机一只防守魔物属性翻倍」单体定向类）只有在 RefreshBaseAttribute 时才会被算进 dicAttribute。
    /// 征服模式「普通关卡→普通关卡」保留现场不重载场景、不会自然重算属性，故此处收到馈赠变化事件后立即刷新已在场的
    /// 防守核心与全部普通防守生物，使加成当场生效（否则要等切BOSS关重载场景才生效）。</para>
    /// </summary>
    /// <param name="abyssalBlessingEntity">发生变化的深渊馈赠实例（仅作事件参数，刷新与具体哪条馈赠无关）</param>
    public void EventForAbyssalBlessingChange(AbyssalBlessingEntityBean abyssalBlessingEntity)
    {
        RefreshAllDefenseCreatureAttribute();
    }

    /// <summary>
    /// 重算防守核心 + 全部在场普通防守生物的属性(RefreshBaseAttribute)。
    /// <para>供以下场景使用：① 深渊馈赠变化(EventForAbyssalBlessingChange)；② 动态属性馈赠(都是兄弟/杀红了眼)下魔物增减、敌人击杀导致加成数值变化时即时生效。</para>
    /// </summary>
    public void RefreshAllDefenseCreatureAttribute()
    {
        if (fightData == null)
            return;
        //防守核心
        var defenseCore = fightData.fightDefenseCoreCreature;
        if (defenseCore != null && defenseCore.fightCreatureData != null && !defenseCore.IsDead())
        {
            defenseCore.fightCreatureData.RefreshBaseAttribute();
        }
        //全部普通防守生物
        var listDefenseCreatureEntity = fightData.dlDefenseCreatureEntity?.List;
        if (listDefenseCreatureEntity == null)
            return;
        for (int i = 0; i < listDefenseCreatureEntity.Count; i++)
        {
            var creatureEntity = listDefenseCreatureEntity[i];
            if (creatureEntity == null || creatureEntity.fightCreatureData == null || creatureEntity.IsDead())
                continue;
            creatureEntity.fightCreatureData.RefreshBaseAttribute();
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
            //手动点击拾取魔晶时播放点击音效(音量放大 1.5 倍由配置表 volume_scale 控制)
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
    /// 魔王自动拾取魔晶（按场上掉落顺序 FIFO 取最先掉落的若干颗）
    /// <para>不做就近计算：直接遍历 listFightPrefab（掉落物按加入顺序排列），筛出"魔晶(路径匹配)且处于可拾取状态(DropCheck)"的掉落物，取最先的 count 颗依次拾取（复用 PickupCrystal 飞回魔王入账）。</para>
    /// </summary>
    /// <param name="count">本次拾取的魔晶颗数（来自研究 DemonLordAutoPickCrystalNum，基础 1）</param>
    public void PickupCrystalForCoreAuto(int count)
    {
        if (count <= 0)
            return;
        var listFightPrefab = FightHandler.Instance.manager.listFightPrefab;
        int picked = 0;
        //按加入顺序遍历，取最先掉落且可拾取的魔晶；PickupCrystal 会置 Droping 关碰撞，故本帧不会重复命中
        for (int i = 0; i < listFightPrefab.Count && picked < count; i++)
        {
            var itemPrefab = listFightPrefab[i];
            if (itemPrefab == null || itemPrefab.gameObject == null)
                continue;
            //仅魔晶(排除掉落魔力)且处于落地可拾取状态
            if (itemPrefab.pathAsstes != FightManager.pathDropCrystalPrefab)
                continue;
            if (itemPrefab.state != GameFightPrefabStateEnum.DropCheck)
                continue;
            PickupCrystal(itemPrefab.gameObject);
            picked++;
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
                //魔晶回到收集点后入账,入账音效由 AddCrystal 内部统一播放
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
