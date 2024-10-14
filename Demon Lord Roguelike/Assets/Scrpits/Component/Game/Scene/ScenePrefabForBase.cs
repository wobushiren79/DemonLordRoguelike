using UnityEngine;
using UnityEngine.VFX;

public class ScenePrefabForBase : ScenePrefabBase
{
    //���Ľ���
    public GameObject objBuildingCore;
    //�����۾�
    public GameObject objBuildingCoreEye;

    public VisualEffect effectEggBreak;


    //public void Awake()
    //{
    //    objBuildingCore = transform.Find("Core/Building").gameObject;
    //    effectEggBreak = transform.Find("Effect/EggBreak").GetComponent<VisualEffect>();
    //}

    public void Update()
    {
        HandleUpdateForBuildingCore();
    }


    #region ���Ľ���������
    //���Ľ���������Ŀ��
    protected Vector3 targetLookAtForBuildingCoreEye;
    //���Ľ�������ת���ٶ�
    protected float speedRotationForBuildingCoreEye = 0.1f;
    //���Ľ�������Ĭ�Ͽ���λ��
    protected Vector3 positionDefLookAtForBuildingCoreEye = new Vector3(0, 0, -1000f);
    /// <summary>
    /// ������Ľ������۾�
    /// </summary>
    public void HandleUpdateForBuildingCore()
    {
        var controlForBaseCreature = GameControlHandler.Instance.manager.controlTargetForCreature;
        Vector3 targetLookAtPosition = positionDefLookAtForBuildingCoreEye;
        if (controlForBaseCreature != null && controlForBaseCreature.gameObject.activeSelf)
        {
            targetLookAtPosition = controlForBaseCreature.transform.position;
        }
        else
        {
            Camera mainCamera = CameraHandler.Instance.manager.mainCamera;
            //��������ͷ
            if (mainCamera != null)
            {
                targetLookAtPosition = mainCamera.transform.position;
            }
        }
        targetLookAtForBuildingCoreEye = Vector3.Lerp(targetLookAtPosition, targetLookAtForBuildingCoreEye, Time.deltaTime * speedRotationForBuildingCoreEye);
        objBuildingCoreEye.transform.LookAt(targetLookAtForBuildingCoreEye);
        return;
    }
    #endregion
}
