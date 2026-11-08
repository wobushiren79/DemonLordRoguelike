using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 管理终焉议会围墙的镜头遮挡和战斗时的门扇。
/// </summary>
public class DoomCouncilArchitectureView : MonoBehaviour
{
    [SerializeField] private Transform wallEast;
    [SerializeField] private GameObject eastBraziers;
    [SerializeField] private Transform wallWest;
    [SerializeField] private GameObject rearBraziers;
    [SerializeField] private Transform wallPodium;
    [SerializeField] private Transform wallGate;
    [SerializeField] private GameObject cutawayEast;
    [SerializeField] private Transform podiumEmblem;
    [SerializeField] private Transform gate;
    [SerializeField] private Transform gateLeftLeaf;
    [SerializeField] private Transform gateRightLeaf;
    [SerializeField] private GameObject gateSeam;

    [System.Flags]
    private enum WallSide
    {
        None = 0,
        East = 1,
        West = 2,
        Podium = 4,
        Gate = 8
    }

    private Renderer[] eastRenderers;
    private Renderer[] westRenderers;
    private Renderer[] podiumRenderers;
    private Renderer[] gateWallRenderers;
    private Renderer[] emblemRenderers;
    private Renderer[] gateRenderers;
    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;
    private WallSide hiddenWalls;

    #region 初始化

    /// <summary>
    /// 缓存围墙显示组件和门扇的关闭姿态。
    /// </summary>
    private void Awake()
    {
        eastRenderers = wallEast.GetComponentsInChildren<Renderer>(true);
        westRenderers = wallWest.GetComponentsInChildren<Renderer>(true);
        podiumRenderers = wallPodium.GetComponentsInChildren<Renderer>(true);
        gateWallRenderers = wallGate.GetComponentsInChildren<Renderer>(true);
        emblemRenderers = podiumEmblem.GetComponentsInChildren<Renderer>(true);
        gateRenderers = gate.GetComponentsInChildren<Renderer>(true);
        leftClosedRotation = gateLeftLeaf.localRotation;
        rightClosedRotation = gateRightLeaf.localRotation;
    }

    /// <summary>
    /// 在主摄像机渲染前检查围墙遮挡。
    /// </summary>
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += UpdateWallVisibility;
    }

    /// <summary>
    /// 解绑渲染回调并恢复完整围墙的可见性。
    /// </summary>
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= UpdateWallVisibility;
        SetHiddenWalls(WallSide.None);
    }

    #endregion

    #region 围墙与大门显示

    /// <summary>
    /// 战斗时向外打开门扇，为议员的进攻路线留出入口。
    /// </summary>
    public void SetForBattle(bool forBattle)
    {
        Vector3 hingeUp = gate.InverseTransformDirection(transform.up);
        float angle = forBattle ? 90f : 0f;
        gateLeftLeaf.localRotation = Quaternion.AngleAxis(-angle, hingeUp) * leftClosedRotation;
        gateRightLeaf.localRotation = Quaternion.AngleAxis(angle, hingeUp) * rightClosedRotation;
        gateSeam.SetActive(!forBattle);
    }

    /// <summary>
    /// 主摄像机移出围墙时隐藏近侧墙面，避免遮挡议员或战斗道路。
    /// </summary>
    private void UpdateWallVisibility(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType != CameraType.Game || camera != Camera.main)
            return;

        Vector3 point = transform.InverseTransformPoint(camera.transform.position);
        WallSide outside = WallSide.None;
        if (point.x > transform.InverseTransformPoint(wallEast.position).x)
            outside |= WallSide.East;
        if (point.x < transform.InverseTransformPoint(wallWest.position).x)
            outside |= WallSide.West;
        if (point.z < transform.InverseTransformPoint(wallPodium.position).z)
            outside |= WallSide.Podium;
        if (point.z > transform.InverseTransformPoint(wallGate.position).z)
            outside |= WallSide.Gate;

        SetHiddenWalls(outside);
    }

    /// <summary>
    /// 只更新本次跨过边界的墙面，东墙使用已建好的低墙剖视。
    /// </summary>
    private void SetHiddenWalls(WallSide outside)
    {
        WallSide changed = hiddenWalls ^ outside;
        if ((changed & WallSide.East) != 0)
        {
            bool hidden = (outside & WallSide.East) != 0;
            SetRenderersHidden(eastRenderers, hidden);
            cutawayEast.SetActive(hidden);
            if (eastBraziers != null)
                eastBraziers.SetActive(!hidden);
        }
        if ((changed & WallSide.West) != 0)
        {
            bool hidden = (outside & WallSide.West) != 0;
            SetRenderersHidden(westRenderers, hidden);
            rearBraziers.SetActive(!hidden);
        }
        if ((changed & WallSide.Podium) != 0)
        {
            bool hidden = (outside & WallSide.Podium) != 0;
            SetRenderersHidden(podiumRenderers, hidden);
            SetRenderersHidden(emblemRenderers, hidden);
        }
        if ((changed & WallSide.Gate) != 0)
        {
            bool hidden = (outside & WallSide.Gate) != 0;
            SetRenderersHidden(gateWallRenderers, hidden);
            SetRenderersHidden(gateRenderers, hidden);
        }
        hiddenWalls = outside;
    }

    /// <summary>
    /// 切换模型渲染，同时保留碰撞和场景交互状态。
    /// </summary>
    private static void SetRenderersHidden(Renderer[] renderers, bool hidden)
    {
        foreach (Renderer renderer in renderers)
            renderer.forceRenderingOff = hidden;
    }

    #endregion
}
