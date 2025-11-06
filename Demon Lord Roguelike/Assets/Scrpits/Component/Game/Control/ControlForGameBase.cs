using OfficeOpenXml.Packaging;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class ControlForGameBase : BaseControl
{
    [HideInInspector]
    public InputAction inputActionMove;
    public InputAction inputActionUseE;

    [Header("角色移动速度")]
    public float speedForCreatureMoveX = 2f;
    public float speedForCreatureMoveZ = 2f;

    public SkeletonAnimation controlTargetForCreatureSkeletonAnimation;
    public SpineAnimationStateEnum controlTargetForCreatureAnim = SpineAnimationStateEnum.None;

    //交互提示
    public GameObject controlTargetForInteraction;

    //交互刷新时间
    protected float timeUpdateForInteraction;
    protected float timeMaxForInteraction = 0.2f;

    public void Awake()
    {
        inputActionMove = InputHandler.Instance.manager.GetInputPlayerData("Move");

        inputActionUseE = InputHandler.Instance.manager.GetInputPlayerData("UseE");
        inputActionUseE.started += HandleForUseEDown;
        inputActionUseE.canceled += HandleForUseEUp;
    }

    public void Update()
    {
        HandleForInteraction();
        HandleForCameraBlock();
    }

    public void FixedUpdate()
    {
        HandleForMoveUpdate();
    }

    public void EnabledControl(bool enabled, bool isHideControlTarget)
    {
        base.EnabledControl(enabled);
        if (!enabled)
        {
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
    /// 播放控制物体的动画
    /// </summary>
    /// <param name="animationCreatureState"></param>
    /// <param name="isLoop"></param>
    public void PlayAnimForControlTarget(SpineAnimationStateEnum animationCreatureState, bool isLoop = true)
    {
        if (controlTargetForCreatureSkeletonAnimation == null)
        {
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            controlTargetForCreatureSkeletonAnimation = targetMove.transform.Find("Renderer").GetComponent<SkeletonAnimation>();
        }
        if (controlTargetForCreatureAnim != animationCreatureState && controlTargetForCreatureSkeletonAnimation != null)
        {
            SpineHandler.Instance.PlayAnim(controlTargetForCreatureSkeletonAnimation, animationCreatureState, isLoop);
            controlTargetForCreatureAnim = animationCreatureState;
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
        }
        else
        {
            Vector3 targetMoveOffset = new Vector3(moveData.x * Time.deltaTime * speedForCreatureMoveX, 0, moveData.y * Time.deltaTime * speedForCreatureMoveZ);
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            //检测边界
            Vector3 targetPosition = targetMove.transform.position + targetMoveOffset;
            if (!CheckSceneBoard(targetPosition))
            {
                targetMove.transform.position = targetPosition;
            }

            Vector3 sizeOriginal = controlTargetForCreatureSkeletonAnimation.transform.localScale;
            float directionXSize = Mathf.Abs(sizeOriginal.x);
            if (moveData.x > 0)
            {
                controlTargetForCreatureSkeletonAnimation.transform.localScale = new Vector3(directionXSize, sizeOriginal.y, sizeOriginal.z);
            }
            else if (moveData.x < 0)
            {
                controlTargetForCreatureSkeletonAnimation.transform.localScale = new Vector3(-directionXSize, sizeOriginal.y, sizeOriginal.z);
            }
            //播放动画
            PlayAnimForControlTarget(SpineAnimationStateEnum.Walk);
     
        }
    }

    /// <summary>
    /// 检测场景边界
    /// </summary>
    public bool CheckSceneBoard(Vector3 endPosition)
    {
        float dis = Vector3.Distance(endPosition, Vector3.zero);
        if (dis > 8.3)
        {
           return true;
        }
        return false;
    }

    /// <summary>
    /// 处理交互
    /// </summary>
    public void HandleForInteraction()
    {
        if (!enabledControl)
            return;
        timeUpdateForInteraction += Time.deltaTime;
        if (timeUpdateForInteraction > timeMaxForInteraction)
        {
            timeUpdateForInteraction = 0;
            var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;

            if (controlTargetForInteraction == null)
            {
                controlTargetForInteraction = controlTarget.transform.Find("Interaction").gameObject;
            }
            var allHit = RayUtil.OverlapToSphere(controlTarget.transform.position, 0.1f, 1 << LayerInfo.Interaction);
            if (allHit.Length > 0)
            {
                controlTargetForInteraction.SetActive(true);
            }
            else
            {
                controlTargetForInteraction.SetActive(false);
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
        //核心
        if (firstHit.gameObject.name.Equals("CoreInteraction"))
        {
            UIBaseCore targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
        }
        //传送门
        else if (firstHit.gameObject.name.Equals("PortalInteraction"))
        {
            UIBasePortal targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIBasePortal>();
        }
        //终焉议会
        else if (firstHit.gameObject.name.Equals("DoomCouncilInteraction"))
        {
            UIDoomCouncilMain targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilMain>();
        }
    }
}
