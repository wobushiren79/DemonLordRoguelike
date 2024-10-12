using UnityEngine;
using UnityEngine.VFX;

public class ScenePrefabForBase : ScenePrefabBase
{
    //核心建筑
    public GameObject objBuildingCore;
    //核心眼睛
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


    #region 核心建筑眼球处理
    //核心建筑眼球看向目标
    protected Vector3 targetLookAtForBuildingCoreEye;
    //核心建筑眼球转动速度
    protected float speedRotationForBuildingCoreEye = 0.1f;
    /// <summary>
    /// 处理核心建筑的眼睛
    /// </summary>
    public void HandleUpdateForBuildingCore()
    {
        Camera mainCamera = CameraHandler.Instance.manager.mainCamera;
        //看向摄像头
        if (mainCamera != null)
        {
            targetLookAtForBuildingCoreEye = Vector3.Lerp(mainCamera.transform.position, targetLookAtForBuildingCoreEye, Time.deltaTime * speedRotationForBuildingCoreEye);
            objBuildingCoreEye.transform.LookAt(targetLookAtForBuildingCoreEye);
        }
    }
    #endregion
}
