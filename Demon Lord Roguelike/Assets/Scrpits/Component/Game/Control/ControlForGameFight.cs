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
    public InputAction inputActionUseL;
    public InputAction inputActionUseR;

    [Header("镜头移动速度")]
    public float speedForCameraMoveX = 2f;
    public float speedForCameraMoveZ = 2f;

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
        }
    }

    /// <summary>
    /// 点击拾取物品
    /// </summary>
    public void HandleForClickDropUpdate()
    {
        if (!enabledControl)
            return;
        var gameState = GameHandler.Instance.manager.GetGameState();
        //如果是正在游戏中
        if (gameState != GameStateEnum.Gaming)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //手里没有物品
        if (gameFightLogic.selectCreature != null)
            return;
        if (gameFightLogic.selectCreatureDestory != null)
            return;
        float inputValue = inputActionUseL.ReadValue<float>();
        if (inputValue < 1)
            return;
        RayUtil.RayToScreenPointForMousePosition(10, 1 << LayerInfo.Drop, out bool isCollider, out RaycastHit hit, CameraHandler.Instance.manager.mainCamera);
        if (isCollider && hit.collider != null)
        {
            var fightDropPrefab = FightHandler.Instance.manager.GetFightPrefab(hit.collider.gameObject.name);
            if (fightDropPrefab == null)
                return;
            fightDropPrefab.SetState(GameFightPrefabStateEnum.Droping);
            Vector3 targetPos = gameFightLogic.fightData.fightDefenseCoreCreature.creatureObj.transform.position;
            float moveSpeed = 5
                ;
            float moveTime = Vector3.Distance(targetPos, fightDropPrefab.gameObject.transform.position) / moveSpeed;
            //播放动画
            fightDropPrefab.gameObject.transform
                .DOJump(targetPos + new Vector3(0f, 0.5f, 0.5f), Random.Range(0, 0.5f), 1, moveTime)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                    userData.AddCoin(fightDropPrefab.valueInt);
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
        }
        if (gameFightLogic.selectCreatureDestory != null)
        {
            gameFightLogic.UnSelectCreatureDestory();
        }
    }
}
