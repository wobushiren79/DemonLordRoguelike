using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using static UnityEngine.InputSystem.InputAction;

public class ControlForGameFight : BaseControl
{
    [HideInInspector]
    public InputAction inputActionMove;
    //public InputAction inputActionMoveMouse;

    public InputAction inputActionUseL;
    public InputAction inputActionUseR;
    //删除生物模式开关(快捷键 C)，来源于 Player 输入映射的 "C" 动作
    public InputAction inputActionDeleteCreature;

    [Header("镜头移动速度")]
    public float speedForCameraMoveX = 5f;
    public float speedForCameraMoveZ = 5f;
    public bool isUserRClick = false;
    public bool isUserLClick = false;

    //是否正在拖拽摄像头
    public bool isDraggingCamera = false;
    protected float speedForDargCamera = 500f;
    protected float minX = 3;
    protected float maxX = 7f;
    protected float minZ = 2f;
    protected float maxZ = 6f;

    private Vector3 dragCameraOrigin;

    public void Awake()
    {
        inputActionMove = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.Move);
        //inputActionMoveMouse = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.MoveMouse);

        inputActionUseL = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.UseL);
        inputActionUseL.started += HandleForUseLDown;
        inputActionUseL.canceled += HandleForUseLUp;

        inputActionUseR = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.UseR);
        inputActionUseR.started += HandleForUseRDown;
        inputActionUseR.canceled += HandleForUseRUp;

        //删除生物模式开关(C)，按下时触发(替代旧的 Input.GetKeyDown(KeyCode.C) 轮询)
        inputActionDeleteCreature = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.C);
        inputActionDeleteCreature.started += HandleForDeleteCreatureToggle;
    }
    public void OnDestroy()
    {
        inputActionUseL.started -= HandleForUseLDown;
        inputActionUseL.canceled -= HandleForUseLUp;
        inputActionUseR.started -= HandleForUseRDown;
        inputActionUseR.canceled -= HandleForUseRUp;
        inputActionDeleteCreature.started -= HandleForDeleteCreatureToggle;
    }

    public void Update()
    {
        HandleForMoveMouseUpdate();
    }

    public void FixedUpdate()
    {
        HandleForMoveUpdate();
        HandleForClickDropUpdate();
    }

    public override void EnabledControl(bool enabled)
    {
        base.EnabledControl(enabled);
        if (!enabled)
            return;
        GameControlHandler.Instance.manager.controlTargetForEmpty.SetActive(true);
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

        }
        else
        {
            Vector3 targetMoveOffset = new Vector3(moveData.x * Time.deltaTime * speedForCameraMoveX, 0, moveData.y * Time.deltaTime * speedForCameraMoveZ);
            var targetMove = GameControlHandler.Instance.manager.controlTargetForEmpty;
            targetMove.transform.position = targetMove.transform.position + targetMoveOffset;
            ClampCameraPosition();
        }
    }

    /// <summary>
    /// 删除生物模式快捷键(C)处理：
    /// 按 C 快速启动删除生物功能；删除生物功能已启动时再次按 C 关闭。
    /// 由 Player 输入映射的 "C" 动作 started 回调驱动(替代旧的 Input.GetKeyDown(KeyCode.C) 轮询)。
    /// </summary>
    /// <param name="callback">输入回调上下文</param>
    public void HandleForDeleteCreatureToggle(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
            return;
        //已处于删除模式：再次按 C 关闭删除生物功能
        if (gameFightLogic.selectCreatureDestory != null)
        {
            gameFightLogic.UnSelectCreatureDestroy();
        }
        //未处于删除模式：按 C 启动删除生物功能
        else
        {
            gameFightLogic.SelectCreatureDestroy();
        }
    }

    /// <summary>
    /// 鼠标拖拽移动镜头：拖拽状态(isDraggingCamera)与起点(dragCameraOrigin)由鼠标右键 InputAction
    /// (UseR)在 HandleForUseRDown/Up 中维护；此处仅在拖拽中按鼠标位移驱动镜头(经 InputSystem 读取鼠标位置)。
    /// </summary>
    public void HandleForMoveMouseUpdate()
    {
        if (!enabledControl)
            return;
        if (isDraggingCamera && Mouse.current != null)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Vector3 pos = Camera.main.ScreenToViewportPoint(mousePos - dragCameraOrigin);
            Vector3 targetMoveOffset = new Vector3(-pos.x * Time.deltaTime * speedForDargCamera, 0, -pos.y * Time.deltaTime * speedForDargCamera);

            var targetMove = GameControlHandler.Instance.manager.controlTargetForEmpty;
            targetMove.transform.position = targetMove.transform.position + targetMoveOffset;
            ClampCameraPosition();
            dragCameraOrigin = mousePos;
        }
    }

    /// <summary>
    /// 限制摄像头位置
    /// </summary>
    public void ClampCameraPosition()
    {        
        var targetMove = GameControlHandler.Instance.manager.controlTargetForEmpty;
        // 限制相机移动范围
        Vector3 clampedPosition = targetMove.transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minZ, maxZ);
        targetMove.transform.position = clampedPosition;
    }

    /// <summary>
    /// 点击拾取物品
    /// </summary>
    public void HandleForClickDropUpdate()
    {
        if (!enabledControl)
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;
        //手里没有物品
        if (gameFightLogic.selectCreature != null)
            return;
        if (gameFightLogic.selectCreatureDestory != null)
            return;
        float inputValue = inputActionUseL.ReadValue<float>();
        if (inputValue < 1)
            return;
        //拾取水晶
        gameFightLogic.PickupCrystalForMouse();
    }

    /// <summary>
    /// 鼠标左键确认-按下
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseLDown(CallbackContext callback)
    {
        isUserLClick = true;
        if (!enabledControl)
            return;
    }

    /// <summary>
    /// 鼠标左键确认-抬起
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseLUp(CallbackContext callback)
    {
        isUserLClick = false;
        if (!enabledControl)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //如果有选择的物体 放置物体
        if (gameFightLogic.selectCreature != null)
        {
            //如果是选择的生物
            gameFightLogic.PutCard();
            return;
        }
        if (gameFightLogic.selectCreatureDestory != null)
        {
            //如果是选择的删除生物
            gameFightLogic.SelectCreatureDestoryHandle();
            return;
        }
    }

    /// <summary>
    /// 鼠标右键取消-按下
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseRDown(CallbackContext callback)
    {
        isUserRClick = true;
        //仅鼠标右键触发镜头拖拽：记录拖拽起点并进入拖拽状态(替代旧的 Input.GetMouseButtonDown(1))
        if (callback.control != null && callback.control.device is Mouse && Mouse.current != null)
        {
            isDraggingCamera = true;
            dragCameraOrigin = Mouse.current.position.ReadValue();
        }
        if (!enabledControl)
            return;
    }

    /// <summary>
    /// 鼠标右键取消-抬起
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseRUp(CallbackContext callback)
    {
        isUserRClick = false;
        //右键抬起：退出镜头拖拽状态(替代旧的 Input.GetMouseButtonUp(1))
        isDraggingCamera = false;
        if (!enabledControl)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //如果有选择的物体 放置物体
        if (gameFightLogic.selectCreature != null)
        {
            gameFightLogic.UnSelectCard();
        }
        if (gameFightLogic.selectCreatureDestory != null)
        {
            gameFightLogic.UnSelectCreatureDestroy();
        }
    }
}
