using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.Packaging;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class ControlForGameBase : BaseControl
{
    [HideInInspector]
    public InputAction inputActionMove;
    public InputAction inputActionUseE;

    //控制的生物数据
    public CreatureBean creatureData;

    protected SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation skeletonAnimation
    {
        get
        {
            if (_skeletonAnimation != null)
            {
                return _skeletonAnimation;
            }
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            _skeletonAnimation = targetMove.transform.Find("Renderer").GetComponent<SkeletonAnimation>();
            return _skeletonAnimation;
        }
    }
    public SpineAnimationStateEnum creatureAnimEnum = SpineAnimationStateEnum.None;

    //交互提示
    public GameObject controlTargetForInteraction;
    public ControlInteractionEnum currentInteractionEnum = ControlInteractionEnum.None;
    public TextMeshPro interactionText;

    //交互刷新时间
    protected float timeUpdateForInteraction;
    protected float timeMaxForInteraction = 0.2f;

    #region 空格突进(强化研究解锁)
    //空格突进输入(复用 Player 映射的 Jump 动作,已绑定键盘 Space)
    public InputAction inputActionDash;
    //角色当前朝向(水平面),突进沿此方向;默认进入基地朝上(+Z)
    protected Vector3 dashFacing = new Vector3(0, 0, 1);
    [Header("空格突进")]
    //突进耗时(秒):在此时长内快速移动完成,非瞬移
    public float dashDuration = 0.2f;
    //每级突进的世界距离,总距离=研究等级*此值(1/2/3级=1.5/3/4.5单位)
    public float dashDistancePerLevel = 1.5f;
    //每级突进对应的残影数量,总数=研究等级*此值(1级3个,3级9个)
    public int dashGhostCountPerLevel = 3;
    //突进检测障碍/边界的球形半径
    protected float dashCheckRadius = 0.1f;
    protected bool isDashing = false;      //是否正在突进
    protected float dashTimer = 0;         //突进已进行时间
    protected float dashSpeed = 0;         //突进速度=总距离/耗时
    protected float dashCdRemain = 0;      //突进冷却剩余(秒)

    //冲刺残影生成器(懒创建,挂在受控角色物体 controlTargetForCreature 上;框架层通用网格残影)
    protected AfterimageGhostMesh _dashGhost;
    /// <summary>
    /// 冲刺残影生成器(懒创建并绑定当前角色 Spine 网格，做恶魔城式半透明虚影拖尾)
    /// </summary>
    protected AfterimageGhostMesh dashGhost
    {
        get
        {
            if (_dashGhost == null)
            {
                var targetGhost = GameControlHandler.Instance.manager.controlTargetForCreature;
                _dashGhost = targetGhost.GetComponent<AfterimageGhostMesh>();
                if (_dashGhost == null)
                {
                    _dashGhost = targetGhost.AddComponent<AfterimageGhostMesh>();
                }
                //skeletonAnimation 所在物体同时挂 MeshRenderer+MeshFilter,直接按物体绑定(框架层不依赖 Spine 类型)
                _dashGhost.Init(skeletonAnimation.gameObject);
            }
            return _dashGhost;
        }
    }
    #endregion

    public void Awake()
    {
        inputActionMove = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.Move);

        inputActionUseE = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.E);
        inputActionUseE.started += HandleForUseEDown;
        inputActionUseE.canceled += HandleForUseEUp;

        //空格突进:复用 Player 映射的 Jump 动作(Space),按下发起突进
        inputActionDash = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.Jump);
        inputActionDash.started += HandleForDashDown;
    }

    public void OnDestroy()
    {
        //反订阅输入回调,避免控制物体销毁后回调悬挂
        inputActionUseE.started -= HandleForUseEDown;
        inputActionUseE.canceled -= HandleForUseEUp;
        inputActionDash.started -= HandleForDashDown;
    }

    public void Update()
    {
        HandleForInteraction();
        HandleForCameraBlock();
        HandleForDashCdUpdate();
    }

    public void FixedUpdate()
    {
        //突进期间接管移动:忽略常规 WASD 输入,直到突进结束
        if (isDashing)
        {
            HandleForDashUpdate();
            return;
        }
        HandleForMoveUpdate();
    }

    public void EnabledControl(bool enabled, bool isHideControlTarget)
    {
        base.EnabledControl(enabled);
        //切换控制状态时打断进行中的突进,并把朝向重置为默认朝上(进入/恢复基地控制)
        CancelDash();
        dashFacing = new Vector3(0, 0, 1);
        if (!enabled)
        {
            //控制被禁用（如打开界面）时停止走路声，避免残留
            AudioHandler.Instance.StopLoopSound(AudioEnum.sound_walk_1);
            //控制被禁用(打开界面/切场景经 EnableAllControl)时清空冲刺残影池:平时突进复用,此处统一销毁
            if (_dashGhost != null) _dashGhost.ClearAll();
            if (isHideControlTarget)
            {
                GameControlHandler.Instance.manager.controlTargetForCreature.SetActive(false);
            }
            else
            {
                GameControlHandler.Instance.manager.controlTargetForCreature.SetActive(true);
                //播放动画
                PlayAnimForControlTarget(SpineAnimationStateEnum.Idle);
            }
            return;
        }
        GameControlHandler.Instance.manager.controlTargetForCreature.SetActive(true);
        //播放动画
        PlayAnimForControlTarget(SpineAnimationStateEnum.Idle);
    }

    public override void EnabledControl(bool enabled)
    {
        base.EnabledControl(enabled);
        EnabledControl(enabled, true);
    }

    /// <summary>
    /// 设置生物数据
    /// </summary>
    /// <param name="creatureData"></param>
    public void SetCreatureData(CreatureBean creatureData)
    {        
        this.creatureData = creatureData;
        //展示生物数据
        CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData, isNeedWeapon : false);
    }

    /// <summary>
    /// 播放控制物体的动画
    /// </summary>
    /// <param name="animationCreatureState"></param>
    /// <param name="isLoop"></param>
    public void PlayAnimForControlTarget(SpineAnimationStateEnum animationCreatureState, bool isLoop = true, float animSpeed = 1)
    {
        if (creatureAnimEnum != animationCreatureState && skeletonAnimation != null)
        {
            SpineHandler.Instance.PlayAnim(skeletonAnimation, animationCreatureState, creatureData, isLoop, animSpeed: animSpeed);
            creatureAnimEnum = animationCreatureState;
        }
    }

    /// <summary>
    /// 移动处理
    /// </summary>
    public void HandleForMoveUpdate()
    {
        if (!enabledControl)
            return;
        Vector2 moveData = inputActionMove.ReadValue<Vector2>();
        if (moveData.x == 0 && moveData.y == 0)
        {
            //播放动画
            PlayAnimForControlTarget(SpineAnimationStateEnum.Idle);
            //停止走路声（幂等，未在播直接返回）
            AudioHandler.Instance.StopLoopSound(AudioEnum.sound_walk_1);
        }
        else
        {
                    
            float moveSpeed = creatureData.GetAttribute(CreatureAttributeTypeEnum.MSPD);
            float moveSpeedFinal = MathUtil.InterpolationLerp(moveSpeed, 0, 100, 2f, 5f);

            Vector3 targetMoveOffset = new Vector3(moveData.x * Time.deltaTime * moveSpeedFinal, 0, moveData.y * Time.deltaTime * moveSpeedFinal);
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            //检测边界
            Vector3 targetPosition = targetMove.transform.position + targetMoveOffset;
            if (!CheckSceneBoard(targetPosition))
            {
                targetMove.transform.position = targetPosition;
            }
            //记录当前朝向(供空格突进沿此方向),取最近一次移动方向
            dashFacing = new Vector3(moveData.x, 0, moveData.y).normalized;
            //按水平输入翻转精灵朝向
            SetSpriteFlipByX(moveData.x);
            //播放动画
            PlayAnimForControlTarget(SpineAnimationStateEnum.Walk,animSpeed: moveSpeedFinal * 0.8f);
            //播放走路声（连续循环，幂等去重，已在播直接返回）；pitch=1.5 加快脚步节奏(1.5 倍速)
            AudioHandler.Instance.PlayLoopSound(AudioEnum.sound_walk_1, pitch: 1.5f);

        }
    }

    //移动边界物体缓存(基地 Board 空物体,localScale 表达 BOX 长宽);懒加载,场景内固定
    protected Transform _boardBoundary;
    /// <summary>
    /// 移动边界物体(当前基地场景的 Board 空物体);首次访问时从当前场景预制取一次并缓存
    /// </summary>
    protected Transform boardBoundary
    {
        get
        {
            if (_boardBoundary != null)
            {
                return _boardBoundary;
            }
            var scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>();
            if (scenePrefab != null && scenePrefab.objBoard != null)
            {
                _boardBoundary = scenePrefab.objBoard.transform;
            }
            return _boardBoundary;
        }
    }

    /// <summary>
    /// 检测目标落点是否超出场景移动边界(基地 Board 的 BOX 范围)。
    /// 把 Board 视为单位立方体、localScale.x/z 即 BOX 长/宽:将落点转到 Board 本地空间后,
    /// |x| 或 |z| 超过 0.5(半边)即越界;经 InverseTransformPoint 天然支持 Board 平移/旋转/缩放。
    /// 无 Board 时(如尚未取到场景预制)不做限制,返回 false。
    /// </summary>
    /// <param name="endPosition">移动候选落点(世界坐标)</param>
    /// <returns>true=越界应拦截,false=在范围内可移动</returns>
    public bool CheckSceneBoard(Vector3 endPosition)
    {
        Transform board = boardBoundary;
        if (board == null)
        {
            return false;
        }
        //转到 Board 本地空间:单位立方体半边为 0.5,localScale 已由变换应用,只判水平面 x/z
        Vector3 localPos = board.InverseTransformPoint(endPosition);
        if (Mathf.Abs(localPos.x) > 0.5f || Mathf.Abs(localPos.z) > 0.5f)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 按水平输入翻转精灵朝向(x>0 朝右,x<0 朝左,x=0 保持不变)
    /// </summary>
    /// <param name="x">水平方向分量</param>
    protected void SetSpriteFlipByX(float x)
    {
        if (skeletonAnimation == null || x == 0)
        {
            return;
        }
        Vector3 sizeOriginal = skeletonAnimation.transform.localScale;
        float directionXSize = Mathf.Abs(sizeOriginal.x);
        skeletonAnimation.transform.localScale = new Vector3(x > 0 ? directionXSize : -directionXSize, sizeOriginal.y, sizeOriginal.z);
    }

    #region 空格突进

    /// <summary>
    /// 空格(Jump)按下:满足「已解锁 + 非冷却 + 非突进中」时,沿当前朝向发起一次突进。
    /// 突进距离 = 空格突进研究等级 * dashDistancePerLevel(1/2/3级=1/2/3单位),在 dashDuration 内完成,非瞬移。
    /// </summary>
    /// <param name="callback">输入回调上下文</param>
    public void HandleForDashDown(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        //已在突进中:忽略
        if (isDashing)
            return;
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        int dashLevel = userUnlock.GetUnlockSpaceDashLevel();
        //未解锁空格突进研究:不响应
        if (dashLevel <= 0)
            return;
        //冷却中:不响应
        if (dashCdRemain > 0)
            return;
        float dashDistance = dashLevel * dashDistancePerLevel;
        dashSpeed = dashDistance / dashDuration;
        dashTimer = 0;
        isDashing = true;
        //突进发起瞬间播放突进音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_knife_miss_1);
        //按研究等级刷新冷却(突进CD研究可缩短,默认3秒→最低1秒)
        dashCdRemain = userUnlock.GetUnlockSpaceDashCD();
        //突进时精灵按朝向翻转,并播放较快的移动动画
        SetSpriteFlipByX(dashFacing.x);
        PlayAnimForControlTarget(SpineAnimationStateEnum.Walk, animSpeed: 1.5f);
        //起步冲击 + 开始留残影(数量按突进等级:等级*每级数量,1级3个/3级9个)
        var dashTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        ShowDashBurstEffect(dashTarget.transform.position);
        dashGhost.StartSpawn(dashLevel * dashGhostCountPerLevel, dashDuration);
    }

    /// <summary>
    /// 突进逐帧移动(FixedUpdate 驱动):沿朝向按 dashSpeed 增量前进;
    /// 命中场景边界或障碍(Obstacle层)即提前结束,避免穿到建筑或范围外;时长到达 dashDuration 亦结束。
    /// </summary>
    public void HandleForDashUpdate()
    {
        dashTimer += Time.fixedDeltaTime;
        var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
        Vector3 targetMoveOffset = dashFacing * dashSpeed * Time.fixedDeltaTime;
        Vector3 targetPosition = targetMove.transform.position + targetMoveOffset;
        //边界或障碍阻挡:停在当前位置并结束突进(不瞬移穿越)
        if (CheckSceneBoard(targetPosition) || CheckDashObstacle(targetPosition))
        {
            EndDash();
            return;
        }
        targetMove.transform.position = targetPosition;
        //时长用尽:结束突进
        if (dashTimer >= dashDuration)
        {
            EndDash();
        }
    }

    /// <summary>
    /// 突进冷却计时(Update 驱动):每帧递减冷却剩余,归零后可再次突进
    /// </summary>
    public void HandleForDashCdUpdate()
    {
        if (dashCdRemain > 0)
        {
            dashCdRemain -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 检测突进候选落点是否被障碍(Obstacle层)阻挡
    /// </summary>
    /// <param name="targetPosition">突进候选落点</param>
    /// <returns>true=被障碍阻挡,应停止突进</returns>
    protected bool CheckDashObstacle(Vector3 targetPosition)
    {
        var allHit = RayUtil.OverlapToSphere(targetPosition, dashCheckRadius, 1 << LayerInfo.Obstacle);
        return allHit.Length > 0;
    }

    /// <summary>
    /// 正常结束突进:清状态、停止留残影并恢复站立动画
    /// </summary>
    protected void EndDash()
    {
        isDashing = false;
        dashTimer = 0;
        //停止新增残影(已生成的各自淡出);用 backing field 避免仅为停止而创建生成器
        if (_dashGhost != null) _dashGhost.StopSpawn();
        PlayAnimForControlTarget(SpineAnimationStateEnum.Idle);
    }

    /// <summary>
    /// 打断突进(仅清状态,不改动画;供控制切换等外部中断调用)
    /// </summary>
    protected void CancelDash()
    {
        isDashing = false;
        dashTimer = 0;
        if (_dashGhost != null) _dashGhost.StopSpawn();
    }

    /// <summary>
    /// 播放冲刺起步冲击特效(一次性,脚下)
    /// </summary>
    /// <param name="position">播放世界坐标</param>
    protected void ShowDashBurstEffect(Vector3 position)
    {
        EffectBean effectData = new EffectBean();
        effectData.effectName = "EffectBodySlam_1";
        effectData.effectPosition = position;
        effectData.timeForShow = 0.6f;
        effectData.isDestoryPlayEnd = false;   //回对象池复用,高频冲刺省GC
        EffectHandler.Instance.ShowEffect(effectData);
    }

    #endregion

    /// <summary>
    /// 处理交互
    /// </summary>
    public void HandleForInteraction()
    {
        if (!enabledControl)
        {
            ShowInteractionUI(false);
            return;
        }
        timeUpdateForInteraction += Time.deltaTime;
        if (timeUpdateForInteraction > timeMaxForInteraction)
        {
            timeUpdateForInteraction = 0;
            var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
            var allHit = RayUtil.OverlapToSphere(controlTarget.transform.position, 0.1f, 1 << LayerInfo.Interaction);
            if (allHit.Length > 0)
            {
                ShowInteractionUI(true, allHit[0].gameObject);
            }
            else
            {
                ShowInteractionUI(false);
            }
        }
    }

    /// <summary>
    /// 处理视线阻挡
    /// </summary>
    public void HandleForCameraBlock()
    {
        //RayUtil.RayToScreenPointForScreenCenter(100, 1 << LayerInfo.Obstacle, out bool isHit, out RaycastHit hit);
    }


    public void HandleForUseEDown(CallbackContext callback)
    {
        if (!enabledControl)
            return;
    }

    public void HandleForUseEUp(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        var allHit = RayUtil.OverlapToSphere(controlTarget.transform.position, 0.1f, 1 << LayerInfo.Interaction);
        if (allHit.Length <= 0)
            return;
        var firstHit = allHit[0];
        var interactionEnum = GetInteractionEnum(firstHit.gameObject);
        //按E命中有效互动物体时播放互动音效
        if (interactionEnum != ControlInteractionEnum.None)
        {
            AudioHandler.Instance.PlaySound(AudioEnum.sound_btn_1);
        }
        switch (interactionEnum)
        {
            case ControlInteractionEnum.CoreInteraction: //核心
                UIBaseCore baseCore = UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
                break;
            case ControlInteractionEnum.PortalInteraction://传送门
                UIBasePortal basePortal = UIHandler.Instance.OpenUIAndCloseOther<UIBasePortal>();
                break;
            case ControlInteractionEnum.DoomCouncilInteraction://终焉议会入口
                UIDoomCouncilBill doomCouncilBill = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilBill>();
                break;
            case ControlInteractionEnum.DoomCouncilPodium://终焉议会讲台
                DoomCouncilLogic doomCouncilLogic1 = GameHandler.Instance.manager.GetGameLogic<DoomCouncilLogic>();
                doomCouncilLogic1.InteractPodium();
                break;
            case ControlInteractionEnum.Councilor://终焉议会议员
                DoomCouncilLogic doomCouncilLogic2 = GameHandler.Instance.manager.GetGameLogic<DoomCouncilLogic>();
                doomCouncilLogic2.InteractCouncilor(firstHit.gameObject);
                break;
            case ControlInteractionEnum.AchievementInteraction://成就石碑
                //由场景互动打开: 退出时直接返回场景(UIBaseMain)，不再打开 UIBaseCore
                UIHandler.Instance.OpenUIAndCloseOther<UIAchievement>((ui) =>
                {
                    ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
                });
                break;
            case ControlInteractionEnum.VatInteraction://魔物进阶容器(Vat)
                //由场景互动打开: 退出时直接返回场景(UIBaseMain)，不再打开 UIBaseCore
                UIHandler.Instance.OpenUIAndCloseOther<UICreatureVat>((ui) =>
                {
                    ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
                });
                break;
        }
    }

    /// <summary>
    /// 展示互动
    /// </summary>
    public void ShowInteractionUI(bool isShow, GameObject targetInteraction = null)
    {
        if (controlTargetForInteraction == null)
        {
            var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
            controlTargetForInteraction = controlTarget.transform.Find("Interaction").gameObject;
            interactionText = controlTarget.transform.Find("Interaction/InteractionText").GetComponent<TextMeshPro>();
        }
        if (controlTargetForInteraction == null)
        {
            return;
        }
        if (isShow)
        {
            controlTargetForInteraction.SetActive(true);
            //设置显示提示文本
            if (interactionText != null)
            {
                var interactionEnum = GetInteractionEnum(targetInteraction);
                //如果当前枚举不同再设置文本 节约一点性能
                if (interactionEnum != currentInteractionEnum)
                {
                    var interactionName = GetInteractionEnumName(interactionEnum);
                    interactionText.text = interactionName;
                }
                currentInteractionEnum = interactionEnum;
            }
        }
        else
        {
            controlTargetForInteraction.SetActive(false);
        }
    }

    /// <summary>
    /// 获取交互枚举
    /// </summary>
    public ControlInteractionEnum GetInteractionEnum(GameObject targetInteraction)
    {
        if (targetInteraction == null)
            return ControlInteractionEnum.None;
        string targetName = targetInteraction.name;
        //部分交互物体名字带有 UUID 后缀(如议员 Councilor_xxx),只取下划线前的部分作为枚举名
        int underscoreIndex = targetName.IndexOf('_');
        if (underscoreIndex >= 0)
        {
            targetName = targetName.Substring(0, underscoreIndex);
        }
        //名字与枚举不匹配时回退到 None,避免 Enum.Parse 抛出 ArgumentException
        if (!System.Enum.TryParse(targetName, out ControlInteractionEnum interactionEnum))
        {
            return ControlInteractionEnum.None;
        }
        return interactionEnum;
    }

    /// <summary>
    /// 获取交互名字
    /// </summary>
    public string GetInteractionEnumName(ControlInteractionEnum interactionEnum)
    {
        long textId;
        if (interactionEnum == ControlInteractionEnum.None)
        {
            return "";
        }
        else
        {
            textId = 2000 + (int)interactionEnum;
        }
        return TextHandler.Instance.GetTextById(textId);
    }
}
