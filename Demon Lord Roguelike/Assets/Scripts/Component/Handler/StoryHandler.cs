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
            //4.逐步执行(is_async=1 并发发起即下一步;=0 阻塞等完成)
            var steps = StoryDetailsInfoCfg.GetDataByStoryId(storyData.id);
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step.IsAsync())
                {
                    //并发步骤(`_ =`)只发起不等完成,不在此关闭对话 UI(它可能仍在上一句对话中,关闭会打断演示)
                    _ = ExecuteStep(step);
                }
                else
                {
                    //进入非对话步骤前关闭仍开着的演出对话 UI(对话步骤间连播保持打开复用,防亮→亮切换重开闪一帧;收尾兜底在 FinishStory)
                    if (step.GetStepType() != StoryStepTypeEnum.Talk)
                        CloseStoryConversationUI();
                    await ExecuteStep(step);
                }
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
        await MoveStoryCamera(targetPos, stepData.GetParamFloat(2, 1f), stepData.GetParamInt(3, 0));
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
    /// 演出开始接管镜头:从当前生效虚拟相机复制镜头参数(FOV/跟随偏移/阻尼/看向偏移)到故事相机,停靠原相机并瞬切;返回演出期唯一补间锚点(位置已同步,镜头不跳变)
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
        }
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
    /// 演出结束归还镜头:锚点补间回演出起始位后停用故事相机,恢复停靠相机与默认混合时长(姿态与起始一致,瞬切无跳变)
    /// </summary>
    private async UniTask EndStoryCamera()
    {
        await MoveStoryCamera(manager.storyCameraOriginPos, 0.5f, 0);
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
