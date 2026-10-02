using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 故事演出处理器
/// 监听触发事件 -> 判定未播故事 -> 锁输入/暂停战斗 -> 接管镜头逐步执行演出 -> 恢复并记录存档
/// <para>事件注册由 LauncherGame.Launch(真实游戏入口)与 LauncherTest.StartForNormalGame(正常启动游戏)调用；StoryTest 测试场景不注册,自动触发天然关闭,测试面板直接调 PlayStory</para>
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
        EventHandler.Instance.RegisterEvent(EventsInfo.UIFightMain_CardCreateAnimEnd, EventForFightCardCreateAnimEnd);
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
    /// 战斗卡片出现动画播完回调(进战斗场景的演出就绪时机:等下方卡片弹入落位后再触发,保证高亮手卡等目标已在最终位置)
    /// </summary>
    private void EventForFightCardCreateAnimEnd()
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
        var cameraMoveTarget = BeginStoryCamera();
        manager.storyCameraOriginPos = cameraMoveTarget.position;
        try
        {
            //4.分组执行:执行组=当前步+紧随的连续并发步骤(is_async=1),同组同时发起、整组完成才进下一组
            //(并发=与上一步同时进行,如 Talk+并发CameraMove=对话打开即镜头开始移动,对话与补间都结束才继续后续步骤)
            var steps = StoryDetailsInfoCfg.GetDataByStoryId(storyData.id);
            int indexStep = 0;
            while (indexStep < steps.Count)
            {
                var listStepTasks = new List<UniTask>();
                var step = steps[indexStep];
                indexStep++;
                //进入非对话步骤的组前关闭仍开着的演出对话 UI(对话步骤间连播保持打开复用,防亮→亮切换重开闪一帧;收尾兜底在 FinishStory)
                if (step.GetStepType() != StoryStepTypeEnum.Talk)
                    CloseStoryConversationUI();
                listStepTasks.Add(ExecuteStep(step));
                //组内紧随的并发步骤同时发起(不逐个等,整组最后经 WhenAll 一并等完成)
                while (indexStep < steps.Count && steps[indexStep].IsAsync())
                {
                    listStepTasks.Add(ExecuteStep(steps[indexStep]));
                    indexStep++;
                }
                await GTask.WhenAll(listStepTasks.ToArray());
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
        //镜头归还(补间回起始位后瞬切还原,原虚拟相机参数全程未动;内部 unscaled,战斗暂停下照常)
        await EndStoryCamera();
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
        //兜底关闭演出对话 UI(对话步骤保持打开机制下,故事结束必须收口,防高亮/对话框残留)
        CloseStoryConversationUI();
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
    /// <para>镜头走 Story 专用虚拟相机(独立锚点),不再依赖 controlTargetForEmpty,锁输入隐藏它也不影响演出</para>
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
    }
    #endregion

    #region 演出步骤执行
    /// <summary>
    /// 执行单个演出步骤(按 step_type 分发;并发步骤由调用方与同组上一步同时发起,整组经 WhenAll 一并等完成)
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
            case StoryStepTypeEnum.UIHandle:
                ExecuteStepForUIHandle(stepData);
                break;
            default:
                LogUtil.LogWarning($"故事演出跳过未知步骤类型:{stepData.step_type} (步骤id:{stepData.id})");
                break;
        }
    }

    /// <summary>
    /// 对话步骤:param_1 按 &amp; 拆分多个对话ID,同一步内顺序连播(每句各等一次点击);param_2/3/4 为对话框对齐与偏移(空=默认下对齐(0,0))
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
            await PlayTalkOnce(talkData, stepData);
        }
    }

    /// <summary>
    /// 播放单句故事对话:打开对话UI(不关其它UI,保留 UIFightMain 等),按步骤配置设置对话框对齐/偏移,等玩家点击结束后关闭
    /// </summary>
    private async UniTask PlayTalkOnce(StoryTalkInfoBean talkData, StoryDetailsInfoBean stepData)
    {
        bool isTalkEnd = false;
        //实例复用:上一句对话 UI 仍打开(未关闭)时直接续用,不重走 OpenUI——OpenUI 含 HideStoryHighlight 防残留,
        //连播复用路径执行它会让高亮遮罩"隐藏一瞬再淡入",造成亮→亮切换闪烁;由非对话步骤/故事收尾统一关闭
        var uiConversation = manager.storyConversationUI != null && manager.storyConversationUI.gameObject.activeInHierarchy
            ? manager.storyConversationUI
            : UIHandler.Instance.OpenUI<UIGameConversation>();
        manager.storyConversationUI = uiConversation;
        //对话 UI 置顶(演出不关其它 UI,UIFightMain 等保持打开;复用旧实例时 sibling 位置停留创建时,可能在战斗主UI之下,置顶保证永远显示在其他UI之上)
        uiConversation.transform.SetAsLastSibling();
        //OpenUI 内已先把 ui_Content 还原默认布局,这里再覆盖为本步骤的对齐/偏移(先布局后起打字机,防首帧跳变)
        uiConversation.SetStoryContentLayout(stepData.GetTalkContentAnchor(), stepData.GetTalkContentOffset());
        //目标高亮(param_2 高亮/形状/倍率段;空=不高亮,OpenUI 已默认隐藏)
        ApplyTalkHighlight(uiConversation, stepData);
        uiConversation.SetDataForStory(null, talkData, () =>
        {
            isTalkEnd = true;
            //不在此 CloseUI:对话保持打开复用(亮→亮切换不闪),收口统一走 CloseStoryConversationUI
        });
        //等点击结束(逐帧轮询不依赖时间,战斗暂停下照常)
        await GTask.WaitUntil(() => isTalkEnd, manager.cancelForStory);
    }

    /// <summary>
    /// 关闭故事演出对话 UI(非对话步骤/故事收尾时收口;对话步骤连播期间保持打开复用,防止关闭重开闪一帧)
    /// </summary>
    private void CloseStoryConversationUI()
    {
        //魔晶引导置顶一并还原(置于 null 检查前,防对话 UI 已关闭但置顶态残留;渲染器未装配时零副作用)
        SetCrystalAlwaysOnTop(false);
        if (manager.storyConversationUI == null)
            return;
        manager.storyConversationUI.CloseUI();
        manager.storyConversationUI = null;
    }

    /// <summary>
    /// 解析对话步骤的高亮配置并设置对话框遮罩高亮(目标标记空=不高亮;范围默认取目标自身大小(UI 矩形/场景包围盒),形状/倍率按步骤配置;目标当前不存在时警告并兜底不高亮)
    /// </summary>
    private void ApplyTalkHighlight(UIGameConversation uiConversation, StoryDetailsInfoBean stepData)
    {
        string marker = stepData.GetTalkHighlightMarker();
        if (marker.IsNull())
        {
            uiConversation.HideStoryHighlight();
            //无高亮目标时同步退出魔晶引导置顶(从上一步 crystal 高亮转此步骤时还原)
            SetCrystalAlwaysOnTop(false);
            return;
        }
        int shapeType = stepData.GetTalkHighlightShape();
        float sizeScale = stepData.GetTalkHighlightScale();
        //UI 类目标(UIFightMain 上的控件,ui_fight_ 前缀)
        if (marker.StartsWith("ui_fight_", StringComparison.OrdinalIgnoreCase))
        {
            RectTransform targetRect = GetFightUIHighlightRect(marker);
            if (targetRect != null)
            {
                uiConversation.SetStoryHighlight(targetRect, shapeType, sizeScale);
                SetCrystalAlwaysOnTop(false);
                return;
            }
        }
        //场景类目标(世界包围盒→屏幕 UV)
        else if (TryGetSceneHighlightBounds(marker, out Bounds bounds))
        {
            //crystal 高亮时魔晶引导置顶:随机落点在尸体背后也能透过遮挡看到;还原由非 crystal 步骤/收尾兜底(见 SetCrystalAlwaysOnTop 调用点)
            SetCrystalAlwaysOnTop(string.Equals(marker, "crystal", StringComparison.OrdinalIgnoreCase));
            uiConversation.SetStoryHighlight(bounds, shapeType, sizeScale);
            return;
        }
        LogUtil.LogWarning($"故事演出高亮跳过,找不到目标:{marker}");
        uiConversation.HideStoryHighlight();
        SetCrystalAlwaysOnTop(false);
    }

    /// <summary>
    /// 魔晶引导置顶开关:仅"crystal"高亮目标生效——开启期间魔晶 ZTest Always 无视深度永远绘制在最前(随机落点在尸体背后也可见);
    /// 随同步骤高亮状态开启/关闭,收尾另有 CloseStoryConversationUI 统一兜底还原
    /// </summary>
    private static void SetCrystalAlwaysOnTop(bool value)
    {
        //渲染器可能未装配(如基地场景演出),接口内零副作用
        FightHandler.Instance.manager.fightDropCrystalInstanceRenderer.SetAlwaysOnTop(value);
    }

    /// <summary>
    /// 取 UIFightMain 上的高亮目标控件(仅取已打开且激活的 UIFightMain;未开战斗主UI返回 null)
    /// </summary>
    private RectTransform GetFightUIHighlightRect(string marker)
    {
        UIFightMain uiFightMain = GetOpenedFightMain();
        if (uiFightMain == null)
            return null;
        switch (marker.ToLowerInvariant())
        {
            case "ui_fight_card":
                //有手卡则高亮第一张(模板隐藏态不可见),无卡兜底模板区域
                if (uiFightMain.listCreatureCard.Count > 0 && uiFightMain.listCreatureCard[0] != null)
                    return uiFightMain.listCreatureCard[0].GetComponent<RectTransform>();
                return uiFightMain.ui_CardContent;
            case "ui_fight_remove":
                return uiFightMain.ui_BtnRemoveCreature.GetComponent<RectTransform>();
            case "ui_fight_att_progress":
                return uiFightMain.ui_UIViewFightMainAttCreateProgress.GetComponent<RectTransform>();
        }
        return null;
    }

    /// <summary>
    /// 取已打开且激活的 UIFightMain(只扫已存在 UI 列表;不用 GetUI——它找不到会自动创建,基地场景会误生成战斗主UI)
    /// </summary>
    private UIFightMain GetOpenedFightMain()
    {
        var uiList = UIHandler.Instance.manager.uiList;
        if (uiList == null)
            return null;
        for (int i = 0; i < uiList.Count; i++)
        {
            if (uiList[i] is UIFightMain fightMain && fightMain.gameObject.activeInHierarchy)
                return fightMain;
        }
        return null;
    }

    /// <summary>
    /// 取场景类高亮目标的世界包围盒(demon=魔王核心合并子 Renderer 包围盒;crystal=第一颗在屏魔晶+固定尺寸)
    /// </summary>
    private bool TryGetSceneHighlightBounds(string marker, out Bounds bounds)
    {
        bounds = default;
        switch (marker.ToLowerInvariant())
        {
            case "demon":
                var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                var coreObj = fightLogic?.fightData?.fightDefenseCoreCreature?.creatureObj;
                if (coreObj == null)
                    return false;
                bounds = GetWorldBoundsForObj(coreObj, 2f);
                return true;
            case "crystal":
                var crystalRenderer = FightHandler.Instance.manager.fightDropCrystalInstanceRenderer;
                if (crystalRenderer == null || !crystalRenderer.TryGetFirstCrystalPosition(out Vector3 crystalPos))
                    return false;
                bounds = new Bounds(crystalPos, Vector3.one * 0.6f);
                return true;
        }
        return false;
    }

    /// <summary>
    /// 取物体的世界包围盒(合并所有子 Renderer;无 Renderer 时以中心+默认尺寸兜底)
    /// </summary>
    private Bounds GetWorldBoundsForObj(GameObject obj, float defaultSize)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one * defaultSize);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    /// <summary>
    /// 镜头移动步骤:解析目标标记取世界坐标,补间演出跟随目标(param_2=时长秒默认1, param_3=缓动序号默认0)
    /// <para>基地建筑标记(core/portal/gashapon/juicer/altar/vat/achievement)镜头参数同步补间到对应 CV(同 duration/ease);无映射标记(back/self/战斗 core/council)补回演出起始参数,维持现状行为</para>
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
        float duration = stepData.GetParamFloat(2, 1f);
        int easeIndex = stepData.GetParamInt(3, 0);
        var cvCam = GetStoryMarkerCamera(marker);
        //有 CV 映射→参数补向 CV;无映射→补回起始备份;两者都与位置移动同 duration/ease 并行(两通道 DOKill 各自独立:anchor vs storyCam)
        var tweenParams = cvCam != null
            ? TweenStoryCameraParamsFromCV(cvCam, duration, easeIndex)
            : TweenStoryCameraParamsToOrigin(duration, easeIndex);
        await GTask.WhenAll(MoveStoryCamera(targetPos, duration, easeIndex), tweenParams);
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
            //isCloseOther=false:与淡出分支同理,遮罩不能关掉 UIFightMain/UIBaseMain 等场景UI(关掉后演出结束无人重开)
            UIHandler.Instance.HideMask(duration, null, () => isFadeEnd = true, isCloseOther: false);
        }
        await GTask.WaitUntil(() => isFadeEnd, manager.cancelForStory);
    }

    /// <summary>
    /// UI处理步骤:即触发即完成(param_1=要隐藏的UI名字,&amp;分隔多个;param_2=要显示的UI名字,&amp;分隔多个;先隐藏后显示,同名时显示生效)
    /// <para>显式管控演出期间 UI 显隐(如新手引导首步隐藏 UIBaseMain/末步显示);UI 显隐只由本步骤控制,淡入淡出等其它步骤均不再附带关 UI 的副作用</para>
    /// </summary>
    private void ExecuteStepForUIHandle(StoryDetailsInfoBean stepData)
    {
        var hideNames = stepData.GetUIHandleHideNames();
        for (int i = 0; i < hideNames.Length; i++)
        {
            UIHandler.Instance.CloseUI(hideNames[i]);
        }
        var showNames = stepData.GetUIHandleShowNames();
        for (int i = 0; i < showNames.Length; i++)
        {
            //按名字打开(无缓存实例时按名字加载创建);找不到预制体时 OpenUI 内部已报错,这里无需重复处理
            UIHandler.Instance.OpenUI(showNames[i]);
        }
    }
    #endregion

    #region 故事专用镜头
    /// <summary>
    /// 懒创建故事专用虚拟相机与跟随锚点(幂等;挂 Handler 下随单例常驻,初始隐藏,Priority=0 不参与渲染)
    /// </summary>
    private void EnsureStoryCamera()
    {
        if (manager.storyCamera != null)
            return;
        var objCamera = new GameObject("StoryCamera");
        objCamera.transform.SetParent(transform, false);
        var storyCam = objCamera.AddComponent<CinemachineCamera>();
        objCamera.AddComponent<CinemachineFollow>();
        objCamera.AddComponent<CinemachineRotationComposer>();
        storyCam.Priority = 0;
        var objAnchor = new GameObject("StoryCameraAnchor");
        objAnchor.transform.SetParent(transform, false);
        storyCam.Follow = objAnchor.transform;
        storyCam.LookAt = objAnchor.transform;
        objCamera.SetActive(false);
        manager.storyCamera = storyCam;
        manager.storyCameraAnchor = objAnchor.transform;
    }

    /// <summary>
    /// 演出开始接管镜头:从当前生效虚拟相机复制镜头参数(FOV/跟随偏移/阻尼/看向偏移/构图)到故事相机并备份为起始参数(manager.story*Origin,供 back/无映射标记/收尾还原),停靠原相机并瞬切;返回演出期唯一补间锚点(位置已同步,镜头不跳变)
    /// <para>原相机只改激活态,Follow/LookAt 等参数全程不动,结束由 EndStoryCamera 还原</para>
    /// </summary>
    private Transform BeginStoryCamera()
    {
        EnsureStoryCamera();
        var cameraManager = CameraHandler.Instance.manager;
        var brain = cameraManager.cinemachineBrain;
        if (brain == null)
        {
            LogUtil.LogError("故事演出接管镜头失败,主相机尚未初始化(无 CinemachineBrain)");
            return manager.storyCameraAnchor;
        }
        var srcCam = brain.ActiveVirtualCamera as CinemachineCamera;
        if (srcCam == null)
        {
            LogUtil.LogError("故事演出接管镜头失败,当前没有生效的虚拟相机");
            return manager.storyCameraAnchor;
        }
        var storyCam = manager.storyCamera;
        //1.复制镜头参数(FOV/近远裁剪/荷兰角)
        storyCam.Lens = srcCam.Lens;
        //2.复制跟随/构图参数(偏移+阻尼,保证演出镜头手感与原相机一致;新增构图参数需同步复制)
        var srcFollow = srcCam.GetComponent<CinemachineFollow>();
        var dstFollow = storyCam.GetComponent<CinemachineFollow>();
        if (srcFollow != null)
        {
            dstFollow.FollowOffset = srcFollow.FollowOffset;
            dstFollow.TrackerSettings = srcFollow.TrackerSettings;
        }
        var srcComposer = srcCam.GetComponent<CinemachineRotationComposer>();
        var dstComposer = storyCam.GetComponent<CinemachineRotationComposer>();
        if (srcComposer != null)
        {
            dstComposer.TargetOffset = srcComposer.TargetOffset;
            dstComposer.Damping = srcComposer.Damping;
            dstComposer.Composition = srcComposer.Composition;
        }
        //2.5.备份起始参数到 manager(CV 参数补间后,back/无映射标记/EndStoryCamera 以此为还原目标;struct 字段赋值即拷贝)
        manager.storyLensOrigin = storyCam.Lens;
        manager.storyFollowOffsetOrigin = dstFollow.FollowOffset;
        manager.storyTrackerSettingsOrigin = dstFollow.TrackerSettings;
        manager.storyComposerTargetOffsetOrigin = dstComposer.TargetOffset;
        manager.storyComposerDampingOrigin = dstComposer.Damping;
        manager.storyComposerCompositionOrigin = dstComposer.Composition;
        //3.锚点同步到原相机跟随目标位(无跟随目标时取相机位,镜头不跳变)
        manager.storyCameraAnchor.position = srcCam.Follow != null ? srcCam.Follow.position : srcCam.transform.position;
        //4.停靠原相机并瞬切到故事相机(混合时长缓存,结束还原;不瞬切会在战斗 timeScale=0 下混合冻结)
        manager.storyParkedCamera = srcCam;
        manager.storyBlendTimeOrigin = brain.DefaultBlend.Time;
        cameraManager.SetMainCameraDefaultBlend(0);
        srcCam.gameObject.SetActive(false);
        storyCam.gameObject.SetActive(true);
        storyCam.Priority = int.MaxValue;
        return manager.storyCameraAnchor;
    }

    /// <summary>
    /// 故事演出镜头移动:补间故事锚点到指定位置(unscaled,战斗演出 timeScale=0 下照常;先打断在途移动防并发步骤补间叠加)
    /// </summary>
    /// <param name="targetPos">目标世界坐标</param>
    /// <param name="duration">时长秒</param>
    /// <param name="easeIndex">缓动序号(0=走DOTween默认缓动,其余按 DG.Tweening.Ease 强转)</param>
    private async UniTask MoveStoryCamera(Vector3 targetPos, float duration, int easeIndex)
    {
        var anchor = manager.storyCameraAnchor;
        anchor.DOKill();
        var tween = anchor.DOMove(targetPos, duration).SetUpdate(true);
        if (easeIndex > 0 && Enum.IsDefined(typeof(Ease), easeIndex))
            tween.SetEase((Ease)easeIndex);
        //取消源传 null:结束回位必须不可取消,否则演出取消/异常时还原链会断
        await GTask.WaitTween(tween, null);
    }

    /// <summary>
    /// 补间故事相机镜头参数到指定值(与位置移动同 duration/ease 并行;FOV/跟随偏移/看向偏移走 DOTween 平滑过渡,Lens 其余字段/TrackerSettings/Damping/Composition 等非视觉连续参数在起点直接赋值)
    /// <para>补间统一 SetTarget(storyCam)+storyCam.DOKill(),与锚点位置补间(anchor.DOKill)target 不同互不干扰;SetUpdate(true) 战斗 timeScale=0 下照常</para>
    /// </summary>
    private async UniTask TweenStoryCameraParams(float duration, int easeIndex, LensSettings endLens, Vector3 endFollowOffset, Unity.Cinemachine.TargetTracking.TrackerSettings endTracker, Vector3 endTargetOffset, Vector2 endDamping, ScreenComposerSettings endComposition)
    {
        var storyCam = manager.storyCamera;
        //打断在途参数补间(防并发镜头步骤叠加;不碰 anchor 上的位置补间)
        storyCam.DOKill();
        var follow = storyCam.GetComponent<CinemachineFollow>();
        var composer = storyCam.GetComponent<CinemachineRotationComposer>();
        //1.非视觉连续参数起点直接赋值(阻尼/死区/裁剪面非画面构图,瞬切无可见跳变;Lens 是 struct 字段,读-改-写回)
        var lensNow = storyCam.Lens;
        lensNow.OrthographicSize = endLens.OrthographicSize;
        lensNow.NearClipPlane = endLens.NearClipPlane;
        lensNow.FarClipPlane = endLens.FarClipPlane;
        lensNow.Dutch = endLens.Dutch;
        storyCam.Lens = lensNow;
        follow.TrackerSettings = endTracker;
        composer.Damping = endDamping;
        composer.Composition = endComposition;
        //2.视觉连续参数 DOTween 补间(Lens 是 struct 字段,块 lambda 读-改-写回)
        var tweenFov = DOTween.To(() => storyCam.Lens.FieldOfView, v => { var l = storyCam.Lens; l.FieldOfView = v; storyCam.Lens = l; }, endLens.FieldOfView, duration)
            .SetTarget(storyCam).SetUpdate(true);
        var tweenOffset = DOTween.To(() => follow.FollowOffset, v => follow.FollowOffset = v, endFollowOffset, duration)
            .SetTarget(storyCam).SetUpdate(true);
        var tweenTarget = DOTween.To(() => composer.TargetOffset, v => composer.TargetOffset = v, endTargetOffset, duration)
            .SetTarget(storyCam).SetUpdate(true);
        //缓动与 MoveStoryCamera 同规则(0=DOTween 默认,其余按 Ease 强转)
        if (easeIndex > 0 && Enum.IsDefined(typeof(Ease), easeIndex))
        {
            var ease = (Ease)easeIndex;
            tweenFov.SetEase(ease);
            tweenOffset.SetEase(ease);
            tweenTarget.SetEase(ease);
        }
        //取消源传 null:与 MoveStoryCamera 同理,结束还原必须不可取消;被后续步骤 DOKill 时 WaitTween 因 tween 失活正常结束
        await GTask.WhenAll(GTask.WaitTween(tweenFov, null), GTask.WaitTween(tweenOffset, null), GTask.WaitTween(tweenTarget, null));
    }

    /// <summary>
    /// 补间故事相机参数到指定虚拟相机的参数(基地建筑标记专属 CV 用;CV 上组件缺失的项保持故事相机当前值不补;噪波组件不触碰,演出镜头保持无噪波)
    /// </summary>
    private UniTask TweenStoryCameraParamsFromCV(CinemachineCamera cvCam, float duration, int easeIndex)
    {
        var storyCam = manager.storyCamera;
        var follow = storyCam.GetComponent<CinemachineFollow>();
        var composer = storyCam.GetComponent<CinemachineRotationComposer>();
        var cvFollow = cvCam.GetComponent<CinemachineFollow>();
        var cvComposer = cvCam.GetComponent<CinemachineRotationComposer>();
        return TweenStoryCameraParams(duration, easeIndex,
            cvCam.Lens,
            cvFollow != null ? cvFollow.FollowOffset : follow.FollowOffset,
            cvFollow != null ? cvFollow.TrackerSettings : follow.TrackerSettings,
            cvComposer != null ? cvComposer.TargetOffset : composer.TargetOffset,
            cvComposer != null ? cvComposer.Damping : composer.Damping,
            cvComposer != null ? cvComposer.Composition : composer.Composition);
    }

    /// <summary>
    /// 补间故事相机参数回演出起始备份(back 标记/无映射标记/EndStoryCamera 收尾共用,维持"无 CV 映射即起始参数"不变量;纯旧配置下是原地补间零行为变化)
    /// </summary>
    private UniTask TweenStoryCameraParamsToOrigin(float duration, int easeIndex)
    {
        return TweenStoryCameraParams(duration, easeIndex,
            manager.storyLensOrigin,
            manager.storyFollowOffsetOrigin,
            manager.storyTrackerSettingsOrigin,
            manager.storyComposerTargetOffsetOrigin,
            manager.storyComposerDampingOrigin,
            manager.storyComposerCompositionOrigin);
    }

    /// <summary>
    /// 演出结束归还镜头:锚点与镜头参数同步补间回演出起始态后停用故事相机,恢复停靠相机与默认混合时长(姿态与起始一致,瞬切无跳变;参数不补回会停在最后一个 CV 值导致跳变)
    /// <para>锚点已在起始位(末尾并发回位步已播完或无镜头步骤)时跳过补间直接还原,避免无意义的 0.5s 停顿</para>
    /// </summary>
    private async UniTask EndStoryCamera()
    {
        //位置与参数在镜头步骤中始终同步移动,锚点已在起始位即参数也在起始态,无需再补间
        bool needRestore = (manager.storyCameraAnchor.position - manager.storyCameraOriginPos).sqrMagnitude > 0.0001f;
        if (needRestore)
        {
            await GTask.WhenAll(
                MoveStoryCamera(manager.storyCameraOriginPos, 0.5f, 0),
                TweenStoryCameraParamsToOrigin(0.5f, 0));
        }
        manager.storyCamera.gameObject.SetActive(false);
        manager.storyCamera.Priority = 0;
        if (manager.storyParkedCamera != null)
        {
            manager.storyParkedCamera.gameObject.SetActive(true);
            manager.storyParkedCamera = null;
            CameraHandler.Instance.manager.SetMainCameraDefaultBlend(manager.storyBlendTimeOrigin);
        }
    }
    #endregion

    #region 镜头目标标记解析
    /// <summary>基地建筑标记→CV_List 专属虚拟相机节点名(无映射标记:back/self/portal/council/战斗场景 core,保持现状沿用演出起始参数;与 StoryEditorWindow.CameraMarkers 保持一致)</summary>
    private static readonly Dictionary<string, string> dicMarkerToCVName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "core", "CV_Core" },
        { "gashapon", "CV_GashaponMachine" },
        { "juicer", "CV_Juicer" },
        { "altar", "CV_CreatureSacrifice" },
        { "vat", "CV_CreatureVat" },
        { "achievement", "CV_Achievement" },
    };

    /// <summary>
    /// 取镜头目标标记对应的基地专属虚拟相机(只读查找,不改激活态/优先级)
    /// <para>返回 null 的三类情况均走"补回起始参数"路径:①无映射标记(back/self/portal/council) ②战斗场景(含 core,前置短路防误读) ③有映射但 CV 节点缺失(降级为现状行为并警告)</para>
    /// </summary>
    /// <param name="marker">镜头目标标记</param>
    private CinemachineCamera GetStoryMarkerCamera(string marker)
    {
        if (marker.IsNull())
            return null;
        //战斗场景标记无 CV 映射(core=防守核心),保持现状沿用起始参数;前置短路防战斗场景误挂 CV_List 时被误读
        if (GameHandler.Instance.manager.GetGameLogic<GameFightLogic>() != null)
            return null;
        if (!dicMarkerToCVName.TryGetValue(marker, out string cvName))
            return null;
        var cvCam = CameraHandler.Instance.GetBaseSceneCamera(cvName);
        if (cvCam == null)
            LogUtil.LogWarning($"故事演出镜头参数补间跳过,标记 {marker} 映射的 {cvName} 在当前场景 CV_List 下不存在(位置移动照常,参数保持现状)");
        return cvCam;
    }

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
                //传送门/扭蛋机:取实体建筑锚点(勿用 CV 机位节点——常驻未激活,Cinemachine 不驱动其 transform,读到的是出厂陈旧坐标)
                case "portal": targetObj = scenePrefab.objBuildingPortal; break;
                case "gashapon": targetObj = scenePrefab.objBuildingGashaponMachine; break;
            }
            if (targetObj != null)
                return targetObj.transform.position;
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
