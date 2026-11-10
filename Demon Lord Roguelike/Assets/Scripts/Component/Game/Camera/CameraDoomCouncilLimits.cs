using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 收住普通议会自由镜头的构图，让角色走到墙边时仍留在画面内。
/// </summary>
public sealed class CameraDoomCouncilLimits : CinemachineExtension
{
    private const float MinCameraZ = -5.5f;
    private const float MinAimZ = -1.5f;
    private const float EaseDistance = 0.55f;

    /// <summary>本次议会实例。为空时沿用基础镜头行为。</summary>
    public Transform SceneRoot { get; set; }

    #region 镜头构图

    /// <summary>在构图阶段约束视线锚点，保留少量向玩家的偏移以照顾近角。</summary>
    public override void PrePipelineMutateCameraStateCallback(
        CinemachineVirtualCameraBase vcam, ref CameraState state, float deltaTime)
    {
        if (SceneRoot == null || !state.HasLookAt())
            return;

        Vector3 aim = SceneRoot.InverseTransformPoint(state.ReferenceLookAt);
        aim.x = Mathf.Lerp(aim.x, LimitSide(aim.x, state.Lens), 0.87f);
        aim.z = Mathf.Lerp(aim.z, -EaseTowardLimit(-aim.z, -MinAimZ), 0.82f);
        state.ReferenceLookAt = SceneRoot.TransformPoint(aim);
    }

    /// <summary>在跟随阻尼之后约束相机位置，再交给 Cinemachine 计算朝向。</summary>
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body || SceneRoot == null)
            return;

        Vector3 position = state.GetCorrectedPosition();
        Vector3 local = SceneRoot.InverseTransformPoint(position);
        local.x = LimitSide(local.x, state.Lens);
        local.z = -EaseTowardLimit(-local.z, -MinCameraZ);
        state.PositionCorrection += SceneRoot.TransformPoint(local) - position;
    }

    /// <summary>按水平视角预留讲台墙角的构图余量。</summary>
    private static float LimitSide(float value, LensSettings lens)
    {
        float horizontalSlope = Mathf.Tan(lens.FieldOfView * 0.5f * Mathf.Deg2Rad) * lens.Aspect;
        float limit = Mathf.Clamp(6.95f - 2.55f * horizontalSlope, 3.9f, 5.55f);
        return Mathf.Sign(value) * EaseTowardLimit(Mathf.Abs(value), limit);
    }

    /// <summary>靠近边界时逐渐减速，到达边界后保持相同位置。</summary>
    private static float EaseTowardLimit(float value, float limit)
    {
        float start = limit - EaseDistance;
        float remaining = 1f - Mathf.InverseLerp(start, limit + EaseDistance, value);
        return Mathf.Min(value, limit - EaseDistance * remaining * remaining);
    }

    #endregion
}
