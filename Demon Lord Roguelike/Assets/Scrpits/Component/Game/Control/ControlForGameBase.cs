using OfficeOpenXml.Packaging;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    public AnimationCreatureStateEnum controlTargetForCreatureAnim = AnimationCreatureStateEnum.None;

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
    }

    public void FixedUpdate()
    {
        HandleForMoveUpdate();
    }


    public override void EnabledControl(bool enabled)
    {
        base.EnabledControl(enabled);
        if (!enabled)
        {
            GameControlHandler.Instance.manager.controlTargetForCreature.SetActive(false);
            return;
        }
        GameControlHandler.Instance.manager.controlTargetForCreature.SetActive(true);
        //播放动画
        PlayAnimForControlTarget(AnimationCreatureStateEnum.Idle);
    }

    /// <summary>
    /// 播放控制物体的动画
    /// </summary>
    /// <param name="animationCreatureState"></param>
    /// <param name="isLoop"></param>
    public void PlayAnimForControlTarget(AnimationCreatureStateEnum animationCreatureState,bool isLoop =  true)
    {
        if (controlTargetForCreatureSkeletonAnimation == null)
        {
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            controlTargetForCreatureSkeletonAnimation = targetMove.transform.Find("Renderer").GetComponent<SkeletonAnimation>();
        }
        if (controlTargetForCreatureAnim != animationCreatureState && controlTargetForCreatureSkeletonAnimation != null)
        {
            controlTargetForCreatureSkeletonAnimation.AnimationState.SetAnimation(0, animationCreatureState.ToString(), isLoop);
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
            PlayAnimForControlTarget(AnimationCreatureStateEnum.Idle);
        }
        else
        {
            Vector3 targetMoveOffset = new Vector3(moveData.x * Time.deltaTime * speedForCreatureMoveX, 0, moveData.y * Time.deltaTime * speedForCreatureMoveZ);
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            targetMove.transform.position = targetMove.transform.position + targetMoveOffset;

            Vector3 sizeOriginal = controlTargetForCreatureSkeletonAnimation.transform.localScale;
            float directionXSize = Mathf.Abs(sizeOriginal.x);
            if (moveData.x > 0)
            {
                controlTargetForCreatureSkeletonAnimation.transform.localScale = new Vector3(directionXSize, sizeOriginal.y, sizeOriginal.z);
            }
            else if(moveData.x < 0)
            {
                controlTargetForCreatureSkeletonAnimation.transform.localScale = new Vector3(-directionXSize, sizeOriginal.y, sizeOriginal.z);
            }
            //播放动画
            PlayAnimForControlTarget(AnimationCreatureStateEnum.Walk);
        }
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

    public void HandleForUseEDown(CallbackContext callback)
    {

    }

    public void HandleForUseEUp(CallbackContext callback)
    {
        var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        var allHit = RayUtil.OverlapToSphere(controlTarget.transform.position, 0.1f, 1 << LayerInfo.Interaction);
        if (allHit.Length <= 0)
            return;
        var firstHit = allHit[0];
        //核心
        if (firstHit.gameObject.name.Equals("Core"))
        {
            UIBaseCore targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
        }
    }
}
