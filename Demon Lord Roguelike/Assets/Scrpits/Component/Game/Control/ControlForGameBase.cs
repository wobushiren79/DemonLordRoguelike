using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlForGameBase : BaseControl
{
    [HideInInspector]
    public InputAction inputActionMove;

    [Header("角色移动速度")]
    public float speedForCreatureMoveX = 3f;
    public float speedForCreatureMoveZ = 3f;

    public void Awake()
    {
        inputActionMove = InputHandler.Instance.manager.GetInputPlayerData("Move");
    }
    public void FixedUpdate()
    {
        HandleForMoveUpdate();
    }

    public override void EnabledControl(bool enabled)
    {
        base.EnabledControl(enabled);
        GameControlHandler.Instance.manager.controlTargetForCreature.SetActive(true);
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
            Vector3 targetMoveOffset = new Vector3(moveData.x * Time.deltaTime * speedForCreatureMoveX, 0, moveData.y * Time.deltaTime * speedForCreatureMoveZ);
            var targetMove = GameControlHandler.Instance.manager.controlTargetForCreature;
            targetMove.transform.position = targetMove.transform.position + targetMoveOffset;
        }
    }
}
