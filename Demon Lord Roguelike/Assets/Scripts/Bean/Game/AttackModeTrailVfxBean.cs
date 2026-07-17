using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 攻击弹道拖尾粒子(方案2 VFX)的单个视觉桶运行时状态：一个 VFX 实例 + 位置/染色上传缓冲 + 本帧收集列表。
/// <para>【归属】由 <see cref="EffectHandler"/> 的「攻击弹道拖尾粒子」区独占管理(注册/每帧上传/销毁)，存于 EffectManager.dicAttackModeTrailVfx，
/// key = 弹道视觉桶签名 visualKey。AttackModeInstanceRenderer 只负责每帧报「位置 + 本发染色」，不持有也不碰本类——
/// 与血液/护盾粒子同样的分工：调用方给语义数据，粒子的实例/参数/缓冲全由 Effect 系统自管。</para>
/// <para>【一实例多子弹】一个 VFX 实例渲染本桶全部子弹的拖尾粒子，draw call 与子弹数量无关。</para>
/// <para>【逐弹染色】listPosition[i] 与 listColor[i] 严格同序同长——图内两条 buffer 用同一索引(particleId % PositionCount)采样，
/// 故第 i 发子弹的位置必配第 i 发自己的颜色，这正是"一个 VFX 装下多种颜色"的关键。颜色 = 该发 trail_data 的 color 原值。</para>
/// </summary>
public class AttackModeTrailVfxBean
{
    /// <summary>本桶的 VFX 特效实例（克隆自拖尾模板预制；模板缺失时为空=本桶不喷射，拖尾不显示）</summary>
    public VisualEffect vfx;
    /// <summary>本帧收集到的子弹世界坐标（复用 List，Clear 不释放容量；每帧一次性 SetData 上传给 positionBuffer）</summary>
    public readonly List<Vector3> listPosition = new List<Vector3>();
    /// <summary>本帧收集到的逐弹染色 rgb（与 listPosition 严格同序同长）。仅 rgb——alpha 由图内按粒子年龄 Lerp(StartAlpha,EndAlpha) 决定，不逐弹配置</summary>
    public readonly List<Vector3> listColor = new List<Vector3>();
    /// <summary>子弹位置上传缓冲（StructuredBuffer&lt;float3&gt;，绑定到 VFX 的 PositionBuffer；按需扩容、销毁时 Release）</summary>
    public GraphicsBuffer positionBuffer;
    /// <summary>逐弹染色上传缓冲（StructuredBuffer&lt;float3&gt;，绑定到 VFX 的 ColorBuffer；与 positionBuffer 同容量同步扩容）</summary>
    public GraphicsBuffer colorBuffer;
    /// <summary>positionBuffer/colorBuffer 当前容量（元素数），不足时同步扩容</summary>
    public int bufferCapacity;
}
