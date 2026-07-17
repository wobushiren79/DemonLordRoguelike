using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// <see cref="AttackModeInstanceRenderer"/> 的轨迹(拖尾)分部：只放**方案1(Instanced)**——本渲染器自绘的那种拖尾。
/// <para>【效果】克隆弹体桶材质翻成透明+无光+冻结自旋，把弹体贴图画在若干历史位置上、越老越透明，按"年龄档"逐档一次
/// DrawMeshInstanced(整档共享一个 alpha)。颜色是桶级(整桶一色)。</para>
/// <para>【职责边界】本文件 = 轨迹桶结构 + 注册表字段 + 注册/绘制/清理；弹体桶、每帧入口 RenderAll、环境光补偿在主文件
/// AttackModeInstanceRenderer.cs，共享的 MaxInstancesPerBatch 与 BuildInstanceMatrix 亦在主文件(同一 partial 类内可直接访问)。
/// 方案2(Vfx)不在本类，归 <see cref="EffectHandler"/>——见 <see cref="RegisterTrailFromVisual"/> 的分流。</para>
/// </summary>
public partial class AttackModeInstanceRenderer
{
    #region 内部结构：轨迹桶
    /// <summary>
    /// 轨迹桶(拖尾-方案1 Instanced)：克隆自弹体桶材质的透明材质 + 复用弹体 Mesh + 本帧收集的启用弹道列表。
    /// <para>轨迹 = 弹体贴图画在历史位置上、越老越透明；按"年龄档"分批(整档共享 alpha)，每档一次 DrawMeshInstanced。</para>
    /// </summary>
    private class TrailBucket
    {
        //轨迹材质(克隆弹体桶材质→透明+无光+冻结自旋；贴图/UV/宽高比/缩放随克隆继承)
        public Material material;
        //轨迹网格(直接引用弹体桶的 sharedMesh，不拥有、销毁时勿 Destroy)
        public Mesh mesh;
        //轨迹基色(弹体染色 × trail 染色，rgb；alpha 由每档覆盖)
        public Color baseColor;
        //轨迹档数(clamp 到 TrailMaxPoints)
        public int trailNum;
        //最靠近弹体(最新)一档的 alpha
        public float startAlpha;
        //最远(最老)一档的 alpha
        public float endAlpha;
        //本档绘制矩阵缓冲(固定 1023 复用)
        public readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxInstancesPerBatch];
        //缓冲当前已填充数量
        public int count;
        //逐档 alpha 用的 MPB(运行时懒建，规避 MonoBehaviour 构造期限制)
        public MaterialPropertyBlock mpb;
        //本帧收集到的启用轨迹弹道(复用 List，Clear 不释放容量)
        public readonly List<BaseAttackMode> frameAttackModes = new List<BaseAttackMode>();
    }
    #endregion

    #region 字段：轨迹桶注册表
    //轨迹桶注册表：key 与 dicBucket 同签名，value = 轨迹桶；仅 trail_data 启用且 type=Instanced 的视觉登记
    //(方案2 的桶不在这里——存于 EffectManager.dicAttackModeTrailVfx)
    private readonly Dictionary<string, TrailBucket> dicTrailBucket = new Dictionary<string, TrailBucket>();
    //轨迹逐档 alpha 写入用的 _BaseColor 属性 ID
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    #endregion

    #region 轨迹桶注册（拖尾）
    /// <summary>
    /// 为已注册的弹体桶(visualKey)派生轨迹：按 config.type 分流——Vfx 转交 <see cref="EffectHandler"/> 自管，Instanced 则克隆弹体桶材质翻成透明+无光+冻结自旋作轨迹材质。
    /// <para>方案1 的克隆继承弹体材质的贴图/UV(_BaseMap_ST 图集子区域)/宽高比(_VertexScaleXY)/缩放(_VertexScale)，故轨迹与弹体同图同形、零额外拷贝。</para>
    /// <para>config 未启用、桶已建(去重)、或弹体桶尚未就绪(换图子桶异步)时安全跳过；由 FightManager 在弹体桶注册后调用。</para>
    /// </summary>
    public void RegisterTrailFromVisual(string visualKey, AttackModeTrailConfig config)
    {
        if (string.IsNullOrEmpty(visualKey) || !config.enable)
            return;
        //弹体桶未就绪(如换图子桶贴图异步未回)时跳过，下次发射同类弹道再尝试注册
        if (!dicBucket.TryGetValue(visualKey, out VisualBucket vb) || vb.material == null || vb.mesh == null)
            return;
        //方案2(VFX)：本渲染器不建桶不画——转交 EffectHandler 建实例并灌参(去重、表现参数含粒子尺寸皆写死在它内部；逐弹 color 走每帧上传)
        if (config.type == AttackModeTrailType.Vfx)
        {
            EffectHandler.Instance.RegisterAttackModeTrailVfx(visualKey);
            return;
        }
        //去重：该轨迹桶已建则跳过，避免重复克隆材质
        if (dicTrailBucket.ContainsKey(visualKey))
            return;

        //克隆弹体材质(继承贴图/UV/宽高比/缩放)，再翻成轨迹材质
        Material mat = new Material(vb.material);
        SetupTrailMaterial(mat);
        //轨迹基色 = 弹体染色 × trail 染色(仅取 rgb，alpha 由每档覆盖)
        Color tint = mat.HasProperty(PropBaseColor) ? mat.GetColor(PropBaseColor) : Color.white;
        Color baseColor = new Color(tint.r * config.color.r, tint.g * config.color.g, tint.b * config.color.b, 1f);
        dicTrailBucket[visualKey] = new TrailBucket
        {
            material = mat,
            mesh = vb.mesh,
            baseColor = baseColor,
            trailNum = Mathf.Clamp(config.count, 1, BaseAttackMode.TrailMaxPoints),
            startAlpha = config.startAlpha,
            endAlpha = config.endAlpha,
        };
    }

    /// <summary>
    /// 把克隆自弹体桶的材质翻成轨迹材质：透明混合(标准 alpha)、不写深度、关裁剪、冻结自旋、关光照。
    /// <para>轨迹是弹体贴图的半透明冻结快照：关时间自转(否则轨迹一直转)、关 Lit(免 _InstancedFlatGI 补光)。依赖 Shader_Mesh_Common_1 的表面/混合/自旋/光照开关。</para>
    /// </summary>
    private static void SetupTrailMaterial(Material mat)
    {
        //表面类型=透明、渲染模式=标准 alpha 混合(SrcAlpha/OneMinusSrcAlpha)、不写深度
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        //关 Alpha 裁剪(轨迹靠半透明渐隐，不做硬镂空)
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        //冻结自旋(轨迹是过去某刻的静态快照)
        if (mat.HasProperty("_AutoRotateEnable")) mat.SetFloat("_AutoRotateEnable", 0f);
        mat.DisableKeyword("_ROTATE_TIME_ON");
        //关光照(轨迹走无光)
        if (mat.HasProperty("_LitEnable")) mat.SetFloat("_LitEnable", 0f);
        mat.DisableKeyword("_LIT_ON");
        //进透明队列，晚于不透明弹体绘制
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    /// <summary>
    /// 销毁轨迹桶运行时克隆的材质(战斗结束清理，避免泄漏)；Mesh 是弹体桶共享 Mesh，不在此销毁。
    /// </summary>
    private static void DestroyTrailBucket(TrailBucket tb)
    {
        if (tb == null)
            return;
        if (tb.material != null)
            Object.Destroy(tb.material);
    }
    #endregion

    #region 轨迹批量绘制（拖尾-方案1 Instanced）
    /// <summary>
    /// 把本帧各轨迹桶收集到的弹道按"年龄档"批量绘制：每档 = 所有弹道的第 k 个历史点，整档共享一个 alpha 一次 DrawMeshInstanced。
    /// <para>档 0 = 最靠近弹体(最新、最不透明 startAlpha)，档 trailNum-1 = 最远(最老、最透明 endAlpha)；由老到新绘制，使近处不透明档叠在远处透明档之上。</para>
    /// </summary>
    private void DrawTrailBuckets()
    {
        foreach (var tb in dicTrailBucket.Values)
        {
            var listAttackMode = tb.frameAttackModes;
            int amCount = listAttackMode.Count;
            if (amCount == 0 || tb.material == null || tb.mesh == null)
                continue;
            if (tb.mpb == null)
                tb.mpb = new MaterialPropertyBlock();

            //由老(远、透明)到新(近、不透明)绘制，保证近处不透明档覆盖远处透明档
            for (int k = tb.trailNum - 1; k >= 0; k--)
            {
                //本档 alpha：档 0=startAlpha，档 trailNum-1=endAlpha，线性插值
                float t = tb.trailNum > 1 ? (float)k / (tb.trailNum - 1) : 0f;
                Color c = tb.baseColor;
                c.a = Mathf.Lerp(tb.startAlpha, tb.endAlpha, t);
                tb.mpb.SetColor(PropBaseColor, c);

                //填本档所有弹道的第 k 个最新历史点(order = trailCount-1-k)，满批即绘
                tb.count = 0;
                for (int a = 0; a < amCount; a++)
                {
                    var attackMode = listAttackMode[a];
                    int order = attackMode.trailCount - 1 - k;
                    if (order < 0)
                        continue;   //历史点不足 k+1 个，本发本档跳过
                    //烤入该采样点的时间自转角，使旋转弹道的轨迹复现当时姿态(轨迹材质自旋已冻结)
                    tb.matrixBuffer[tb.count] = BuildInstanceMatrix(attackMode, attackMode.GetTrailPoint(order), attackMode.GetTrailSpinAngle(order));
                    tb.count++;
                    if (tb.count >= MaxInstancesPerBatch)
                    {
                        DrawTrailBatch(tb);
                        tb.count = 0;
                    }
                }
                if (tb.count > 0)
                {
                    DrawTrailBatch(tb);
                    tb.count = 0;
                }
            }
            listAttackMode.Clear();
        }
    }

    /// <summary>
    /// 用轨迹桶当前档的 MPB(已设该档 alpha)批量绘制其矩阵缓冲：透明、不投/不收阴影、不走光照探针。
    /// </summary>
    private void DrawTrailBatch(TrailBucket tb)
    {
        Graphics.DrawMeshInstanced(tb.mesh, 0, tb.material, tb.matrixBuffer, tb.count,
            tb.mpb, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off, null);
    }
    #endregion
}
