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
    protected float minZ = 3f;
    protected float maxZ = 6f;

    private Vector3 dragCameraOrigin;

    public void Awake()
    {
        inputActionMove = InputHandler.Instance.manager.GetInputPlayerData("Move");
        //inputActionMoveMouse = InputHandler.Instance.manager.GetInputPlayerData("MoveMouse");

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
    /// 鼠标移动
    /// </summary>
    public void HandleForMoveMouseUpdate()
    {       
        if (Input.GetMouseButtonDown(1))
        {
            dragCameraOrigin = Input.mousePosition;
            isDraggingCamera = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isDraggingCamera = false;
        } 
        if (!enabledControl)
            return;

        if (isDraggingCamera)
        {
            Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragCameraOrigin);
            Vector3 targetMoveOffset = new Vector3(-pos.x * Time.deltaTime * speedForDargCamera, 0, -pos.y * Time.deltaTime * speedForDargCamera);

            var targetMove = GameControlHandler.Instance.manager.controlTargetForEmpty;
            targetMove.transform.position = targetMove.transform.position + targetMoveOffset;
            ClampCameraPosition();
            dragCameraOrigin = Input.mousePosition;
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
        //如果是正在游戏中
        if (gameFightLogic.gameState != GameStateEnum.Gaming)
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
        RayUtil.RayToScreenPointForMousePosition(100, 1 << LayerInfo.Drop, out bool isCollider, out RaycastHit hit);
        if (isCollider && hit.collider != null)
        {
            var fightDropPrefab = FightHandler.Instance.manager.GetFightPrefab(hit.collider.gameObject.name);
            if (fightDropPrefab == null)
                return;
            fightDropPrefab.SetState(GameFightPrefabStateEnum.Droping);
            Vector3 targetPos = gameFightLogic.fightData.fightDefenseCoreCreature.creatureObj.transform.position;
            float moveSpeed = 5;
            float moveTime = Vector3.Distance(targetPos, fightDropPrefab.gameObject.transform.position) / moveSpeed;
            //播放动画
            fightDropPrefab.gameObject.transform
                .DOJump(targetPos + new Vector3(0f, 0.5f, 0.5f), Random.Range(0, 0.5f), 1, moveTime)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                    userData.AddCrystal(fightDropPrefab.valueInt);
                    fightDropPrefab.Destroy();
                    //刷新所有打开的UI
                    UIHandler.Instance.RefreshUI();
                });
        }
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
