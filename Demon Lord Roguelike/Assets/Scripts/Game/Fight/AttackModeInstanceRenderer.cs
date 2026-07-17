using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 攻击模块(弹道)GPU Instancing 批量渲染器（DSP 式："只记录位置，一起绘制"）。
/// <para>【核心】不为每发弹道单独渲染，而是每帧遍历活跃弹道、按视觉桶签名分桶，用各弹道的 position 构建变换矩阵，每桶一次 Graphics.DrawMeshInstanced 批量绘制。</para>
/// <para>【双通道】visual_name 走本批量渲染(DSP)；prefab_name 仍走原 prefab 的 SpriteRenderer/VisualEffect
/// (FightManager.GetAttackModePrefab 创建)。二者独立字段、配置侧二选一，互不干扰。</para>
/// <para>【零副作用】常开无总开关：visual_name 为空、或未 RegisterVisual 的弹道被跳过(什么都不画)。</para>
/// <para>【轨迹(拖尾)】由 trail_data 的 type 键选渲染方式，两者均需配合 visual_name 走 DSP：
/// ①Instanced(type=1，默认)：弹体贴图画在若干历史位置上、越老越透明(类似冲刺残影)，逐"年龄档"一次 DrawMeshInstanced。
/// **本类自绘**，细节见分部文件 AttackModeInstanceRendererTrail.cs。
/// ②Vfx(type=2)：单个 GPU VFX 特效在各子弹位置喷射各自颜色的粒子，全部粒子合一 draw call。**本类不实现**——
/// VFX 实例/参数/缓冲全归 <see cref="EffectHandler"/> 的「攻击弹道拖尾粒子」区(与血液/护盾粒子同一分工：调用方只给语义数据)，
/// 本类仅每帧把「位置 + 本发染色」报给它。</para>
/// <para>【多发弹道】一发弹道=一个位置，故"一次攻击打出多发"的类型(如分裂弹)须拆成多个独立 BaseAttackMode 才能纳入本渲染器：
/// AttackModeRangedSplit 已改为纯发射器、逐条道路发射独立的 AttackModeRangedSplitChild，各子弹与普通弹道一样有视觉桶与轨迹。
/// 新增同类弹道请照此办理，勿在单个 AttackMode 里自管多个 GameObject。</para>
/// <para>【拆分】本类为 partial：弹体桶 / 每帧入口 RenderAll / 环境光补偿在本文件，方案1 轨迹在 AttackModeInstanceRendererTrail.cs。</para>
/// </summary>
public partial class AttackModeInstanceRenderer
{
    #region 常量
    //Graphics.DrawMeshInstanced 单批矩阵上限(Unity 硬限制 1023)
    private const int MaxInstancesPerBatch = 1023;
    //弹体是否投阴影：满屏子弹的阴影几乎看不出，却让每个弹体桶多走一遍 ShadowCaster Pass(draw call 翻倍)，故默认关；想恢复原表现改回 On 即可
    private const ShadowCastingMode BodyShadowCasting = ShadowCastingMode.Off;
    //弹体是否接收阴影(与 BodyShadowCasting 配套：关阴影时一并关掉受影，省 Forward Pass 的阴影采样)
    private const bool BodyReceiveShadows = false;
    //世界速度矢量的瞬移钳制倍率：帧差分速度超过「弹道理论速度 GetMoveSpeed × 本倍率」即判定为瞬移(SetPosition 跳位)，本帧速度取 0
    //(留 1.5 倍余量是给抛物线弹的重力分量：其实际速度本就大于配置的水平速度，严格按 1 倍卡会把正常弹道误判成瞬移)
    private const float VelocityClampRate = 1.5f;
    #endregion

    #region 内部结构：渲染桶
    /// <summary>
    /// 弹体视觉桶：一个 Mesh + 一个(开 GPU Instancing 的)Material + 复用矩阵缓冲。同 visualKey 的弹道合批到此。
    /// </summary>
    private class VisualBucket
    {
        //弹体网格(通常是朝相机的 Quad)
        public Mesh mesh;
        //弹体材质(须开 Enable GPU Instancing 或用支持 instancing 的 shader)
        public Material material;
        //本帧待绘制矩阵缓冲(固定 1023 复用，避免热路径分配)
        public readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxInstancesPerBatch];
        //缓冲当前已填充数量
        public int count;
        //——逐实例属性(仅"桶材质声明了 _VelocityWS"的桶启用，如火球/冰球；其余桶两个缓冲恒为 null，不占内存也不进热路径)——
        //本桶是否需要逐实例世界速度+种子：注册期按材质有无 _VelocityWS 判定一次(见 RegisterVisual)
        public bool hasVelocity;
        //本帧待上传的逐实例世界速度矢量(与 matrixBuffer 同下标；定长 1023 复用，热路径零分配)
        public Vector4[] velocityBuffer;
        //本帧待上传的逐实例种子偏移(同上)：不灌则同屏所有火球的火星同一帧同时爆同时灭(_Time.y 是全局的)
        public float[] seedBuffer;
        //——热路径直连：同 key 的拖尾桶引用挂在本桶上，RenderAll 逐发只查一次 dicBucket，不再按字符串键二次查拖尾表——
        //方案1(Instanced)轨迹桶(未派生轨迹为空)；由 RegisterTrailFromVisual 建桶时回填
        public TrailBucket trail;
        //方案2(Vfx)轨迹的 VFX 数据(未派生为空)；桶实例归 EffectHandler 持有/销毁，本类只借引用喂数据
        public AttackModeTrailVfxBean trailVfx;
    }
    #endregion

    #region 字段
    //弹体桶注册表：key = 视觉桶签名(BuildVisualBucketKey，默认桶即 visual_name)，value = 弹体桶
    private readonly Dictionary<string, VisualBucket> dicBucket = new Dictionary<string, VisualBucket>();
    //方案1 轨迹桶 TrailBucket / dicTrailBucket / PropBaseColor 在分部文件 AttackModeInstanceRendererTrail.cs

    //——环境光(GI)补偿：DrawMeshInstanced 的 SampleSH 读不到全局环境探针，开 Lit 的桶材质会偏暗；把探针求平坦 GI 经 MPB 灌进 shader 补齐(详见 RefreshAmbientSH)——
    //共享 MPB(承载 _InstancedFlatGI；⚠️必须运行时懒建，禁止字段初始化器 new——本类由 MonoBehaviour 构造期创建会触发 CreateImpl 异常)
    private MaterialPropertyBlock sharedMPB;
    //_InstancedFlatGI 属性 ID(避免每帧字符串查找)
    private static readonly int PropInstancedFlatGI = Shader.PropertyToID("_InstancedFlatGI");
    //自旋写入用的 shader 参数：_RotateSpeed 属性 ID + 时间自转关键字(与本文件其它属性一致走 ID，不用字符串字面量)
    private static readonly int PropRotateSpeed = Shader.PropertyToID("_RotateSpeed");
    private const string KeywordRotateTimeOn = "_ROTATE_TIME_ON";
    //——逐实例属性 ID(火球/冰球等自带世界化特效的桶专用，见 VisualBucket.hasVelocity)——
    //_VelocityWS：本发的世界速度矢量(方向×速率，单位/秒)。通用属性非火星专用：shader 拿它反推"特效出生时弹体在哪"，
    //使火星脱离弹体留在世界空间(不灌=火星刚性挂在弹体上跟着平移)。详见 Shader_Mesh_FireBallInstanced_1 的火星世界化⚠️
    private static readonly int PropVelocityWS = Shader.PropertyToID("_VelocityWS");
    //_SeedOffset：本发的随机相位(复用 spinPhase)，让各发火星生灭错峰
    private static readonly int PropSeedOffset = Shader.PropertyToID("_SeedOffset");
    //环境探针求值用的固定采样方向(6 轴)与结果缓冲(预分配复用，避免热路径分配)
    private static readonly Vector3[] ambientEvalDirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    private readonly Color[] ambientEvalResults = new Color[6];
    //上次灌入 MPB 的环境探针，仅在变化时重求值+SetVector；默认(全 0)保证首帧必写
    private SphericalHarmonicsL2 appliedAmbient = default;
    #endregion

    #region 弹体桶注册
    /// <summary>
    /// 注册/替换某签名(visualKey)的弹体桶；mesh 或 material 为空视为取消该桶的 instancing 渲染。
    /// <para>换图 sprite 的宽高比修正由桶材质 shader 的 _VertexScaleXY(对象空间 XY 缩放)在注册前写好，此处不涉及。</para>
    /// <para>自旋在此一次性写进桶材质：桶签名已按 spinAxis×spinSpeed 细分(见 <see cref="BuildVisualBucketKey"/>)，
    /// 故整桶自旋恒定、注册期写死即可，无需每帧逐发重写。基础桶(key=visual_name)按签名规则必然无自旋，用默认值 0 即对。</para>
    /// </summary>
    /// <param name="spinAxis">该桶弹道的自旋轴(与 visualKey 里编码的一致)</param>
    /// <param name="spinSpeed">该桶弹道的自旋速度(度/秒)；0=不自旋</param>
    public void RegisterVisual(string visualKey, Mesh mesh, Material material, Vector3 spinAxis = default, float spinSpeed = 0f)
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
        ApplyBucketSpin(material, spinAxis, spinSpeed);
        //逐实例世界速度：只有声明了 _VelocityWS 的材质(火球/冰球)才启用并分配缓冲，其余桶零内存零热路径开销
        bucket.hasVelocity = material.HasProperty(PropVelocityWS);
        if (bucket.hasVelocity && bucket.velocityBuffer == null)
        {
            bucket.velocityBuffer = new Vector4[MaxInstancesPerBatch];
            bucket.seedBuffer = new float[MaxInstancesPerBatch];
        }
    }

    /// <summary>
    /// 注销某签名的弹体桶(该类型退回原 prefab 渲染)，并同步移除其轨迹：方案1 销毁克隆材质，方案2 转交 EffectHandler 销毁 VFX 实例。
    /// </summary>
    public void UnregisterVisual(string visualKey)
    {
        if (string.IsNullOrEmpty(visualKey))
            return;
        //先断开弹体桶对两条轨迹的直连引用，再移除桶(桶对象可能仍被本帧局部变量持有，留着引用即悬垂)
        if (dicBucket.TryGetValue(visualKey, out VisualBucket vb))
        {
            vb.trail = null;
            vb.trailVfx = null;
        }
        dicBucket.Remove(visualKey);
        if (dicTrailBucket.TryGetValue(visualKey, out TrailBucket tb))
        {
            DestroyTrailBucket(tb);
            dicTrailBucket.Remove(visualKey);
        }
        //方案2(VFX)的桶由 EffectHandler 持有，交它销毁(无该桶时内部安全跳过)
        EffectHandler.Instance.ClearAttackModeTrailVfx(visualKey);
    }

    /// <summary>
    /// 清空所有桶注册（方案1 轨迹桶销毁克隆材质，轨迹 Mesh 是弹体桶共享 Mesh 不销毁；方案2 VFX 桶交 EffectHandler 统一销毁）。
    /// </summary>
    public void ClearVisuals()
    {
        dicBucket.Clear();
        if (dicTrailBucket.Count > 0)
        {
            foreach (var tb in dicTrailBucket.Values)
                DestroyTrailBucket(tb);
            dicTrailBucket.Clear();
        }
        //方案2(VFX)的桶全部由 EffectHandler 持有并销毁(实例+缓冲)，本渲染器不持有
        EffectHandler.Instance.ClearAllAttackModeTrailVfx();
    }

    /// <summary>
    /// 是否已注册任意弹体桶。
    /// </summary>
    public bool HasAnyVisual => dicBucket.Count > 0;

    /// <summary>
    /// 指定签名是否已注册弹体桶；供调用方懒注册去重，避免重复加载。
    /// </summary>
    public bool HasVisual(string visualKey)
    {
        return !string.IsNullOrEmpty(visualKey) && dicBucket.ContainsKey(visualKey);
    }

    /// <summary>
    /// 生成弹道视觉桶签名：按「会影响桶共享材质的视觉差异」(换图 sprite + 自旋 spinAxis×spinSpeed)对 visual_name 细分子桶。
    /// <para>无换图且无自旋=默认桶，直接返回 visual_name(与基础桶同 key、复用基材质)；有覆盖项才拼子桶签名，
    /// 使不同贴图/不同自旋各占独立子桶(各自克隆材质)，桶内仍 GPU Instancing 合批。仅发射时调用(非热路径)。</para>
    /// </summary>
    public static string BuildVisualBucketKey(string visualName, string spriteName, Vector3 spinAxis, float spinSpeed)
    {
        if (string.IsNullOrEmpty(visualName))
            return null;
        bool hasSprite = !string.IsNullOrEmpty(spriteName);
        bool hasSpin = spinSpeed != 0f;
        if (!hasSprite && !hasSpin)
            return visualName;
        string key = visualName;
        if (hasSprite)
            key += "|s=" + spriteName;
        if (hasSpin)
        {
            //自旋并入 spinAxis×spinSpeed 乘积向量(与 ApplyBucketSpin 一致)，两配置乘积相同即归同一子桶
            Vector3 r = spinAxis * spinSpeed;
            key += "|r=" + r.x.ToString("F2") + "," + r.y.ToString("F2") + "," + r.z.ToString("F2");
        }
        return key;
    }
    #endregion

    #region 每帧渲染入口
    /// <summary>
    /// 每帧调用：遍历活跃弹道，按签名分桶收集 position 矩阵并批量绘制；方案1 轨迹的弹道额外采样历史点、收进轨迹桶后按年龄档绘制；方案2 轨迹的弹道把位置+染色报给 EffectHandler。
    /// <para>visual_name 为空或未注册桶的弹道被跳过(不画，交由原 prefab 渲染)；唯一的早退条件是"无任何弹体桶"。</para>
    /// <para>⚠️**弹道列表为空时也必须执行到底**：VFX 轨迹的 Begin→Flush 要把喷发数归零，否则子弹死光后 VFX 会在残留位置持续喷粒子。
    /// 故勿在此加 listAttackMode 为空的早退。</para>
    /// </summary>
    public void RenderAll(List<BaseAttackMode> listAttackMode)
    {
        if (dicBucket.Count == 0)
            return;

        //1) 同步全局环境光到共享 MPB(仅探针变化时真正重填)，供各桶实例化绘制注入 SH
        RefreshAmbientSH();

        //2) VFX 轨迹(方案2)帧初始化：清空 EffectHandler 各 VFX 桶本帧收集缓冲(本帧无弹道时也要走，见方法注释)
        EffectHandler effectHandler = EffectHandler.Instance;
        effectHandler.BeginAttackModeTrailVfxFrame();

        //3) 轨迹帧初始化：有轨迹桶时取当前时刻(用 timeSinceLevelLoad 与 shader _Time.y 同基准，使轨迹自旋角与弹体连续)，并清空各桶本帧收集列表
        bool hasTrail = dicTrailBucket.Count > 0;
        float trailNow = 0f;
        if (hasTrail)
        {
            trailNow = Time.timeSinceLevelLoad;
            foreach (var tb in dicTrailBucket.Values)
                tb.frameAttackModes.Clear();
        }

        //4) 收集：把每发弹道的位置矩阵填入对应桶缓冲，满批即刻绘制并清零；启用轨迹的弹道顺带采样+收集
        //逐实例世界速度用的帧时长(只取一次)：暂停帧(deltaTime=0)时 CalculateVelocityWS 内部按 0 处理，不除零
        float deltaTime = Time.deltaTime;
        int count = listAttackMode == null ? 0 : listAttackMode.Count;
        for (int i = 0; i < count; i++)
        {
            var itemAttackMode = listAttackMode[i];
            if (itemAttackMode == null || !itemAttackMode.isValid || itemAttackMode.attackModeInfo == null)
                continue;
            //取预算好的视觉桶签名(visual_name + 换图 + 自旋)，支持逐弹换图/自旋分桶
            //⚠️本帧对本发唯一的一次字符串键查找：桶签名长、哈希按长度计费，故拖尾两条路都改走桶上的直连引用(bucket.trail / bucket.trailVfx)而非再查表
            string visualKey = itemAttackMode.visualBucketKey;
            if (string.IsNullOrEmpty(visualKey) || !dicBucket.TryGetValue(visualKey, out VisualBucket bucket))
                continue;

            //轨迹：按间隔采样当前位置，并把本发收进对应轨迹桶(绘制延到收尾按年龄档批量画)
            if (hasTrail && itemAttackMode.trailMode == AttackModeTrailType.Instanced && bucket.trail != null)
            {
                itemAttackMode.SampleTrail(trailNow);
                if (itemAttackMode.trailCount >= 1)
                    bucket.trail.frameAttackModes.Add(itemAttackMode);
            }

            //VFX 轨迹(方案2)：只把"本发的位置+自身染色"报给 EffectHandler，粒子实例/缓冲/上传全由它自管(本渲染器不碰 VFX)
            if (itemAttackMode.trailMode == AttackModeTrailType.Vfx)
                effectHandler.AddAttackModeTrailVfxPoint(bucket.trailVfx, itemAttackMode.position, itemAttackMode.trailColor);

            //逐实例世界速度+种子(仅火球/冰球这类桶)：须在 count++ 前填，与本发矩阵同下标
            if (bucket.hasVelocity)
            {
                bucket.velocityBuffer[bucket.count] = CalculateVelocityWS(itemAttackMode, deltaTime);
                //种子直接复用每发随机自旋相位(0~360)：shader 侧 frac 只取小数部分，随机性原样保留，无需再加字段
                bucket.seedBuffer[bucket.count] = itemAttackMode.spinPhase;
            }
            //填本发弹体矩阵(自旋已在注册期写进桶材质，此处不再逐发重写)
            bucket.matrixBuffer[bucket.count] = BuildInstanceMatrix(itemAttackMode, itemAttackMode.position);
            bucket.count++;
            if (bucket.count >= MaxInstancesPerBatch)
            {
                DrawBucket(bucket);
                bucket.count = 0;
            }
        }

        //5) 弹体收尾：绘制各桶剩余不足一批的矩阵并清零
        foreach (var bucket in dicBucket.Values)
        {
            if (bucket.count > 0)
            {
                DrawBucket(bucket);
                bucket.count = 0;
            }
        }

        //6) 轨迹收尾：把本帧收集到的各轨迹桶按年龄档批量绘制
        if (hasTrail)
            DrawTrailBuckets();

        //7) VFX 轨迹(方案2)收尾：交 EffectHandler 把本帧收集到的位置+逐弹染色上传给 VFX 驱动喷射(本帧无子弹时它会把喷发数归零)
        effectHandler.FlushAttackModeTrailVfxFrame();
    }
    #endregion

    #region 弹体批量绘制
    /// <summary>
    /// 构建单发弹道的 per-instance 变换矩阵(位置 + 起始角 + 自旋角 + 缩放)。弹体与轨迹共用，保证轨迹与弹体同姿态。
    /// <para>弹体本体传 extraSpinAngle=0：时间自转由桶材质 shader 驱动(_RotateSpeed)，矩阵只含起始角 + per-instance 相位。
    /// 轨迹(轨迹材质冻结自旋)传该采样点的时间自转角，把当时的旋转姿态烤进矩阵，使旋转弹道(如骷髅骨头)的轨迹复现旋转。
    /// 缩放：visualScale&gt;=0 用武器配置，&lt;0(未配置)取1、实际大小交桶材质 _VertexScale；换图宽高比修正由材质 _VertexScaleXY(对象空间)处理。</para>
    /// </summary>
    private static Matrix4x4 BuildInstanceMatrix(BaseAttackMode attackMode, Vector3 position, float extraSpinAngle = 0f)
    {
        float scale = attackMode.visualScale >= 0f ? attackMode.visualScale : 1f;
        bool hasStartAngle = attackMode.visualStartAngle != 0f;
        //有自旋时叠加(每发随机相位 + 传入的时间自转角)，绕自旋轴：弹体本体 extra=0(只相位、时间自转交 shader)，轨迹 extra=采样时自转角
        float spinAngle = attackMode.spinSpeed != 0f ? attackMode.spinPhase + extraSpinAngle : 0f;
        //无旋转的快速路径(不配起始角且不自旋，最常见)：直接拼对角矩阵，跳过 Quaternion.AngleAxis + Matrix4x4.TRS 两个原生调用
        //(二者均为 extern，托管↔原生穿越按每发每帧计费)；结果与 TRS(position, identity, one*scale) 逐位等价
        if (!hasStartAngle && spinAngle == 0f)
        {
            Matrix4x4 fastMatrix = default;   //Matrix4x4 是 16 个 float 的结构体，default 即全 0
            fastMatrix.m00 = scale;
            fastMatrix.m11 = scale;
            fastMatrix.m22 = scale;
            fastMatrix.m33 = 1f;
            fastMatrix.m03 = position.x;
            fastMatrix.m13 = position.y;
            fastMatrix.m23 = position.z;
            return fastMatrix;
        }
        Quaternion rot = hasStartAngle ? Quaternion.AngleAxis(attackMode.visualStartAngle, Vector3.forward) : Quaternion.identity;
        if (spinAngle != 0f)
            rot *= Quaternion.AngleAxis(spinAngle, attackMode.spinAxis);
        return Matrix4x4.TRS(position, rot, Vector3.one * scale);
    }

    /// <summary>
    /// 算单发弹道当前的世界速度矢量(方向×速率，单位/秒)，供 shader 的 _VelocityWS 反推特效出生点(火星世界化)；顺带推进本发的差分基准。
    /// <para>【为什么帧差分而不问弹道要】差的是"本帧位移 ÷ 帧时长" = 本帧平均速度，直线弹/追踪弹/抛物线弹全部自动正确，
    /// 无需每种 AttackMode 各自暴露飞行方向再各自算对一遍(拐弯与下坠的分量差分天然含进去了)。</para>
    /// <para>【瞬移钳制】<see cref="BaseAttackMode.SetPosition"/> 可瞬间改位置，差分会算出荒谬速度把火星甩到天边；
    /// 故超过「理论速度 × <see cref="VelocityClampRate"/>」即判为瞬移、本帧速度取 0(退化成"火星挂在弹体上"仅一帧，不可察)。
    /// 速度为 0 的原地弹道(speed_move=0)同样返回 0——它本就不该有拖拽。</para>
    /// </summary>
    private static Vector4 CalculateVelocityWS(BaseAttackMode attackMode, float deltaTime)
    {
        Vector3 delta = attackMode.position - attackMode.lastRenderPosition;
        //基准无条件推进：即便本帧速度被判瞬移丢弃，也不能让基准停在旧位置(否则下一帧会把这段位移二次计入)
        attackMode.lastRenderPosition = attackMode.position;
        //暂停帧不除零；理论速度<=0 的原地弹道直接退化为旧行为
        float moveSpeed = attackMode.GetMoveSpeed();
        if (deltaTime <= 0f || moveSpeed <= 0f)
            return Vector4.zero;
        Vector3 velocity = delta / deltaTime;
        float maxSpeed = moveSpeed * VelocityClampRate;
        //超速判瞬移(比较平方值，省一次开方)
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            return Vector4.zero;
        return new Vector4(velocity.x, velocity.y, velocity.z, 0f);
    }

    /// <summary>
    /// 把弹道自旋(spinAxis×spinSpeed，每轴 度/秒)写入桶材质的 shader 自转参数(_RotateSpeed + _ROTATE_TIME_ON)；spinSpeed=0 时关闭自转。
    /// <para>仅在 <see cref="RegisterVisual"/> 注册期调用一次(整桶自旋恒定，见其注释)，故无需缓存"上次写入值"做变化检测。</para>
    /// </summary>
    private static void ApplyBucketSpin(Material material, Vector3 spinAxis, float spinSpeed)
    {
        if (material == null)
            return;
        if (spinSpeed != 0f)
        {
            material.EnableKeyword(KeywordRotateTimeOn);
            //每轴度/秒：方向由 spinAxis 符号承载(如 (0,0,-1)×360 = 绕 -Z)，材质 _RotateDirection 保持正向
            material.SetVector(PropRotateSpeed, spinAxis * spinSpeed);
        }
        else
        {
            material.DisableKeyword(KeywordRotateTimeOn);
        }
    }

    /// <summary>
    /// 用携带平坦环境光的共享 MPB 批量绘制单个弹体桶当前缓冲的实例。
    /// <para>MPB 的 _InstancedFlatGI 补齐 Lit 材质在实例化绘制下缺失的环境光，使亮度与预制 MeshRenderer 一致；
    /// 不走光照探针(LightProbeUsage.Off，自定义 shader 未启用逐实例 SH)；阴影投射/接收见 <see cref="BodyShadowCasting"/>(默认关，省一遍 ShadowCaster Pass)。</para>
    /// <para>【逐实例数组现灌现画】火球/冰球桶的 _VelocityWS/_SeedOffset 在此刻才灌进 <b>共享</b> MPB：Set*Array 是即时拷贝、
    /// 紧接着就提交绘制，故各桶轮流借用同一个 MPB 不会串数据，无需每桶再建 MPB(那样反而要为每个新 MPB 同步补灌 _InstancedFlatGI，
    /// 漏灌即偏暗)。必须每桶一份的只有缓冲数组本身——各桶的填充是交错进行的。
    /// 数组按定长 1023 整份上传(超出 count 的部分被忽略)，与轨迹桶的 _TrailAlpha 同理；未声明这两个属性的桶提交时 Unity 直接忽略残留数组。</para>
    /// </summary>
    private void DrawBucket(VisualBucket bucket)
    {
        if (bucket.hasVelocity)
        {
            sharedMPB.SetVectorArray(PropVelocityWS, bucket.velocityBuffer);
            sharedMPB.SetFloatArray(PropSeedOffset, bucket.seedBuffer);
        }
        Graphics.DrawMeshInstanced(bucket.mesh, 0, bucket.material, bucket.matrixBuffer, bucket.count,
            sharedMPB, BodyShadowCasting, BodyReceiveShadows, 0, null, LightProbeUsage.Off, null);
    }
    #endregion

    #region 环境光补偿（Lit 亮度对齐）
    /// <summary>
    /// 把全局环境探针(RenderSettings.ambientProbe)求成一份平坦 GI 颜色，灌进共享 MPB 的 _InstancedFlatGI。
    /// <para>背景：DrawMeshInstanced 的 SampleSH 读不到环境探针 → 开 Lit 的桶材质比预制 MeshRenderer 偏暗一份环境光；
    /// 探针在 6 轴求值取平均得平坦近似(billboard 法线近恒定)，Lit 分支把它加回反照率上。仅探针变化时重求值+SetVector，静态场景每帧只做一次结构体比较。</para>
    /// </summary>
    private void RefreshAmbientSH()
    {
        //懒建 MPB：只在运行时(RenderAll 首帧)创建，规避 MonoBehaviour 构造期 CreateImpl 限制；在早退检查前，保证 DrawBucket 拿到非空 MPB
        if (sharedMPB == null)
            sharedMPB = new MaterialPropertyBlock();
        SphericalHarmonicsL2 ambient = RenderSettings.ambientProbe;
        if (ambient == appliedAmbient)
            return;
        appliedAmbient = ambient;
        //6 轴求值取平均 → 平坦环境光，写进 MPB 的 _InstancedFlatGI
        ambient.Evaluate(ambientEvalDirs, ambientEvalResults);
        Color avg = default;
        for (int i = 0; i < ambientEvalResults.Length; i++)
            avg += ambientEvalResults[i];
        avg /= ambientEvalResults.Length;
        sharedMPB.SetVector(PropInstancedFlatGI, new Vector4(avg.r, avg.g, avg.b, 0f));
    }
    #endregion
}
