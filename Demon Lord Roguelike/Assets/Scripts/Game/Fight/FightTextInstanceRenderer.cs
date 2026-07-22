using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 飘字(伤害数字)GPU Instancing 批量渲染器（DSP 式：与 <see cref="AttackModeInstanceRenderer"/> 同思路——"只记槽位，一起绘制"）。
/// <para>【核心】不再每条飘字一个 GameObject+TextMeshPro+DOTween，而是把每条飘字按字符拆成实例槽：
/// 诞生时 CPU 一次算好「字符世界锚点矩阵(含排版偏移+字符缩放) + 图集 UV + 颜色 + 出生时刻」填槽，
/// 此后每帧整批一次 Graphics.DrawMeshInstanced 画完，上浮/淡出/弹跳动画全部由 shader 用 _Time.y - _TextTime 时间驱动
/// (与弹体自旋 _RotateSpeed 同思路)，CPU 零每帧更新、零 TMP、零 DOTween、热路径零 GC。</para>
/// <para>【实例粒度 = 字符】一个实例 = 一个字符 quad，"12345" = 5 个实例。同屏上限 <see cref="MaxInstances"/> 个字符槽
/// (注意是字符粒度不是条数)，一次 draw call 画完(512 远低于 DrawMeshInstanced 单批 1023 硬限制)。槽满时丢弃新字符(保旧)：
/// 旧槽寿命将尽很快自然让位；若反向丢旧，在屏数字会半途凭空消失、比新数字出不来更刺眼。</para>
/// <para>【剔除】实例矩阵含真实世界位置，逐实例视锥剔除按矩阵×mesh.bounds 正常工作
/// (shader 内上浮仅 ~0.5 世界单位且锚点贴近被击者，剔除误差方向安全，可忽略)。</para>
/// <para>【图集制作约定(美术按此画 _BaseMap)】等分格子(行列数 = 材质面板 _AtlasCols/_AtlasRows，默认 4×4)，
/// 格序 = <see cref="atlasChars"/> 的字符序，第 0 格在图集【左上】、从左到右从上到下(UV 行序 shader 内已翻转)；
/// 字形格内居中、建议占格 ~80%；排版按等宽处理，字距由 <see cref="charSpacingRatio"/> 控制；
/// 字符表外的字符跳过不显示。⚠️格子总数(列×行)须 ≥ <see cref="atlasChars"/> 长度，否则表尾字符采错格子(CPU 不预检)。</para>
/// <para>【材质约定】shader 用 FrameWork/URP/MeshTextInstanced1；材质面板可调：图集贴图与行列数(_AtlasCols/_AtlasRows)、生命/上浮/淡出/弹跳参数、
/// ZTest(默认 LEqual 被场景遮挡，改 Always 则恒在最前)。本类逐实例只灌「格序索引+颜色+出生时刻」，UV 由 shader 按材质行列数解算，
/// 故行列数改动即时生效、无需重新 Setup。逐实例属性(_TextIndex/_TextColor/_TextTime)面板值无效。</para>
/// <para>【生命周期一致性】剔除过期槽用的 <see cref="lifeTime"/> 在 <see cref="Setup"/> 时从材质 _Lifetime 读一次缓存；
/// 运行期改材质的 _Lifetime 需重新 Setup 才同步。</para>
/// </summary>
public class FightTextInstanceRenderer
{
    #region 常量
    //字符槽上限(粒度是"字符"不是"条"——一条 5 位伤害数字占 5 槽)；远低于 DrawMeshInstanced 单批 1023 硬限制，单批一次 draw
    private const int MaxInstances = 512;
    #endregion

    #region 配置字段(图集与排版约定)
    //图集字符表：索引即图集格序(0='0' ... 9='9'，纯数字；表外字符跳过不显示)；
    //格子行列数由材质面板 _AtlasCols/_AtlasRows 决定(UV 在 shader 解算)，本表长度须 ≤ 列×行
    public string atlasChars = "0123456789";
    //字距系数：相邻字符锚点间距 = 字符缩放 × 本值
    public float charSpacingRatio = 1f;
    //普通飘字字符缩放(quad 边长的世界单位，字形实际高度 ≈ 本值 × 格内字形占比)
    public float textScaleNormal = 0.2f;
    //暴击飘字字符缩放
    public float textScaleCrit = 0.3f;
    #endregion

    #region 内部结构：字符槽
    /// <summary>
    /// 字符槽：一个待渲染字符的全部静态数据，诞生时一次算好，存活期不再改写(动画全在 shader 时间驱动)。
    /// </summary>
    private struct TextCharSlot
    {
        //TRS 矩阵：平移=字符世界锚点(含排版偏移)，均匀缩放=字符尺寸；无旋转(billboard 展开在 shader 做)
        public Matrix4x4 matrix;
        //图集格序索引(UV 由 shader 按材质 _AtlasCols/_AtlasRows 解算)
        public float charIndex;
        //rgb=染色，a=基础透明度(恒 1，留扩展)
        public Vector4 color;
        //出生时刻(Time.timeSinceLevelLoad 基准，与 shader _Time.y 同源)
        public float spawnTime;
    }
    #endregion

    #region 字段
    //渲染资源(Setup 注册)：Quad 网格 + instanced 材质；未注册时本渲染器零副作用
    private Mesh mesh;
    private Material material;
    //过期槽剔除用的生命时长：Setup 时从材质 _Lifetime 读一次缓存
    private float lifeTime = 1f;
    //活跃字符槽(无序，过期 swap-back 移除)
    private readonly List<TextCharSlot> listSlot = new List<TextCharSlot>(MaxInstances);
    //本帧绘制缓冲(定长复用，热路径零分配)
    private readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxInstances];
    private readonly float[] indexBuffer = new float[MaxInstances];
    private readonly Vector4[] colorBuffer = new Vector4[MaxInstances];
    private readonly float[] timeBuffer = new float[MaxInstances];
    //共享 MPB(承载逐实例数组；⚠️必须运行时懒建，禁止字段初始化器 new——同 AttackModeInstanceRenderer.sharedMPB 的 CreateImpl 教训)
    private MaterialPropertyBlock sharedMPB;
    //逐实例属性 ID(避免每帧字符串查找)
    private static readonly int PropTextIndex = Shader.PropertyToID("_TextIndex");
    private static readonly int PropTextColor = Shader.PropertyToID("_TextColor");
    private static readonly int PropTextTime = Shader.PropertyToID("_TextTime");
    private static readonly int PropLifeTime = Shader.PropertyToID("_Lifetime");
    //格子宽高比计算用的材质属性 ID
    private static readonly int PropBaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int PropAtlasCols = Shader.PropertyToID("_AtlasCols");
    private static readonly int PropAtlasRows = Shader.PropertyToID("_AtlasRows");
    //格子像素宽高比缓存(格宽/格高)：图集单格非正方形时的横向修正，ShowText 低频刷新(1 秒)，编辑器调材质即时可见
    private float cellAspect = 1f;
    private float cellAspectRefreshTime = -1f;
    //主相机缓存(排版右轴用；相机销毁后 Unity 假空自动触发重查)
    private static Camera cachedMainCam;
    #endregion

    #region 注册与清理
    /// <summary>
    /// 是否已注册网格与材质(未注册时 ShowText/RenderAll 零副作用)。
    /// </summary>
    public bool IsReady => mesh != null && material != null;

    /// <summary>
    /// 注册飘字渲染资源(Quad mesh + MeshTextInstanced1 材质)；重复调用为替换。
    /// <para>生命时长从材质 _Lifetime 读入缓存(剔除过期槽用)；运行期改材质 _Lifetime 需重新调用本方法同步。</para>
    /// </summary>
    public void Setup(Mesh mesh, Material material)
    {
        this.mesh = mesh;
        this.material = material;
        lifeTime = (material != null && material.HasProperty(PropLifeTime)) ? material.GetFloat(PropLifeTime) : 1f;
    }

    /// <summary>
    /// 清空活跃在屏字符槽(战斗结束调用)；渲染资源保留，跨场复用。
    /// </summary>
    public void Clear()
    {
        listSlot.Clear();
    }
    #endregion

    #region 飘字生成
    /// <summary>
    /// 生成一条飘字：按字符拆成实例槽(整条居中排版，字符锚点沿相机右轴排开)，动画由 shader 时间驱动，调用后即不管。
    /// </summary>
    /// <param name="basePos">飘字基准世界坐标(整条居中点)</param>
    /// <param name="text">显示文本(字符须收录于 atlasChars，未收录字符跳过)</param>
    /// <param name="color">染色(5 类颜色由调用方按类型给)</param>
    /// <param name="charScale">字符缩放(quad 边长世界单位)</param>
    /// <param name="randomPosOffset">基准位置随机偏移范围(与旧 TMP 方案一致)</param>
    public void ShowText(Vector3 basePos, string text, Color color, float charScale, float randomPosOffset = 0.2f)
    {
        if (!IsReady || string.IsNullOrEmpty(text))
            return;
        //排版右轴取相机世界右轴(roll=0 时恒水平)，使字符排列方向与 billboard 展开右轴严格平行
        if (cachedMainCam == null)
            cachedMainCam = Camera.main;
        Vector3 rightWS = cachedMainCam != null ? cachedMainCam.transform.right : Vector3.right;

        Vector3 anchorBase = RandomUtil.GetRandomVector3(basePos, randomPosOffset);
        float spawnTime = Time.timeSinceLevelLoad;
        //格子宽高比修正(1 秒刷新一次)：图集单格非正方形(如 8×12 像素)时，正方形 quad 会把字形横向拉伸，
        //故字符宽 = 字符高 × 格宽/格高，字距同步按修正后字宽排——缺贴图/参数时 cellAspect=1 不修正
        if (Time.timeSinceLevelLoad - cellAspectRefreshTime >= 1f)
        {
            cellAspectRefreshTime = Time.timeSinceLevelLoad;
            cellAspect = CalculateCellAspect();
        }
        float charWidth = charScale * cellAspect;
        float advance = charWidth * charSpacingRatio;
        //整条水平居中：首字符相对基准的偏移
        float startOffset = -(text.Length - 1) * advance * 0.5f;
        Vector4 colorV = new Vector4(color.r, color.g, color.b, color.a);

        int placed = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (!TryGetCharIndex(text[i], out int charIndex))
                continue; //字符未收录 → 跳过该字符(后续字符补位，排版略缩进，可接受)
            if (listSlot.Count >= MaxInstances)
                return; //槽满丢弃剩余字符(保旧，见类头注)
            TextCharSlot slot;
            slot.matrix = BuildCharMatrix(anchorBase + rightWS * (startOffset + placed * advance), charWidth, charScale);
            slot.charIndex = charIndex;
            slot.color = colorV;
            slot.spawnTime = spawnTime;
            listSlot.Add(slot);
            placed++;
        }
    }

    /// <summary>
    /// 计算图集单格像素宽高比(格宽/格高)：读材质 _BaseMap 贴图导入尺寸(已含 NPOT 缩放)与 _AtlasCols/_AtlasRows。
    /// 非正方形格子时供横向缩放修正，使字形按图集原比例显示；缺贴图/参数时回退 1(不修正)。
    /// </summary>
    private float CalculateCellAspect()
    {
        if (material == null)
            return 1f;
        float cols = material.HasProperty(PropAtlasCols) ? material.GetFloat(PropAtlasCols) : 4f;
        float rows = material.HasProperty(PropAtlasRows) ? material.GetFloat(PropAtlasRows) : 4f;
        Texture tex = material.HasProperty(PropBaseMap) ? material.GetTexture(PropBaseMap) : null;
        if (tex == null || cols <= 0f || rows <= 0f)
            return 1f;
        return (tex.width / cols) / (tex.height / rows);
    }

    /// <summary>
    /// 取字符的图集格序索引；字符未收录于 atlasChars 时返回 false(该字符跳过不显示)。
    /// <para>索引对应的 UV 格子由 shader 按材质 _AtlasCols/_AtlasRows 解算(行列数改动即时生效，CPU 不参与)；
    /// ⚠️格子总数(列×行)须 ≥ atlasChars 长度，本方法不预检(越界会采到错误格子)。</para>
    /// </summary>
    private bool TryGetCharIndex(char c, out int index)
    {
        index = atlasChars.IndexOf(c);
        return index >= 0;
    }

    /// <summary>
    /// 构建字符实例矩阵(位置 + XY 各自缩放，无旋转)：X=字符宽(含格子宽高比修正)，Y=字符高。
    /// 与 AttackModeInstanceRenderer.BuildInstanceMatrix 快速路径同理，直接填对角矩阵，跳过 Matrix4x4.TRS 原生调用。
    /// </summary>
    private static Matrix4x4 BuildCharMatrix(Vector3 position, float scaleX, float scaleY)
    {
        Matrix4x4 matrix = default;
        matrix.m00 = scaleX;
        matrix.m11 = scaleY;
        matrix.m22 = 1f;
        matrix.m33 = 1f;
        matrix.m03 = position.x;
        matrix.m13 = position.y;
        matrix.m23 = position.z;
        return matrix;
    }
    #endregion

    #region 每帧渲染入口
    /// <summary>
    /// 每帧调用(FightHandler.Update)：剔除寿终槽 → 整批填充绘制缓冲 → 一次 DrawMeshInstanced 画完所有在屏字符。
    /// <para>未 Setup 网格/材质时零副作用直接返回；无活跃槽时只剔除不绘制。</para>
    /// </summary>
    public void RenderAll()
    {
        if (!IsReady)
            return;
        //剔除寿终槽(swap-back 无序移除；shader 端另有 age≥_Lifetime 塌缩保险丝兜底)
        float now = Time.timeSinceLevelLoad;
        for (int i = listSlot.Count - 1; i >= 0; i--)
        {
            if (now - listSlot[i].spawnTime >= lifeTime)
            {
                int last = listSlot.Count - 1;
                listSlot[i] = listSlot[last];
                listSlot.RemoveAt(last);
            }
        }
        int count = listSlot.Count;
        if (count == 0)
            return;
        //填充本帧绘制缓冲(槽数据全静态，纯拷贝)
        for (int i = 0; i < count; i++)
        {
            TextCharSlot slot = listSlot[i];
            matrixBuffer[i] = slot.matrix;
            indexBuffer[i] = slot.charIndex;
            colorBuffer[i] = slot.color;
            timeBuffer[i] = slot.spawnTime;
        }
        //懒建 MPB(禁止字段初始化器 new，见字段注释)
        if (sharedMPB == null)
            sharedMPB = new MaterialPropertyBlock();
        //数组按定长 512 整份上传(超出 count 的部分被忽略)，与弹体桶 _VelocityWS 同理
        sharedMPB.SetFloatArray(PropTextIndex, indexBuffer);
        sharedMPB.SetVectorArray(PropTextColor, colorBuffer);
        sharedMPB.SetFloatArray(PropTextTime, timeBuffer);
        //单批一次 draw；不投阴影不受影(同弹体桶——满屏飘字的阴影无意义还多走一遍 ShadowCaster Pass)
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixBuffer, count,
            sharedMPB, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off, null);
    }
    #endregion
}
