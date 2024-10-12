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
    /// <summary>
    /// ������Ľ������۾�
    /// </summary>
    public void HandleUpdateForBuildingCore()
    {
        Camera mainCamera = CameraHandler.Instance.manager.mainCamera;
        //��������ͷ
        if (mainCamera != null)
        {
            targetLookAtForBuildingCoreEye = Vector3.Lerp(mainCamera.transform.position, targetLookAtForBuildingCoreEye, Time.deltaTime * speedRotationForBuildingCoreEye);
            objBuildingCoreEye.transform.LookAt(targetLookAtForBuildingCoreEye);
        }
    }
    #endregion
}
