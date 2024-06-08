using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using static UnityEngine.InputSystem.InputAction;

public class ControlForGameFight: BaseControl
{
    [HideInInspector]
    public InputAction inputActionMove;
    public InputAction inputActionUseL;
    public InputAction inputActionUseR;
    
    [Header("镜头移动速度")]
    public float speedForCameraMoveX = 2f;
    public float speedForCameraMoveZ = 2f;
    public float speedForLerpCameraMove = 0.2f;

    public void Awake()
    {
        inputActionMove = InputHandler.Instance.manager.GetInputPlayerData("Move"); 

        inputActionUseL = InputHandler.Instance.manager.GetInputPlayerData("UseL");
        inputActionUseL.started += HandleForUseLDown;
        inputActionUseL.canceled += HandleForUseLUp;

        inputActionUseR = InputHandler.Instance.manager.GetInputPlayerData("UseR");
        inputActionUseR.started += HandleForUseRDown;
        inputActionUseR.canceled += HandleForUseRUp;
    }
    public void OnDestroy()
    {
        inputActionUseL.started -= HandleForUseLDown;
        inputActionUseL.canceled -= HandleForUseLUp;
        inputActionUseR.started -= HandleForUseRDown;
        inputActionUseR.canceled -= HandleForUseRUp;
    }

    public void FixedUpdate()
    {
        HandlerForMoveUpdate();
    }

    public override void EnabledControl(bool enabled)
    {
        base.EnabledControl(enabled); 
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        targetCamerMovePos = mainCamera.transform.position;
    }

    protected Vector3 targetCamerMovePos;
    /// <summary>
    /// 移动处理
    /// </summary>
    public void HandlerForMoveUpdate()
    {
        if (!enabledControl)
            return;
        Vector2 moveData = inputActionMove.ReadValue<Vector2>();
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        Vector3 currentPos = mainCamera.transform.position;
        if (moveData.x == 0 && moveData.y == 0)
        {
          
        }
        else
        {
            Vector3 targetMoveOffset = new Vector3(moveData.x * Time.deltaTime * speedForCameraMoveX, 0, moveData.y * Time.deltaTime * speedForCameraMoveZ);
            targetCamerMovePos = targetCamerMovePos + targetMoveOffset;
        }
        mainCamera.transform.position = Vector3.Lerp(currentPos, targetCamerMovePos,  speedForLerpCameraMove);
    }

    /// <summary>
    /// 鼠标左键确认-按下
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseLDown(CallbackContext callback)
    {
        if (!enabledControl)
            return;
    }

    /// <summary>
    /// 鼠标左键确认-抬起
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseLUp(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //如果有选择的物体 放置物体
        if (gameFightLogic.selectCreature != null)
        {
            gameFightLogic.PutCard();
            return;
        }
    }

    /// <summary>
    /// 鼠标右键取消-按下
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseRDown(CallbackContext callback)
    {
        if (!enabledControl)
            return;
    }

    /// <summary>
    /// 鼠标右键取消-抬起
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseRUp(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //如果有选择的物体 放置物体
        if (gameFightLogic.selectCreature != null)
        {
            gameFightLogic.UnSelectCard();
            return;
        }
    }
}
