using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击模块(弹道)GPU Instancing 批量渲染器（DSP 式："只记录位置，一起绘制"）。
/// <para>不为每发弹道单独渲染，而是每帧遍历活跃弹道，按视觉类型(attackModeInfo.visual_name)分桶，
/// 用各弹道的 position(方案B 权威源)构建变换矩阵，每桶用 Graphics.DrawMeshInstanced 一次性批量绘制。</para>
/// <para>与原预制渲染并存、互不干扰：visual_name 走本批量渲染(DSP)，prefab_name 仍走原 prefab 上的
/// SpriteRenderer/VisualEffect(由 FightManager.GetAttackModePrefab 创建)。二者是独立字段，配置侧二选一。</para>
/// <para>常开(无总开关)但天然零副作用：visual_name 为空、或未 RegisterVisual 该视觉桶的弹道会被跳过，什么都不画。</para>
/// 拖尾(Trail)暂不处理。分裂弹(AttackModeRangedSplit)自管多个 GameObject，不纳入本渲染器(不注册其视觉桶)。
/// </summary>
public class AttackModeInstanceRenderer
{
    #region 常量与内部结构
    //Graphics.DrawMeshInstanced 单批矩阵上限(Unity 硬限制 1023)
    private const int MaxInstancesPerBatch = 1023;

    /// <summary>
    /// 单个视觉类型的渲染桶：一个 Mesh + 一个(开启 GPU Instancing 的)Material + 复用矩阵缓冲。
    /// </summary>
    private class VisualBucket
    {
        //弹体网格(通常是朝相机的 Quad)
        public Mesh mesh;
        //弹体材质(必须勾选 Enable GPU Instancing，或用支持 instancing 的 shader)
        public Material material;
        //本帧待绘制矩阵缓冲(固定 1023 复用，避免热路径分配)
        public readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxInstancesPerBatch];
        //缓冲当前已填充数量
        public int count;
    }
    #endregion

    #region 字段
    //视觉类型注册表：key = attackModeInfo.visual_name，value = 对应渲染桶
    private readonly Dictionary<string, VisualBucket> dicBucket = new Dictionary<string, VisualBucket>();
    //弹体统一缩放(过渡期简单参数，后续可下沉到配置/按桶区分)
    public float uniformScale = 1f;
    #endregion

    #region 视觉桶注册
    /// <summary>
    /// 注册/替换某视觉类型(visual_name)的渲染桶；mesh 或 material 为空视为取消该类型的 instancing 渲染。
    /// </summary>
    public void RegisterVisual(string visualKey, Mesh mesh, Material material)
    {
        if (string.IsNullOrEmpty(visualKey))
            return;
        if (mesh == null || material == null)
        {
            dicBucket.Remove(visualKey);
            return;
        }
        if (!dicBucket.TryGetValue(visualKey, out VisualBucket bucket))
        {
            bucket = new VisualBucket();
            dicBucket[visualKey] = bucket;
        }
        bucket.mesh = mesh;
        bucket.material = material;
    }

    /// <summary>
    /// 注销某视觉类型的渲染桶(该类型退回原 prefab 渲染)。
    /// </summary>
    public void UnregisterVisual(string visualKey)
    {
        if (!string.IsNullOrEmpty(visualKey))
            dicBucket.Remove(visualKey);
    }

    /// <summary>
    /// 清空所有视觉桶注册。
    /// </summary>
    public void ClearVisuals()
    {
        dicBucket.Clear();
    }

    /// <summary>
    /// 是否已注册任意视觉桶。
    /// </summary>
    public bool HasAnyVisual => dicBucket.Count > 0;

    /// <summary>
    /// 指定视觉类型(visual_name)是否已注册视觉桶；供调用方懒注册去重，避免重复加载。
    /// </summary>
    public bool HasVisual(string visualKey)
    {
        return !string.IsNullOrEmpty(visualKey) && dicBucket.ContainsKey(visualKey);
    }
    #endregion

    #region 批量绘制
    /// <summary>
    /// 每帧调用：遍历活跃弹道，按 visual_name 分桶收集 position 矩阵并批量绘制。
    /// <para>无注册桶或列表为空时直接返回；visual_name 为空或未注册视觉桶的弹道类型被跳过(不画，交由原 prefab 渲染)。</para>
    /// </summary>
    public void RenderAll(List<BaseAttackMode> listAttackMode)
    {
        if (dicBucket.Count == 0 || listAttackMode == null)
            return;
        int count = listAttackMode.Count;
        if (count == 0)
            return;

        //收集：按视觉类型把每发弹道的位置矩阵填入对应桶缓冲，缓冲填满即刻绘制并清空
        for (int i = 0; i < count; i++)
        {
            var itemAttackMode = listAttackMode[i];
            if (itemAttackMode == null || !itemAttackMode.isValid || itemAttackMode.attackModeInfo == null)
                continue;
            string visualKey = itemAttackMode.attackModeInfo.visual_name;
            if (string.IsNullOrEmpty(visualKey) || !dicBucket.TryGetValue(visualKey, out VisualBucket bucket))
                continue;

            //位置来自方案B 权威源 position；旋转暂用单位四元数(朝相机 billboard 交由 shader 处理)
            bucket.matrixBuffer[bucket.count] = Matrix4x4.TRS(itemAttackMode.position, Quaternion.identity, Vector3.one * uniformScale);
            bucket.count++;
            if (bucket.count >= MaxInstancesPerBatch)
            {
                Graphics.DrawMeshInstanced(bucket.mesh, 0, bucket.material, bucket.matrixBuffer, bucket.count);
                bucket.count = 0;
            }
        }

        //收尾：绘制各桶剩余不足一批的矩阵并清空计数
        foreach (var bucket in dicBucket.Values)
        {
            if (bucket.count > 0)
            {
                Graphics.DrawMeshInstanced(bucket.mesh, 0, bucket.material, bucket.matrixBuffer, bucket.count);
                bucket.count = 0;
            }
        }
    }
    #endregion
}
