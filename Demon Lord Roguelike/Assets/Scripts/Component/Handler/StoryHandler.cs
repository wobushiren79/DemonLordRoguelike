using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 故事演出处理器
/// 监听触发事件 -> 判定未播故事 -> 锁输入/暂停战斗 -> 接管镜头逐步执行演出 -> 恢复并记录存档
/// <para>事件注册仅由 LauncherGame 调用(真实游戏入口)；测试场景不注册,自动触发天然关闭,测试面板直接调 PlayStory</para>
/// </summary>
public partial class StoryHandler : BaseHandler<StoryHandler, StoryManager>
{
    #region 生命周期
    /// <summary>
    /// 初始化(幂等):注册故事触发事件监听
    /// </summary>
    public void InitData()
    {
        if (manager.isInited)
            return;
        manager.isInited = true;
        EventHandler.Instance.RegisterEvent(EventsInfo.World_EnterGameForBaseScene, EventForEnterBaseScene);
        EventHandler.Instance.RegisterEvent(EventsInfo.GameFightLogic_StartGame, EventForFightStartGame);
        EventHandler.Instance.RegisterEvent<FightDropCrystalBean>(EventsInfo.GameFightLogic_CreatureDeadDropCrystal, EventForFightDropCrystal);
    }
    #endregion

    #region 触发事件回调
    /// <summary>
    /// 进入基地场景就绪回调
    /// </summary>
    private void EventForEnterBaseScene()
    {
        TryTriggerStory(StoryTriggerConditionEnum.EnterBaseSceneFirst);
    }

    /// <summary>
    /// 战斗开始回调(所有战斗模式 PreGame 完成进入 Gaming)
    /// </summary>
    private void EventForFightStartGame()
    {
        TryTriggerStory(StoryTriggerConditionEnum.EnterFightSceneFirst);
    }

    /// <summary>
    /// 生物死亡掉落魔晶回调(参数内容不看,仅作"首次掉晶"时机)
    /// </summary>
    private void EventForFightDropCrystal(FightDropCrystalBean fightDropCrystal)
    {
        TryTriggerStory(StoryTriggerConditionEnum.FightFirstDropCrystal);
    }
    #endregion

    #region 触发判定
    /// <summary>
    /// 按触发条件尝试播放一个未播故事(同条件取 priority 最小者;一次事件最多播一个;演出中/无存档直接丢弃)
    /// <para>高频事件(掉晶等)短路:候选列表有缓存不重复构建排序;候选全部为只播一次且已播完时标记耗尽,后续事件一次查询秒退</para>
    /// </summary>
    private void TryTriggerStory(StoryTriggerConditionEnum condition)
    {
        if (manager.isStoryPlaying)
            return;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null)
            return;
        var userStoryData = userData.GetUserStoryData();
        //切换存档槽(实例变更)后重建耗尽标记,防止旧档标记误伤新档
        if (manager.exhaustedForStoryData != userStoryData)
        {
            manager.setExhaustedCondition.Clear();
            manager.exhaustedForStoryData = userStoryData;
        }
        //该条件已无可播故事,秒退
        if (manager.setExhaustedCondition.Contains(condition))
            return;
        var matched = GetConditionStories(condition);
        bool hasPending = false;
        for (int i = 0; i < matched.Count; i++)
        {
            var story = matched[i];
            if (story.IsOnce() && userStoryData.IsStoryPlayed(story.id))
                continue;
            //还有未播(或可重复)的故事,本条件不标记耗尽
            hasPending = true;
            if (!CheckSceneMatch(story))
                continue;
            PlayStory(story.id);
            return;
        }
        //没有任何待播故事(含条件无配置),标记耗尽;场景不符的留待下次事件再判
        if (!hasPending)
            manager.setExhaustedCondition.Add(condition);
    }

    /// <summary>
    /// 获取指定触发条件的候选故事列表(带缓存:配置静态不变,避免高频事件每次重新筛选+排序)
    /// </summary>
    private List<StoryInfoBean> GetConditionStories(StoryTriggerConditionEnum condition)
    {
        if (manager.dicConditionStories == null)
            manager.dicConditionStories = new Dictionary<StoryTriggerConditionEnum, List<StoryInfoBean>>();
        if (!manager.dicConditionStories.TryGetValue(condition, out var list))
        {
            list = StoryInfoCfg.GetDataByCondition(condition);
            manager.dicConditionStories.Add(condition, list);
        }
        return list;
    }

    /// <summary>
    /// 检查故事配置的演出场景与当前场景是否匹配(防误播,如"战斗中掉晶"条件在基地误触发)
    /// </summary>
    private bool CheckSceneMatch(StoryInfoBean storyData)
    {
        switch (storyData.GetSceneType())
        {
            case StorySceneTypeEnum.Base:
                return WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.BaseGaming) != null;
            case StorySceneTypeEnum.Fight:
                return GameHandler.Instance.manager.GetGameLogic<GameFightLogic>() != null;
            case StorySceneTypeEnum.DoomCouncil:
                return GameHandler.Instance.manager.GetGameLogic<DoomCouncilLogic>() != null;
            default:
                return false;
        }
    }
    #endregion

    #region 演出播放
    /// <summary>
    /// 播放故事(入口,发射即忘;触发判定与测试面板共用)
    /// </summary>
    /// <param name="storyId">故事ID(StoryInfo.id)</param>
    public void PlayStory(long storyId)
    {
        if (manager.isStoryPlaying)
        {
            LogUtil.LogWarning($"故事演出播放中,忽略本次播放请求 storyId:{storyId}");
            return;
        }
        var storyData = StoryInfoCfg.GetItemData(storyId);
        if (storyData == null)
        {
            LogUtil.LogError($"故事演出播放失败,找不到故事配置 id:{storyId}");
            return;
        }
        _ = PlayStoryAsync(storyData);
    }

    /// <summary>
    /// 演出主流程(async UniTaskVoid 发射即忘):锁输入/暂停 -> 接管镜头 -> 逐步执行 -> 收尾(取消/异常也必走 finally 恢复状态)
    /// </summary>
    private async UniTaskVoid PlayStoryAsync(StoryInfoBean storyData)
    {
        manager.isStoryPlaying = true;
        manager.currentStoryData = storyData;
        //取消源懒创建一次复用,开始 Reset 重建令牌
        if (manager.cancelForStory == null)
            manager.cancelForStory = GTask.NewCancel(gameObject);
        manager.cancelForStory.Reset();
        bool isFight = storyData.GetSceneType() == StorySceneTypeEnum.Fight;
        //1.锁输入(基地锁控制但保持魔王可见,与议会交谈同款;战斗全禁)
        LockInputForStory(isFight);
        //2.战斗场景暂停(先例 UIGameSystem:缓存原值->0->结束还原;演出内一切等待/补间必须 unscaled)
        if (isFight)
        {
            manager.timeScaleOrigin = Time.timeScale;
            Time.timeScale = 0f;
        }
        //3.接管镜头并记录起始位(back 标记与结束归还都以此为锚)
        var cameraMoveTarget = CameraHandler.Instance.BeginStoryCameraControl(isFight);
        manager.storyCameraOriginPos = cameraMoveTarget.position;
        try
        {
            //4.逐步执行(is_async=1 并发发起即下一步;=0 阻塞等完成)
            var steps = StoryDetailsInfoCfg.GetDataByStoryId(storyData.id);
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step.IsAsync())
                    _ = ExecuteStep(step);
                else
                    await ExecuteStep(step);
            }
        }
        finally
        {
            //5.收尾:镜头归还/恢复暂停与输入/记录存档(取消或异常也必须恢复,防止演出中断后游戏卡死)
            await FinishStory(storyData, isFight);
        }
    }

    /// <summary>
    /// 演出收尾:归还镜头 -> 恢复暂停与输入 -> 记录已播存档
    /// </summary>
    private async UniTask FinishStory(StoryInfoBean storyData, bool isFight)
    {
        //镜头归还(补间回起始位后,基地恢复跟随魔王;内部 unscaled,战斗暂停下照常)
        await CameraHandler.Instance.EndStoryCameraControl(manager.storyCameraOriginPos, 0.5f);
        //恢复暂停与输入
        if (isFight)
        {
            Time.timeScale = manager.timeScaleOrigin;
            GameControlHandler.Instance.SetFightControl();
        }
        else
        {
            GameControlHandler.Instance.SetBaseControl();
        }
        //记录存档(is_once 才记;isTestSimulation 时 SaveUserData 被 GameDataManager 拦截不落盘)
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData != null && storyData.IsOnce())
        {
            var userStoryData = userData.GetUserStoryData();
            if (!userStoryData.IsStoryPlayed(storyData.id))
            {
                userStoryData.MarkStoryPlayed(storyData.id);
                GameDataHandler.Instance.manager.SaveUserData();
            }
        }
        manager.isStoryPlaying = false;
        manager.currentStoryData = null;
    }

    /// <summary>
    /// 演出锁输入:基地锁控制但保持魔王可见(与议会交谈同款);战斗全禁
    /// 锁后重新激活镜头跟随目标 controlTargetForEmpty(EnableAllControl 会隐藏它,而演出镜头移动依赖 Cinemachine 跟随)
    /// </summary>
    private void LockInputForStory(bool isFight)
    {
        if (isFight)
        {
            GameControlHandler.Instance.manager.EnableAllControl(false);
        }
        else
        {
            GameControlHandler.Instance.SetBaseControl(false, isHideControlTarget: false);
        }
        GameControlHandler.Instance.manager.controlTargetForEmpty.SetActive(true);
    }
    #endregion

    #region 演出步骤执行
    /// <summary>
    /// 执行单个演出步骤(按 step_type 分发;并发步骤由调用方发射即忘,不等返回)
    /// </summary>
    private async UniTask ExecuteStep(StoryDetailsInfoBean stepData)
    {
        switch (stepData.GetStepType())
        {
            case StoryStepTypeEnum.Talk:
                await ExecuteStepForTalk(stepData);
                break;
            case StoryStepTypeEnum.CameraMove:
                await ExecuteStepForCameraMove(stepData);
                break;
            case StoryStepTypeEnum.Wait:
                //实时等待,战斗演出 timeScale=0 下照常
                await GTask.WaitReal(stepData.GetParamFloat(1, 0f), manager.cancelForStory);
                break;
            case StoryStepTypeEnum.Effect:
                ExecuteStepForEffect(stepData);
                break;
            case StoryStepTypeEnum.Audio:
                ExecuteStepForAudio(stepData);
                break;
            case StoryStepTypeEnum.Fade:
                await ExecuteStepForFade(stepData);
                break;
            default:
                LogUtil.LogWarning($"故事演出跳过未知步骤类型:{stepData.step_type} (步骤id:{stepData.id})");
                break;
        }
    }

    /// <summary>
    /// 对话步骤:param_1 按 &amp; 拆分多个对话ID,同一步内顺序连播(每句各等一次点击)
    /// </summary>
    private async UniTask ExecuteStepForTalk(StoryDetailsInfoBean stepData)
    {
        var talkIds = stepData.GetTalkIds();
        for (int i = 0; i < talkIds.Length; i++)
        {
            var talkData = StoryTalkInfoCfg.GetItemData(talkIds[i]);
            if (talkData == null)
            {
                LogUtil.LogError($"故事演出对话步骤跳过,找不到对话配置 id:{talkIds[i]} (步骤id:{stepData.id})");
                continue;
            }
            await PlayTalkOnce(talkData);
        }
    }

    /// <summary>
    /// 播放单句故事对话:打开对话UI(不关其它UI,保留 UIFightMain 等),等玩家点击结束后关闭
    /// </summary>
    private async UniTask PlayTalkOnce(StoryTalkInfoBean talkData)
    {
        bool isTalkEnd = false;
        var uiConversation = UIHandler.Instance.OpenUI<UIGameConversation>();
        uiConversation.SetDataForStory(null, talkData, () =>
        {
            isTalkEnd = true;
            uiConversation.CloseUI();
        });
        //等点击结束(逐帧轮询不依赖时间,战斗暂停下照常)
        await GTask.WaitUntil(() => isTalkEnd, manager.cancelForStory);
    }

    /// <summary>
    /// 镜头移动步骤:解析目标标记取世界坐标,补间演出跟随目标(param_2=时长秒默认1, param_3=缓动序号默认0)
    /// </summary>
    private async UniTask ExecuteStepForCameraMove(StoryDetailsInfoBean stepData)
    {
        string marker = stepData.GetParam(1);
        Vector3 targetPos = GetStoryMarkerPosition(marker, out bool isValid);
        if (!isValid)
        {
            LogUtil.LogError($"故事演出镜头步骤跳过,无法解析目标标记:{marker} (步骤id:{stepData.id})");
            return;
        }
        await CameraHandler.Instance.MoveStoryCameraTarget(targetPos, stepData.GetParamFloat(2, 1f), stepData.GetParamInt(3, 0));
    }

    /// <summary>
    /// 特效步骤:即触发即完成(param_1=特效ID, param_2=目标标记空=战斗防守核心/基地魔王位, param_3=尺寸倍率默认1)
    /// </summary>
    private void ExecuteStepForEffect(StoryDetailsInfoBean stepData)
    {
        long effectId = stepData.GetParamLong(1, 0);
        if (effectId == 0)
        {
            LogUtil.LogError($"故事演出特效步骤跳过,特效ID为空或非法 (步骤id:{stepData.id})");
            return;
        }
        string marker = stepData.GetParam(2);
        Vector3 targetPos;
        if (marker.IsNull())
        {
            targetPos = GetStoryDefaultPosition();
        }
        else
        {
            targetPos = GetStoryMarkerPosition(marker, out bool isValid);
            if (!isValid)
            {
                LogUtil.LogError($"故事演出特效步骤跳过,无法解析目标标记:{marker} (步骤id:{stepData.id})");
                return;
            }
        }
        EffectHandler.Instance.ShowEffect(effectId, targetPos, Direction2DEnum.None, stepData.GetParamFloat(3, 1f));
    }

    /// <summary>
    /// 音效步骤:即触发即完成(param_1=音效ID,按 AudioInfoCfg 查表播放)
    /// </summary>
    private void ExecuteStepForAudio(StoryDetailsInfoBean stepData)
    {
        long audioId = stepData.GetParamLong(1, 0);
        if (audioId == 0)
        {
            LogUtil.LogError($"故事演出音效步骤跳过,音效ID为空或非法 (步骤id:{stepData.id})");
            return;
        }
        AudioHandler.Instance.PlaySoundOnce((AudioEnum)audioId);
    }

    /// <summary>
    /// 淡入淡出步骤:param_1=out 淡出变黑/in 淡入(复用 UICommonMask,内部补间 unscaled),param_2=时长秒默认0.5
    /// </summary>
    private async UniTask ExecuteStepForFade(StoryDetailsInfoBean stepData)
    {
        string direction = stepData.GetParam(1);
        float duration = stepData.GetParamFloat(2, 0.5f);
        bool isFadeEnd = false;
        if (string.Equals(direction, "out", StringComparison.OrdinalIgnoreCase))
        {
            //isCloseOther=false:遮罩不能关掉 UIFightMain/UIBaseMain 等场景UI
            UIHandler.Instance.ShowMask(duration, null, () => isFadeEnd = true, isCloseOther: false);
        }
        else
        {
            UIHandler.Instance.HideMask(duration, null, () => isFadeEnd = true);
        }
        await GTask.WaitUntil(() => isFadeEnd, manager.cancelForStory);
    }
    #endregion

    #region 镜头目标标记解析
    /// <summary>
    /// 解析镜头目标标记为世界坐标(通用:back=演出起始位;战斗:core=防守核心;基地:self=魔王/core/portal/gashapon/juicer/altar/vat/achievement/council;未知标记 isValid=false)
    /// </summary>
    private Vector3 GetStoryMarkerPosition(string marker, out bool isValid)
    {
        isValid = true;
        if (string.Equals(marker, "back", StringComparison.OrdinalIgnoreCase))
            return manager.storyCameraOriginPos;
        //战斗场景标记:core=防守核心(魔王核心)
        var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (fightLogic != null)
        {
            if (string.Equals(marker, "core", StringComparison.OrdinalIgnoreCase) && fightLogic.fightData?.fightDefenseCoreCreature != null)
                return fightLogic.fightData.fightDefenseCoreCreature.creatureObj.transform.position;
            isValid = false;
            return Vector3.zero;
        }
        //基地标记:self=魔王本体
        if (string.Equals(marker, "self", StringComparison.OrdinalIgnoreCase))
        {
            var creatureTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
            if (creatureTarget != null)
                return creatureTarget.transform.position;
            isValid = false;
            return Vector3.zero;
        }
        //基地建筑标记:取 ScenePrefabForBase 建筑物体
        var baseScene = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.BaseGaming);
        var scenePrefab = baseScene != null ? baseScene.GetComponent<ScenePrefabForBase>() : null;
        if (scenePrefab != null)
        {
            GameObject targetObj = null;
            switch (marker?.ToLower())
            {
                case "core": targetObj = scenePrefab.objBuildingCore; break;
                case "altar": targetObj = scenePrefab.objBuildingAltar; break;
                case "vat": targetObj = scenePrefab.objBuildingVat; break;
                case "council": targetObj = scenePrefab.objBuildingjDoomCouncil; break;
                case "achievement": targetObj = scenePrefab.objBuildingAchievement; break;
                case "juicer": targetObj = scenePrefab.objBuildingJuicer; break;
            }
            if (targetObj != null)
                return targetObj.transform.position;
        }
        //传送门/扭蛋机无场景建筑物体,取对应固定机位 CV 节点位置作为近似锚点
        if (string.Equals(marker, "portal", StringComparison.OrdinalIgnoreCase) || string.Equals(marker, "gashapon", StringComparison.OrdinalIgnoreCase))
        {
            string cvName = marker.ToLower() == "portal" ? "CV_Portal" : "CV_GashaponMachine";
            var cv = CameraHandler.Instance.GetBaseSceneCamera(cvName);
            if (cv != null)
                return cv.transform.position;
        }
        isValid = false;
        return Vector3.zero;
    }

    /// <summary>
    /// 演出缺省位置:战斗取防守核心,基地取魔王本体,兜底取演出起始镜头位
    /// </summary>
    private Vector3 GetStoryDefaultPosition()
    {
        var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (fightLogic?.fightData?.fightDefenseCoreCreature != null)
            return fightLogic.fightData.fightDefenseCoreCreature.creatureObj.transform.position;
        var creatureTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        if (creatureTarget != null)
            return creatureTarget.transform.position;
        return manager.storyCameraOriginPos;
    }
    #endregion
}
