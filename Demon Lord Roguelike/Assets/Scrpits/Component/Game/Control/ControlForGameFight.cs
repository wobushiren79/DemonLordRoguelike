using DG.Tweening;
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
    
    [Header("��ͷ�ƶ��ٶ�")]
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
        HandleForMoveUpdate();
        HandleForClickDropUpdate();
    }

    public override void EnabledControl(bool enabled)
    {
        base.EnabledControl(enabled); 
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        targetCamerMovePos = mainCamera.transform.position;
    }

    protected Vector3 targetCamerMovePos;
    /// <summary>
    /// �ƶ�����
    /// </summary>
    public void HandleForMoveUpdate()
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
    /// ���ʰȡ��Ʒ
    /// </summary>
    public void HandleForClickDropUpdate()
    {
        if (!enabledControl)
            return;
        var gameState = GameHandler.Instance.manager.GetGameState();
        //�����������Ϸ��
        if (gameState != GameStateEnum.Gaming)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //����û����Ʒ
        if (gameFightLogic.selectCreature != null)
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
            Vector3 targetPos = gameFightLogic.fightData.fightDefCoreCreature.creatureObj.transform.position;
            float moveSpeed = 5
                ;
            float moveTime = Vector3.Distance(targetPos, fightDropPrefab.gameObject.transform.position) / moveSpeed;
            //���Ŷ���
            fightDropPrefab.gameObject.transform
                .DOJump(targetPos + new Vector3(0f, 0.5f, 0.5f), Random.Range(0, 0.5f), 1, moveTime)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                    userData.AddCoin(fightDropPrefab.valueInt);
                    fightDropPrefab.Destroy();
                    //ˢ�����д򿪵�UI
                    UIHandler.Instance.RefreshUI();
                });
        }
    }

    /// <summary>
    /// ������ȷ��-����
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseLDown(CallbackContext callback)
    {
        if (!enabledControl)
            return;
    }

    /// <summary>
    /// ������ȷ��-̧��
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseLUp(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //�����ѡ������� ��������
        if (gameFightLogic.selectCreature != null)
        {
            gameFightLogic.PutCard();
            return;
        }
    }

    /// <summary>
    /// ����Ҽ�ȡ��-����
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseRDown(CallbackContext callback)
    {
        if (!enabledControl)
            return;
    }

    /// <summary>
    /// ����Ҽ�ȡ��-̧��
    /// </summary>
    /// <param name="callback"></param>
    public void HandleForUseRUp(CallbackContext callback)
    {
        if (!enabledControl)
            return;
        if (CheckUtil.CheckIsPointerUI())
            return;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //�����ѡ������� ��������
        if (gameFightLogic.selectCreature != null)
        {
            gameFightLogic.UnSelectCard();
            return;
        }
    }
}
