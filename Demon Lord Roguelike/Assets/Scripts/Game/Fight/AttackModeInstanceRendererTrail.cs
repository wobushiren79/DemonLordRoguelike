using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// <see cref="AttackModeInstanceRenderer"/> 的轨迹(拖尾)分部：只放**方案1(Instanced)**——本渲染器自绘的那种拖尾。
/// <para>【效果】克隆弹体桶材质、换成轨迹专用 shader，把弹体贴图画在若干历史位置上、越老越透明。颜色是桶级(整桶一色)。</para>
/// <para>【每桶每帧 1 次 draw】「年龄档透明度」是 shader 的**逐实例**属性(_TrailAlpha)，故"所有年龄档 × 所有弹道"
/// 填进同一个矩阵缓冲、一次 DrawMeshInstanced 画完(旧实现每档一次 draw，因为档 alpha 是 MPB 上的整批 uniform)。
/// 仅当单桶实例数(弹道数 × 档数)超过 1023 时才分批。⚠️叠加顺序靠"填充顺序 = 实例 ID 顺序 = 光栅化顺序"保证：
/// 必须仍按档由老到新填，近处不透明档才会叠在远处透明档之上。</para>
/// <para>【职责边界】本文件 = 轨迹桶结构 + 注册表字段 + 注册/绘制/清理；弹体桶、每帧入口 RenderAll、环境光补偿在主文件
/// AttackModeInstanceRenderer.cs，共享的 MaxInstancesPerBatch 与 BuildInstanceMatrix 亦在主文件(同一 partial 类内可直接访问)。
/// 方案2(Vfx)不在本类，归 <see cref="EffectHandler"/>——见 <see cref="RegisterTrailFromVisual"/> 的分流。</para>
/// </summary>
public partial class AttackModeInstanceRenderer
{
    #region 内部结构：轨迹桶
    /// <summary>
    /// 轨迹桶(拖尾-方案1 Instanced)：克隆自弹体桶材质的透明材质 + 复用弹体 Mesh + 本帧收集的启用弹道列表。
    /// <para>轨迹 = 弹体贴图画在历史位置上、越老越透明；所有年龄档合到一次 DrawMeshInstanced(档 alpha 走逐实例属性)。</para>
    /// </summary>
    private class TrailBucket
    {
        //轨迹材质(克隆弹体桶材质→换轨迹专用 shader；贴图/UV/宽高比/缩放随克隆按同名属性继承，基色已写入其 _BaseColor)
        public Material material;
        //轨迹网格(直接引用弹体桶的 sharedMesh，不拥有、销毁时勿 Destroy)
        public Mesh mesh;
        //轨迹档数(clamp 到 TrailMaxPoints)
        public int trailNum;
        //最靠近弹体(最新)一档的 alpha
        public float startAlpha;
        //最远(最老)一档的 alpha
        public float endAlpha;
        //本批绘制矩阵缓冲(固定 1023 复用；一批可含多个年龄档的实例)
        public readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxInstancesPerBatch];
        //缓冲当前已填充数量
        public int count;
        //承载逐实例 alpha 数组的 MPB(运行时懒建，规避 MonoBehaviour 构造期限制)
        public MaterialPropertyBlock mpb;
        //本帧收集到的启用轨迹弹道(复用 List，Clear 不释放容量)
        public readonly List<BaseAttackMode> frameAttackModes = new List<BaseAttackMode>();
    }
    #endregion

    #region 字段：轨迹桶注册表
    //轨迹桶注册表：key 与 dicBucket 同签名，value = 轨迹桶；仅 trail_data 启用且 type=Instanced 的视觉登记
    //(方案2 的桶不在这里——存于 EffectManager.dicAttackModeTrailVfx)
    private readonly Dictionary<string, TrailBucket> dicTrailBucket = new Dictionary<string, TrailBucket>();
    //轨迹基色写入用的 _BaseColor 属性 ID(注册期写进桶材质，整桶一色)
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    //逐实例年龄档 alpha 的 _TrailAlpha 属性 ID(每批经 MPB.SetFloatArray 灌入)
    private static readonly int PropTrailAlpha = Shader.PropertyToID("_TrailAlpha");

    //——轨迹专用 shader(见 Shader_Mesh_TrailInstanced_1.shader)：把年龄档 alpha 做成逐实例属性，使整桶所有档一次画完——
    //shader 内部名(Shader.Find 用)
    private const string TrailShaderName = "FrameWork/URP/MeshTrailInstanced1";
    //查找结果缓存(Shader.Find 是字符串查找，只在注册期做一次)；null=找不到，此时方案1 拖尾整体跳过
    private Shader trailShader;
    //已尝试查找过(缺失时避免每次注册都重查 + 重复刷屏报错)
    private bool triedFindTrailShader;

    //逐实例年龄档 alpha 缓冲(与 TrailBucket.matrixBuffer 同下标一一对应，跨桶复用；固定 1023 定长——
    //MPB 数组一经设定长度不宜再变，且实例数上限本就是 1023，故直接定长分配，热路径零分配)
    private readonly float[] trailAlphaBuffer = new float[MaxInstancesPerBatch];

    //——逐发基准矩阵缓存(DrawTrailBuckets 每帧重填，跨桶复用，按需扩容)——
    //无自旋弹道整发所有档的旋转/缩放完全相同、只有平移在变，故每帧每发只算一次 TRS，逐档改写平移列即可，
    //省下"档数×弹道数"次 Quaternion.AngleAxis/Matrix4x4.TRS 原生调用(二者均为 extern，是本函数原先的热点)
    private Matrix4x4[] trailBaseMatrix = new Matrix4x4[64];
    //对应下标弹道是否有自旋：有自旋则每档旋转姿态不同(要烤入采样时自转角)，不能复用基准矩阵
    private bool[] trailBaseHasSpin = new bool[64];
    #endregion

    #region 轨迹桶注册（拖尾）
    /// <summary>
    /// 为已注册的弹体桶(visualKey)派生轨迹：按 config.type 分流——Vfx 转交 <see cref="EffectHandler"/> 自管，Instanced 则克隆弹体桶材质、换成轨迹专用 shader 作轨迹材质。
    /// <para>方案1 的克隆按同名属性继承弹体材质的贴图/UV(_BaseMap_ST 图集子区域)/宽高比(_VertexScaleXY)/缩放(_VertexScale)，故轨迹与弹体同图同形、零额外拷贝。</para>
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
        //把它返回的桶数据挂到弹体桶上，RenderAll 逐发直接喂，免去按字符串键查 EffectHandler 的表
        if (config.type == AttackModeTrailType.Vfx)
        {
            vb.trailVfx = EffectHandler.Instance.RegisterAttackModeTrailVfx(visualKey);
            return;
        }
        //去重：该轨迹桶已建则只重新挂接引用，避免重复克隆材质
        //⚠️必须重挂而非直接 return：RegisterVisual(key,null,null) 会移除弹体桶但留下轨迹桶，之后重新注册会新建一个 trail 为空的 VisualBucket，
        //此时若跳过挂接，轨迹桶虽在表里却再也收不到采样点(拖尾静默消失)
        if (dicTrailBucket.TryGetValue(visualKey, out TrailBucket existTrail))
        {
            vb.trail = existTrail;
            return;
        }

        //轨迹 shader 缺失(未打进构建包)时整体跳过：弹体不受影响，只是没有拖尾
        Shader shaderTrail = GetTrailShader();
        if (shaderTrail == null)
            return;
        //克隆弹体材质(继承贴图/UV/宽高比/缩放)，再翻成轨迹材质
        Material mat = new Material(vb.material);
        //轨迹基色 = 弹体染色 × trail 染色(仅取 rgb，alpha 由逐实例年龄档决定)；须在换 shader 前读，取的是弹体材质的染色
        Color tint = mat.HasProperty(PropBaseColor) ? mat.GetColor(PropBaseColor) : Color.white;
        Color baseColor = new Color(tint.r * config.color.r, tint.g * config.color.g, tint.b * config.color.b, 1f);
        SetupTrailMaterial(mat, shaderTrail, baseColor);
        TrailBucket trailBucket = new TrailBucket
        {
            material = mat,
            mesh = vb.mesh,
            trailNum = Mathf.Clamp(config.count, 1, BaseAttackMode.TrailMaxPoints),
            startAlpha = config.startAlpha,
            endAlpha = config.endAlpha,
        };
        dicTrailBucket[visualKey] = trailBucket;
        vb.trail = trailBucket;
    }

    /// <summary>
    /// 取轨迹专用 shader(懒查一次并缓存)；找不到时报错一次并返回 null，调用方据此跳过方案1 拖尾。
    /// <para>⚠️该 shader **只被本类按名查找、没有任何材质资产引用它**，故必须加进
    /// ProjectSettings &gt; Graphics &gt; Always Included Shaders 才会被打进构建包——否则 Editor 里正常、进包 Find 返回 null。</para>
    /// </summary>
    private Shader GetTrailShader()
    {
        if (triedFindTrailShader)
            return trailShader;
        triedFindTrailShader = true;
        trailShader = Shader.Find(TrailShaderName);
        if (trailShader == null)
            LogUtil.LogError($"找不到轨迹 shader [{TrailShaderName}]，方案1(Instanced)拖尾不显示(弹体本体不受影响)。该 shader 仅被代码引用，须加进 ProjectSettings>Graphics>Always Included Shaders");
        return trailShader;
    }

    /// <summary>
    /// 把克隆自弹体桶的材质翻成轨迹材质：换轨迹专用 shader + 写入基色 + 关掉轨迹不该有的开关 + 进透明队列。
    /// <para>轨迹 shader 的参数集与 Shader_Mesh_Common_1 **完全一致**(它是后者的 drop-in 替身，多一个逐实例 _TrailAlpha)，
    /// 换 shader 时 Unity 按**属性名**保留同名属性值，故弹体材质的全部设定(贴图 _BaseMap、图集 UV 子区域 _BaseMap_ST、
    /// 宽高比 _VertexScaleXY、缩放 _VertexScale、静态角 _VertexRotation、描边…)零拷贝继承下来。</para>
    /// <para>⚠️正因为参数全量继承，**克隆过来的关键字也会一起生效**，故必须显式关掉下面三个——这不是可省的清理，是正确性要求：
    /// <list type="bullet">
    /// <item>_ROTATE_TIME_ON：每个采样点当时的自转角已由 C# 烤进实例矩阵，shader 再按 _Time 转一遍就是**转两遍**
    /// (骨头 200001 踩过：两套自旋互相抵消看似不转)。弹体材质普遍带此关键字(如 Mat_AttackModeVisual_RangedNormal 的 _RotateSpeed=(0,0,-360))。</item>
    /// <item>_ALPHATEST_ON：轨迹靠 alpha 渐隐(最老档 ~0.05)，硬裁剪阈值(默认 0.5)会把整条尾巴裁没。</item>
    /// <item>_LIT_ON：轨迹走无光(与历史表现一致)；且轨迹的 MPB 只灌 _TrailAlpha、不灌 _InstancedFlatGI，开 Lit 会缺一份环境光而偏暗。</item>
    /// </list>
    /// 描边 _OUTLINE_ON **不关**——随弹体材质继承(弹体有描边则轨迹也有，符合"轨迹是弹体贴图的快照")。</para>
    /// </summary>
    private static void SetupTrailMaterial(Material mat, Shader shaderTrail, Color baseColor)
    {
        mat.shader = shaderTrail;
        //基色整桶一份(alpha 交给逐实例 _TrailAlpha 按年龄档覆盖)
        mat.SetColor(PropBaseColor, baseColor);
        //表面类型=透明、渲染模式=标准 alpha 混合(SrcAlpha/OneMinusSrcAlpha)、不写深度
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        //关 Alpha 裁剪(轨迹靠半透明渐隐，不做硬镂空——开着会把渐隐档整档裁没)
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        //冻结自旋(轨迹是过去某刻的静态快照，自转角已烤进实例矩阵；不关就是转两遍)
        if (mat.HasProperty("_AutoRotateEnable")) mat.SetFloat("_AutoRotateEnable", 0f);
        mat.DisableKeyword("_ROTATE_TIME_ON");
        //关光照(轨迹走无光)
        if (mat.HasProperty("_LitEnable")) mat.SetFloat("_LitEnable", 0f);
        mat.DisableKeyword("_LIT_ON");
        //显式定队列：克隆自弹体的队列可能带自定义偏移，轨迹恒进透明队列、晚于不透明弹体绘制
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
    /// 把本帧各轨迹桶收集到的弹道绘制出来：**整桶所有年龄档 × 所有弹道合到一次 DrawMeshInstanced**，
    /// 每个实例的档 alpha 由逐实例属性 _TrailAlpha 承载(见轨迹 shader)。仅实例数超 1023 时才分批。
    /// <para>档 0 = 最靠近弹体(最新、最不透明 startAlpha)，档 trailNum-1 = 最远(最老、最透明 endAlpha)。</para>
    /// <para>⚠️**填充顺序即叠加顺序**：图形 API 保证单次实例化绘制内按实例 ID 顺序光栅化，故必须保持"由老到新"填充，
    /// 近处不透明档才会叠在远处透明档之上(轨迹不写深度、靠绘制先后定叠加)。这也是旧的逐档绘制顺序，表现逐位一致。</para>
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

            //预算每发的基准矩阵：无自旋弹道整发所有档同旋转同缩放，逐档只需换平移(见 trailBaseMatrix 注释)
            EnsureTrailBaseCache(amCount);
            for (int a = 0; a < amCount; a++)
            {
                var attackMode = listAttackMode[a];
                bool hasSpin = attackMode.spinSpeed != 0f;
                trailBaseHasSpin[a] = hasSpin;
                //有自旋的逐档现算(旋转随采样时刻变)，无自旋的在此算一次、平移留空待逐档覆盖
                if (!hasSpin)
                    trailBaseMatrix[a] = BuildInstanceMatrix(attackMode, Vector3.zero);
            }

            //由老(远、透明)到新(近、不透明)填进同一缓冲，满批才绘；不再逐档提交
            tb.count = 0;
            for (int k = tb.trailNum - 1; k >= 0; k--)
            {
                //本档 alpha：档 0=startAlpha，档 trailNum-1=endAlpha，线性插值；整档共用一个值，但逐实例写入
                float t = tb.trailNum > 1 ? (float)k / (tb.trailNum - 1) : 0f;
                float alpha = Mathf.Lerp(tb.startAlpha, tb.endAlpha, t);

                //填本档所有弹道的第 k 个最新历史点(order = trailCount-1-k)
                for (int a = 0; a < amCount; a++)
                {
                    var attackMode = listAttackMode[a];
                    int order = attackMode.trailCount - 1 - k;
                    if (order < 0)
                        continue;   //历史点不足 k+1 个，本发本档跳过
                    if (trailBaseHasSpin[a])
                    {
                        //有自旋：烤入该采样点的时间自转角，使旋转弹道的轨迹复现当时姿态(轨迹 shader 无时间自转)
                        attackMode.GetTrailSample(order, out Vector3 spinPoint, out float spinAngle);
                        tb.matrixBuffer[tb.count] = BuildInstanceMatrix(attackMode, spinPoint, spinAngle);
                    }
                    else
                    {
                        //无自旋：复用本发基准矩阵，只改写平移列(m03/m13/m23)，等价于同旋转同缩放下换个位置
                        Matrix4x4 matrix = trailBaseMatrix[a];
                        Vector3 point = attackMode.GetTrailPoint(order);
                        matrix.m03 = point.x;
                        matrix.m13 = point.y;
                        matrix.m23 = point.z;
                        tb.matrixBuffer[tb.count] = matrix;
                    }
                    trailAlphaBuffer[tb.count] = alpha;
                    tb.count++;
                    //超 1023 才分批(分批点可能落在档中间，但批次仍按顺序提交，叠加顺序不变)
                    if (tb.count >= MaxInstancesPerBatch)
                    {
                        DrawTrailBatch(tb);
                        tb.count = 0;
                    }
                }
            }
            if (tb.count > 0)
            {
                DrawTrailBatch(tb);
                tb.count = 0;
            }
            listAttackMode.Clear();
        }
    }

    /// <summary>
    /// 确保逐发基准矩阵缓存容量 &gt;= amCount；不足时按 2 的幂扩容(只增不减，跨帧跨桶复用，热路径零分配)。
    /// </summary>
    private void EnsureTrailBaseCache(int amCount)
    {
        if (trailBaseMatrix.Length >= amCount)
            return;
        int capacity = Mathf.NextPowerOfTwo(amCount);
        trailBaseMatrix = new Matrix4x4[capacity];
        trailBaseHasSpin = new bool[capacity];
    }

    /// <summary>
    /// 批量绘制轨迹桶当前缓冲的实例：先把逐实例年龄档 alpha 数组灌进 MPB，再一次 DrawMeshInstanced(透明、不投/不收阴影、不走光照探针)。
    /// <para>数组按定长 1023 整份上传(与 matrixBuffer 同下标)，超出 tb.count 的部分被忽略；shader 侧经 UNITY_ACCESS_INSTANCED_PROP 取本实例那一份。</para>
    /// </summary>
    private void DrawTrailBatch(TrailBucket tb)
    {
        tb.mpb.SetFloatArray(PropTrailAlpha, trailAlphaBuffer);
        Graphics.DrawMeshInstanced(tb.mesh, 0, tb.material, tb.matrixBuffer, tb.count,
            tb.mpb, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off, null);
    }
    #endregion
}
