using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public partial class EffectHandler
{
    #region 内部工具
    /// <summary>
    /// 按 id 从 EffectInfo 配置解析粒子资源名(res_name)并缓存到 manager 字段：首次查表，之后直接读缓存字段，供高频粒子避免重复查配置表
    /// </summary>
    /// <param name="effectId">EffectInfo 配置表 id</param>
    /// <param name="cache">manager 上对应的 res_name 缓存字段(ref)</param>
    private string GetEffectResName(long effectId, ref string cache)
    {
        if (cache == null)
            cache = EffectInfoCfg.GetItemData(effectId).res_name;
        return cache;
    }
    #endregion

    #region 攻击弹道拖尾粒子(方案2 VFX)
    //——VFX Graph 暴露属性 ID(与图 Assets/FrameWork/Prefabs/Effect/VFX/VFX_Trail_1.vfx 的 Exposed Property 名一一对应，是 C# 与图的唯一耦合点)——
    //⚠️无下划线：本项目粒子命名约定(与血液的 "PositionStart"/"Direction" 一致)，改名须与图同步否则静默失效
    //⚠️图内的粒子贴图 MainTex 不在此列：由 VFX 预制自带，C# 不覆盖
    private static readonly int PropTrailPositionBuffer = Shader.PropertyToID("PositionBuffer");  //StructuredBuffer<float3> 本帧子弹位置
    private static readonly int PropTrailColorBuffer = Shader.PropertyToID("ColorBuffer");        //StructuredBuffer<float3> 本帧逐弹染色 rgb，与 PositionBuffer 同索引配对
    private static readonly int PropTrailPositionCount = Shader.PropertyToID("PositionCount");    //uint(或 int) 本帧有效子弹数，驱动喷发
    private static readonly int PropTrailStartAlpha = Shader.PropertyToID("StartAlpha");          //float 最新粒子透明度
    private static readonly int PropTrailEndAlpha = Shader.PropertyToID("EndAlpha");              //float 最老粒子透明度
    private static readonly int PropTrailLifetime = Shader.PropertyToID("Lifetime");              //float 粒子寿命(秒)
    private static readonly int PropTrailSpawnInterval = Shader.PropertyToID("SpawnInterval");    //float 每子弹喷粒间隔(秒)
    private static readonly int PropTrailParticleSize = Shader.PropertyToID("ParticleSize");      //float 粒子尺寸
    //位置/染色缓冲元素步长：均为 float3 = 12 字节(染色只传 rgb，alpha 由图内按粒子年龄 Lerp)
    private const int TrailVfxStride = 12;

    //——拖尾表现(全局写死；要调表现改这五个值)——
    //【为何不放配置表】它们是桶级的：注册时灌进 VFX 实例、同一 visualKey 只注册一次，逐行填也只有首个 type:2 行生效，
    //放配置表等于误导策划"可逐行调"。故 trail_data 走 type:2 时只需配 type + color(color 是唯一逐弹生效的项)。
    private const float TrailVfxLifetime = 1f;          //粒子寿命(秒)≈拖尾可见时长，空间长度≈弹速×本值
    private const float TrailVfxSpawnInterval = 0.02f;  //每发子弹喷粒间隔(秒)；单发同时存活粒子数≈Lifetime/SpawnInterval(当前≈50)
    private const float TrailVfxStartAlpha = 0.5f;      //最新(靠近弹体)粒子透明度
    private const float TrailVfxEndAlpha = 0.05f;       //最老(拖尾末端)粒子透明度
    private const float TrailVfxParticleSize = 0.05f;    //粒子尺寸(世界单位)；曾取弹体材质 _VertexScale 跟随弹体，现全局统一

    /// <summary>
    /// 注册某弹道视觉桶(visualKey)的拖尾 VFX：建一个常驻 VFX 实例并灌入一次性参数。
    /// <para>与血液/护盾同源(EffectInfo id→res_name→Effects 目录模型)，但**非播放式**：不入池、不 PlayEffect，实例常驻由本类每帧喂 GraphicsBuffer 驱动。
    /// 一个 visualKey 一个实例(内部去重)，该桶全部子弹的拖尾粒子由它一并渲染，draw call 与子弹数无关。</para>
    /// <para>【故不收 trail 配置、也不收桶材质】表现参数(含粒子尺寸)写死在本类(见 TrailVfx* 常量)、贴图由 VFX 预制自带、染色逐弹经
    /// <see cref="AddAttackModeTrailVfxPoint"/> 每帧传入——注册期无配置可读，只需桶签名。</para>
    /// <para>模板资源缺失时仍登记(每帧照常收集位置)但不建实例——拖尾静默不显示，弹体本体与方案1 均不受影响。</para>
    /// </summary>
    /// <param name="visualKey">弹道视觉桶签名(AttackModeInstanceRenderer.BuildVisualBucketKey 生成)</param>
    /// <returns>该桶的 VFX 数据(调用方缓存住它，之后每帧走 <see cref="AddAttackModeTrailVfxPoint(AttackModeTrailVfxBean, Vector3, Vector3)"/> 直接喂，热路径免去逐发字符串查字典)；visualKey 非法时返回 null</returns>
    public AttackModeTrailVfxBean RegisterAttackModeTrailVfx(string visualKey)
    {
        if (string.IsNullOrEmpty(visualKey))
            return null;
        //去重：该桶已建则复用(返回已有数据供调用方重新缓存引用)，避免重复实例化 VFX
        if (manager.dicAttackModeTrailVfx.TryGetValue(visualKey, out AttackModeTrailVfxBean existData))
            return existData;
        AttackModeTrailVfxBean trailVfxData = new AttackModeTrailVfxBean();
        manager.dicAttackModeTrailVfx[visualKey] = trailVfxData;

        //模板缺失(资源未制作/加载失败)：桶已登记但无实例，静默降级不喷射
        GameObject objModel = GetAttackModeTrailModel();
        if (objModel == null)
            return trailVfxData;
        //克隆模板实例化(世界空间模拟，位置由 buffer 提供，故实例本身置零点)
        GameObject objVfx = Instantiate(objModel);
        objVfx.name = $"AttackModeTrailVfx_{visualKey}";
        objVfx.transform.position = Vector3.zero;
        VisualEffect targetVisualEffect = objVfx.GetComponentInChildren<VisualEffect>(true);
        if (targetVisualEffect == null)
        {
            LogUtil.LogError($"拖尾 VFX 模板缺少 VisualEffect 组件：{objModel.name}");
            Destroy(objVfx);
            return trailVfxData;
        }
        trailVfxData.vfx = targetVisualEffect;

        //灌一次性参数(注册期设一次，此后不变)：逐属性 Has 判断后再 Set，图缺某属性时静默跳过(不报错)
        if (targetVisualEffect.HasFloat(PropTrailStartAlpha))
            targetVisualEffect.SetFloat(PropTrailStartAlpha, TrailVfxStartAlpha);
        if (targetVisualEffect.HasFloat(PropTrailEndAlpha))
            targetVisualEffect.SetFloat(PropTrailEndAlpha, TrailVfxEndAlpha);
        if (targetVisualEffect.HasFloat(PropTrailLifetime))
            targetVisualEffect.SetFloat(PropTrailLifetime, TrailVfxLifetime);
        if (targetVisualEffect.HasFloat(PropTrailSpawnInterval))
            targetVisualEffect.SetFloat(PropTrailSpawnInterval, TrailVfxSpawnInterval);
        if (targetVisualEffect.HasFloat(PropTrailParticleSize))
            targetVisualEffect.SetFloat(PropTrailParticleSize, TrailVfxParticleSize);
        return trailVfxData;
    }

    /// <summary>
    /// 拖尾 VFX 帧初始化：清空各桶本帧收集的位置/染色（由 AttackModeInstanceRenderer 每帧开头调）。
    /// <para>⚠️即使本帧一发子弹都没有也必须走完 Begin→Flush：Flush 会把喷发数归零，否则 VFX 会在上一批子弹的残留位置持续喷粒子。</para>
    /// </summary>
    public void BeginAttackModeTrailVfxFrame()
    {
        foreach (var itemData in manager.dicAttackModeTrailVfx.Values)
        {
            itemData.listPosition.Clear();
            itemData.listColor.Clear();
        }
    }

    /// <summary>
    /// 收集一发子弹本帧的拖尾采样点（由 AttackModeInstanceRenderer 逐弹调；未注册该桶时安全跳过）。
    /// <para>染色取该发 trailColor 原值上传，配置表配什么色就显示什么色；同桶不同攻击模式能各配各色，靠的就是这里逐弹取值(而非桶级取一次)。</para>
    /// </summary>
    /// <param name="visualKey">该发弹道的视觉桶签名</param>
    /// <param name="position">该发弹道当前世界坐标</param>
    /// <param name="trailColor">该发弹道自身的拖尾染色 rgb(BaseAttackMode.trailColor，来自其 trail_data 的 color)</param>
    public void AddAttackModeTrailVfxPoint(string visualKey, Vector3 position, Vector3 trailColor)
    {
        if (string.IsNullOrEmpty(visualKey) || !manager.dicAttackModeTrailVfx.TryGetValue(visualKey, out AttackModeTrailVfxBean trailVfxData))
            return;
        AddAttackModeTrailVfxPoint(trailVfxData, position, trailColor);
    }

    /// <summary>
    /// 收集一发子弹本帧的拖尾采样点(桶句柄版)：语义同上，但直接吃 <see cref="RegisterAttackModeTrailVfx"/> 返回的桶数据，省掉逐发的字符串哈希+字典查找。
    /// <para>热路径专用——AttackModeInstanceRenderer 每帧对每发 VFX 拖尾弹道调用一次，桶签名字符串长(含换图/自旋后缀)、哈希按长度计费，故由调用方把句柄缓存在视觉桶上。</para>
    /// </summary>
    /// <param name="trailVfxData">该发弹道所属视觉桶的 VFX 数据(注册时拿到并缓存；为空安全跳过)</param>
    /// <param name="position">该发弹道当前世界坐标</param>
    /// <param name="trailColor">该发弹道自身的拖尾染色 rgb(BaseAttackMode.trailColor，来自其 trail_data 的 color)</param>
    public void AddAttackModeTrailVfxPoint(AttackModeTrailVfxBean trailVfxData, Vector3 position, Vector3 trailColor)
    {
        if (trailVfxData == null)
            return;
        //⚠️位置与染色必须成对 Add 保持同序同长：图内用同一索引采样两条 buffer，错位即张冠李戴
        trailVfxData.listPosition.Add(position);
        trailVfxData.listColor.Add(trailColor);
    }

    /// <summary>
    /// 拖尾 VFX 帧收尾：把各桶本帧收集到的位置+逐弹染色一次性上传给对应 VFX 实例并驱动喷射（由 AttackModeInstanceRenderer 每帧末尾调）。
    /// <para>经持久 GraphicsBuffer(PositionBuffer/ColorBuffer) + 有效数(PositionCount)传入，图内一次 dispatch 在全部子弹位置喷各自颜色的粒子(合一 draw call)。</para>
    /// <para>⚠️本帧无子弹(posCount=0)时**不能跳过**：仍要把 PositionCount 写成 0，图内才会停止生成新粒子，否则会在上一批子弹的残留位置持续喷。</para>
    /// <para>【逐弹配色原理】图内两条 buffer 用同一索引(particleId % PositionCount)采样：本帧第 i 发子弹的位置与颜色分别在 [i] 位，故粒子拿到的位置与颜色必属同一发子弹。
    /// 索引虽随 particleId 递增而轮转，但同一批喷发内 {startId..startId+N-1} mod N 恰是 0..N-1 的一个排列，位置/颜色成对取同一 i，配对永远正确。</para>
    /// </summary>
    public void FlushAttackModeTrailVfxFrame()
    {
        foreach (var itemData in manager.dicAttackModeTrailVfx.Values)
        {
            VisualEffect targetVisualEffect = itemData.vfx;
            if (targetVisualEffect == null)
                continue;   //模板缺失/实例化失败：本桶不喷射
            int posCount = itemData.listPosition.Count;
            //本帧有子弹才需传位置/染色；无子弹时只把喷发数置 0 即可(已有粒子按寿命自然消散)
            if (posCount > 0)
            {
                //缓冲扩容时(含首帧新建)才需要重新绑定：VFX 持有的是 buffer 引用，容量没变就一直有效，无谓重绑是每帧白付的开销
                bool bufferRecreated = EnsureAttackModeTrailVfxBuffer(itemData, posCount);
                //只上传本帧前 posCount 个位置/染色(List→StructuredBuffer)；两者同序同长，保证图内同索引采样即同一发子弹
                itemData.positionBuffer.SetData(itemData.listPosition, 0, 0, posCount);
                itemData.colorBuffer.SetData(itemData.listColor, 0, 0, posCount);
                if (bufferRecreated)
                {
                    if (targetVisualEffect.HasGraphicsBuffer(PropTrailPositionBuffer))
                        targetVisualEffect.SetGraphicsBuffer(PropTrailPositionBuffer, itemData.positionBuffer);
                    //图未建 ColorBuffer 时静默跳过(不报错)：粒子拿不到染色，退化为贴图原色
                    if (targetVisualEffect.HasGraphicsBuffer(PropTrailColorBuffer))
                        targetVisualEffect.SetGraphicsBuffer(PropTrailColorBuffer, itemData.colorBuffer);
                }
            }
            //喷发数：兼容图内 PositionCount 建成 uint 或 int 两种类型(Has 判断后再 Set，避免类型不符静默丢失)
            if (targetVisualEffect.HasUInt(PropTrailPositionCount))
                targetVisualEffect.SetUInt(PropTrailPositionCount, (uint)posCount);
            else if (targetVisualEffect.HasInt(PropTrailPositionCount))
                targetVisualEffect.SetInt(PropTrailPositionCount, posCount);
        }
    }

    /// <summary>
    /// 注销单个视觉桶的拖尾 VFX（热替换/桶注销时调）：销毁实例+释放缓冲。
    /// </summary>
    public void ClearAttackModeTrailVfx(string visualKey)
    {
        if (string.IsNullOrEmpty(visualKey) || !manager.dicAttackModeTrailVfx.TryGetValue(visualKey, out AttackModeTrailVfxBean trailVfxData))
            return;
        DestroyAttackModeTrailVfx(trailVfxData);
        manager.dicAttackModeTrailVfx.Remove(visualKey);
    }

    /// <summary>
    /// 清空全部拖尾 VFX（整场战斗结束时随视觉桶一并清理）：销毁各实例+释放缓冲+复位模板门控。
    /// <para>模板预制本体由 EffectManager.dicEffectModel 持有(跨战斗保留)，此处不释放；仅置空本地引用，下场重取即缓存命中。</para>
    /// </summary>
    public void ClearAllAttackModeTrailVfx()
    {
        foreach (var itemData in manager.dicAttackModeTrailVfx.Values)
            DestroyAttackModeTrailVfx(itemData);
        manager.dicAttackModeTrailVfx.Clear();
        manager.objAttackModeTrailModel = null;
        manager.triedLoadAttackModeTrailModel = false;
    }

    /// <summary>
    /// 确保某桶的位置/染色缓冲容量 &gt;= count；不足时 Release 旧的、按 2 的幂(下限16)重建 StructuredBuffer&lt;float3&gt;。
    /// <para>两条 buffer 同容量、同步扩容(索引一一对应，容量不同即错位)；容量够用时原样复用，返回 false 让调用方跳过重绑。</para>
    /// </summary>
    /// <returns>是否重建了缓冲——true 时调用方**必须**重新 SetGraphicsBuffer，否则 VFX 仍指着已 Release 的旧 buffer</returns>
    private static bool EnsureAttackModeTrailVfxBuffer(AttackModeTrailVfxBean trailVfxData, int count)
    {
        if (trailVfxData.positionBuffer != null && trailVfxData.colorBuffer != null && trailVfxData.bufferCapacity >= count)
            return false;
        trailVfxData.positionBuffer?.Release();
        trailVfxData.colorBuffer?.Release();
        int cap = Mathf.NextPowerOfTwo(Mathf.Max(count, 16));
        trailVfxData.positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cap, TrailVfxStride);
        trailVfxData.colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cap, TrailVfxStride);
        trailVfxData.bufferCapacity = cap;
        return true;
    }

    /// <summary>
    /// 销毁单个拖尾 VFX 桶的运行时资源：Release 位置/染色 GraphicsBuffer + Destroy VisualEffect 实例 GameObject，避免显存/对象泄漏。
    /// </summary>
    private void DestroyAttackModeTrailVfx(AttackModeTrailVfxBean trailVfxData)
    {
        if (trailVfxData == null)
            return;
        if (trailVfxData.positionBuffer != null)
        {
            trailVfxData.positionBuffer.Release();
            trailVfxData.positionBuffer = null;
        }
        if (trailVfxData.colorBuffer != null)
        {
            trailVfxData.colorBuffer.Release();
            trailVfxData.colorBuffer = null;
        }
        trailVfxData.bufferCapacity = 0;
        if (trailVfxData.vfx != null)
        {
            Destroy(trailVfxData.vfx.gameObject);
            trailVfxData.vfx = null;
        }
    }

    /// <summary>
    /// 取拖尾 VFX 的模板预制(不实例化/不播放/不入池)：id→res_name(查 EffectInfo 配置,缓存)→Effects 目录模型，与血液/护盾同一加载源。
    /// <para>本场至多真正加载一次(triedLoadAttackModeTrailModel 门控)：资源未制作时 Addressables 会抛异常，门控保证不会逐桶注册反复抛、刷屏日志。
    /// 加载失败/缺资源返回 null → 拖尾静默不显示。整场结束 ClearAllAttackModeTrailVfx 复位门控，下场重试。</para>
    /// </summary>
    private GameObject GetAttackModeTrailModel()
    {
        if (manager.triedLoadAttackModeTrailModel)
            return manager.objAttackModeTrailModel;
        manager.triedLoadAttackModeTrailModel = true;
        try
        {
            manager.objAttackModeTrailModel = manager.GetEffectModelSync(GetEffectResName(manager.effectAttackModeTrailId, ref manager.resNameAttackModeTrail));
            if (manager.objAttackModeTrailModel == null)
                LogUtil.Log("[AttackModeTrail] 拖尾粒子未找到，方案2(type:2)拖尾暂不显示；EffectInfo 配置/资源就绪后即生效");
        }
        catch (System.Exception e)
        {
            //Addressables 缺 key 会抛异常：吞掉并降级，避免资源未建期间刷屏
            LogUtil.Log($"[AttackModeTrail] 拖尾粒子加载失败，方案2(type:2)拖尾暂不显示；资源就绪后即生效：{e.Message}");
        }
        return manager.objAttackModeTrailModel;
    }
    #endregion

    #region 打击 / 溅血粒子
    /// <summary>
    /// 播放护盾打击粒子
    /// </summary>
    /// <param name="targetPos">粒子播放的世界坐标</param>
    /// <param name="attDirection">攻击来向(x<0从左、否则从右)，决定护盾朝向</param>
    public void ShowShieldHitEffect(Vector3 targetPos, Vector3 attDirection)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            targetVisualEffect.SetVector3("Position", targetPos);
            targetVisualEffect.SetInt("Direction", attDirection.x < 0 ? 0 : 1);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(GetEffectResName(manager.effectShieldHitId, ref manager.resNameShieldHit), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

    /// <summary>
    /// 播放血粒子
    /// </summary>
    /// <param name="targetPos">粒子播放的世界坐标</param>
    /// <param name="attDirection">攻击来向(x>0血向右溅、否则向左溅)</param>
    public void ShowBloodEffect(Vector3 targetPos, Vector3 attDirection)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            if (attDirection.x > 0)
            {
                //targetVisualEffect.SetVector3("BloodVelocityRandomStart", new Vector3(1, 3, 1));
                targetVisualEffect.SetVector3("BloodVelocityRandomEnd", new Vector3(3, -1, -1));
            }
            else
            {
                //targetVisualEffect.SetVector3("BloodVelocityRandomStart", new Vector3(1, 3, 1));
                targetVisualEffect.SetVector3("BloodVelocityRandomEnd", new Vector3(-3, -1, -1));
            }
            targetVisualEffect.SetVector3("PositionStart", targetPos);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(GetEffectResName(manager.effectBloodId, ref manager.resNameBlood), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }
    #endregion

    #region 落雷粒子(全局单例PS)
    /// <summary>
    /// 播放落雷粒子：移动全局唯一实例到落雷点并重播。
    /// <para>重播用 Stop(StopEmitting)+Play：保活前几道雷的存活粒子只停发射，Play 重置系统时间重新触发爆发，
    /// 支持 0.1 秒间隔连发多道雷交叠；要求粒子为世界空间模拟(Effect_Thunder_3 已配 moveWithTransform=1)。</para>
    /// </summary>
    /// <param name="targetPos">落雷点世界坐标</param>
    public void ShowThunderEffect(Vector3 targetPos)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            targetEffect.transform.position = targetPos;
            //停发射保活已有粒子，再重播触发新一轮爆发(playing状态直接Play不会重新触发)
            if (targetEffect.mainPS != null)
            {
                targetEffect.mainPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(GetEffectResName(manager.effectThunderId, ref manager.resNameThunder), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }
    #endregion

    #region 飘字(伤害数字)粒子
    /// <summary>
    /// 播放数字粒子(伤害/闪避/HP/护甲飘字)：转发给 GPU Instancing 批量渲染器(FightTextInstanceRenderer，字符级实例一次
    /// DrawMeshInstanced、动画全在 shader 时间驱动)；首次调用时按 effectTextNumberName 预制装配(整场至多试一次)。
    /// </summary>
    /// <param name="targetPos">飘字起始的世界坐标</param>
    /// <param name="number">显示的数值(闪避类型传 0，显示 0)</param>
    /// <param name="type">类型 0普通伤害 1闪避 2暴击伤害 3HP增加 4护甲增加</param>
    /// <param name="randomPosOffset">起始位置随机偏移范围</param>
    public void ShowTextNumEffect(Vector3 targetPos, int number, int type, float randomPosOffset = 0.2f)
    {
        FightTextInstanceRenderer textRenderer = FightHandler.Instance.manager.fightTextInstanceRenderer;
        if (!textRenderer.IsReady)
        {
            if (manager.triedSetupTextNumInstanced)
                return; //已装配失败过(预制缺失/结构不符)，不再重试
            manager.triedSetupTextNumInstanced = true;
            TrySetupTextNumInstanced(textRenderer);
            if (!textRenderer.IsReady)
                return;
        }

        Color targetColor = manager.colorDamage;
        float targetScale = textRenderer.textScaleNormal;
        string textContent = number.ToString();
        switch (type)
        {
            case 1: // 闪避(显示0，调用方传 number=0)
                targetColor = manager.colorDamageEVA;
                break;
            case 2: // 暴击伤害
                targetColor = manager.colorDamageCRT;
                targetScale = textRenderer.textScaleCrit;
                break;
            case 3: // HP增加
                targetColor = manager.colorHPAdd;
                break;
            case 4: // 护甲增加
                targetColor = manager.colorDRAdd;
                break;
        }
        textRenderer.ShowText(targetPos, textContent, targetColor, targetScale, randomPosOffset);
    }

    /// <summary>
    /// 把飘字预制装配进 GPU Instancing 渲染器：加载预制(缓存 dicTextNumModel)，取其 MeshFilter 的 mesh 与
    /// MeshRenderer 的 material 调用 <see cref="FightTextInstanceRenderer.Setup"/>。
    /// 预制必须是「MeshFilter(Quad)+MeshRenderer(instanced材质)」结构；仍是 TMP 结构或缺组件时报错不装配
    /// (整场至多试一次，triedSetupTextNumInstanced 门控，与拖尾 VFX 的 triedLoadAttackModeTrailModel 同理)。
    /// </summary>
    protected void TrySetupTextNumInstanced(FightTextInstanceRenderer textRenderer)
    {
        if (!manager.dicTextNumModel.TryGetValue(manager.effectTextNumberName, out GameObject objModel) || objModel == null)
        {
            objModel = LoadAddressablesUtil.LoadAssetSync<GameObject>(manager.effectTextNumberName);
            if (objModel != null)
                manager.dicTextNumModel[manager.effectTextNumberName] = objModel;
        }
        if (objModel == null)
        {
            LogUtil.LogError($"飘字预制体加载失败：{manager.effectTextNumberName}");
            return;
        }
        if (objModel.GetComponentInChildren<TextMeshPro>() != null)
        {
            LogUtil.LogError($"飘字预制体仍是 TMP 结构(已废弃)，请改用 Quad+instanced 材质：{manager.effectTextNumberName}");
            return;
        }
        MeshFilter meshFilter = objModel.GetComponentInChildren<MeshFilter>();
        MeshRenderer meshRenderer = objModel.GetComponentInChildren<MeshRenderer>();
        if (meshFilter == null || meshFilter.sharedMesh == null || meshRenderer == null || meshRenderer.sharedMaterial == null)
        {
            LogUtil.LogError($"飘字预制体缺少 MeshFilter/MeshRenderer：{manager.effectTextNumberName}");
            return;
        }
        textRenderer.Setup(meshFilter.sharedMesh, meshRenderer.sharedMaterial);
    }

    /// <summary>
    /// 清理所有飘字(战斗结束时调用)：清 instanced 飘字在屏字符槽(渲染器本体与材质引用保留，跨场复用)
    /// </summary>
    public void ClearTextNumEffect()
    {
        FightHandler.Instance.manager.fightTextInstanceRenderer.Clear();
    }
    #endregion

    #region 生物进阶粒子
    /// <summary>
    /// 展示生物进阶增加进度粒子(从起点飞向终点容器的流动光点)
    /// </summary>
    /// <param name="addNum">发射的光点数量</param>
    /// <param name="startPosition">光点起始世界坐标(带随机散布)</param>
    /// <param name="endPosition">光点汇聚的终点世界坐标</param>
    public void ShowCreatureAscendAddProgressEffect(int addNum, Vector3 startPosition, Vector3 endPosition)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            float randomRange = 0.5f;
            targetVisualEffect.SetInt("EffectNum", addNum);
            targetVisualEffect.SetFloat("StartSize", 0.2f);
            targetVisualEffect.SetFloat("MoveSpeed", 0.02f);
            targetVisualEffect.SetVector3("StartPositionRandomA", startPosition + new Vector3(-randomRange, -randomRange, -randomRange));
            targetVisualEffect.SetVector3("StartPositionRandomB", startPosition + new Vector3(randomRange, randomRange, randomRange));
            targetVisualEffect.SetVector3("EndPosition", endPosition);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(GetEffectResName(manager.effectCreatureAscendAddProgressId, ref manager.resNameAscendAddProgress), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

    /// <summary>
    /// 展示生物进阶完成的庆祝粒子(专用粒子,进阶成功时在容器处播放,按升阶后的新稀有度上色成"稀有度流光")。
    /// </summary>
    /// <param name="targetPosition">容器世界坐标(粒子播放位置)</param>
    /// <param name="rarityColor">升阶后新稀有度的主色(粒子起始颜色)</param>
    public void ShowCreatureAscendCompleteEffect(Vector3 targetPosition, Color rarityColor)
    {
        //专用一次性粒子:先定位、按稀有度给所有粒子系统上色,再播放,2秒后自动销毁
        EffectBean effectData = new EffectBean();
        effectData.effectName = GetEffectResName(manager.effectCreatureAscendCompleteId, ref manager.resNameAscendComplete);
        effectData.effectPosition = targetPosition;
        effectData.timeForShow = 2f;
        effectData.isDestoryPlayEnd = true;
        effectData.isPlayInShow = false;
        ShowEffect(effectData, (effect) =>
        {
            if (effect == null)
                return;
            //白模板粒子按新稀有度上色(逐个粒子系统改 startColor)后再播放
            if (!effect.listPS.IsNull())
            {
                for (int i = 0; i < effect.listPS.Count; i++)
                {
                    var mainModule = effect.listPS[i].main;
                    mainModule.startColor = rarityColor;
                }
            }
            effect.PlayEffect();
        });
    }
    #endregion

    #region 放置魔物粒子
    /// <summary>
    /// 放置魔物时在魔王(防守核心)位置播放消耗魔力粒子
    /// </summary>
    /// <param name="targetPos">魔王世界坐标</param>
    public void ShowManaEffect(Vector3 targetPos)
    {
        ShowEffect(manager.effectManaId, targetPos);
    }

    /// <summary>
    /// 放置魔物时在生成的魔物位置播放登场粒子
    /// </summary>
    /// <param name="targetPos">魔物生成的世界坐标</param>
    public void ShowCreatureShowEffect(Vector3 targetPos)
    {
        ShowEffect(manager.effectCreatureShowId, targetPos);
    }
    #endregion

    #region 配置驱动通用粒子
    /// <summary>
    /// 展示粒子(配置驱动)：按 EffectInfo 配置解析 float/int/vector3/vector4 数据并写入 VFX/PS，按展示类型(持久/一次性)取实例播放
    /// </summary>
    /// <param name="effectId">EffectInfo 配置表 id</param>
    /// <param name="targetPos">粒子播放的世界坐标</param>
    /// <param name="direction">方向(供含 {Direction} 占位的 int 数据使用)</param>
    /// <param name="size">尺寸倍率(供含 {Size} 占位的 float 数据及 PS 起始大小使用)</param>
    public void ShowEffect(long effectId, Vector3 targetPos, Direction2DEnum direction = Direction2DEnum.None, float size = 1)
    {
        targetPos += new Vector3(0, 0.002f, -0.001f);
        var effectInfo = EffectInfoCfg.GetItemData(effectId);
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            var targetParticleSystem = targetEffect.GetParticleSystem();
            //粒子系统处理
            var dicEffectData = effectInfo.GetEffectItemData();
            dicEffectData.ForEach((index, value) =>
            {
                switch (value.dataType)
                {
                    case 1://float
                        float targetFloatData = value.dataFloat;
                        if (value.isSize)
                        {
                            targetFloatData = size * targetFloatData;
                            //设置PS系统的起始位置
                            if (targetParticleSystem != null) targetEffect.SetParticleSystemSize(targetFloatData);
                        }
                        targetVisualEffect?.SetFloat(value.dataName, targetFloatData);
                        break;
                    case 2://int
                        int targetIntData = value.dataInt;
                        if (value.isDirection)
                        {
                            targetIntData = (int)direction;
                        }
                        targetVisualEffect?.SetInt(value.dataName, targetIntData);
                        break;
                    case 5://vector3
                        Vector3 targetVector3Data = value.dataVector3;
                        if (value.isStartPosition)
                        {
                            targetVector3Data = targetPos + targetVector3Data;
                            //设置PS系统的起始位置
                            if (targetParticleSystem != null) targetEffect.SetParticleSystemStartPosition(targetVector3Data);
                        }
                        targetVisualEffect?.SetVector3(value.dataName, targetVector3Data);
                        break;
                    case 6://vector4
                        Vector4 targetVector4Data = value.dataVector4;
                        targetVisualEffect?.SetVector4(value.dataName, targetVector4Data);
                        break;
                }
            });
            targetEffect.PlayEffect();
        };
        EffectShowTypeEnum showType = effectInfo.GetShowType();
        //如果是持久
        if (showType == EffectShowTypeEnum.Enduring)
        {
            //获取粒子实例
            manager.GetEffectForEnduring(effectInfo.res_name, (targetEffect) =>
            {
                playEffect?.Invoke(targetEffect);
            });
        }
        //如果是一次性
        else if (showType == EffectShowTypeEnum.Once)
        {
            EffectBean effectData = effectInfo.GetEffectData();
            effectData.effectPosition = targetPos + new Vector3(0, 0f, -0.1f);
            ShowEffect(effectData, (targetEffect) =>
            {
                playEffect?.Invoke(targetEffect);
            });
        }
    }
    #endregion

    #region 献祭粒子
    /// <summary>
    /// 展示献祭粒子：各祭品发射光流飞向中心点，中心汇聚粒子按延时/存活时长播放，半程停祭品光流，结束触发完成回调
    /// </summary>
    /// <param name="listSacrficeTarget">祭品对象列表(各自带 VisualEffect 光流)</param>
    /// <param name="endPostion">汇聚中心世界坐标</param>
    /// <param name="timeCenterDelay">中心粒子起始延时</param>
    /// <param name="timeCenterLifetime">中心粒子存活时长</param>
    /// <param name="actionForComplete">全部播放结束的回调</param>
    public void ShowSacrficeEffect(List<GameObject> listSacrficeTarget, Vector3 endPostion, float timeCenterDelay, float timeCenterLifetime, Action actionForComplete)
    {
        if (manager.animForShowSacrficeEffect != null)
            manager.animForShowSacrficeEffect.Kill();
        if (manager.animForShowSacrficeEffectComplete != null)
            manager.animForShowSacrficeEffectComplete.Kill();
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            listSacrficeTarget.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.gameObject.SetActive(true);

                visualEffect.SetVector3("StartPosition", visualEffect.transform.position);
                visualEffect.SetVector3("EndPosition", endPostion + new Vector3(0, 0.5f, 0));
                visualEffect.Play();
            });

            var targetVisualEffect = targetEffect.GetVisualEffect();
            targetVisualEffect.SetFloat("StartDelay", timeCenterDelay);
            targetVisualEffect.SetFloat("LifeTime", timeCenterLifetime);
            targetVisualEffect.SetVector3("EndPosition", endPostion + new Vector3(0, 0.5f, 0));
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring("EffectSacrfice_1", (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });

        manager.animForShowSacrficeEffect = DOVirtual.DelayedCall(timeCenterDelay + (timeCenterLifetime / 2f), () =>
        {
            listSacrficeTarget.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.Stop();
            });
        });

        manager.animForShowSacrficeEffectComplete = DOVirtual.DelayedCall(timeCenterDelay + timeCenterLifetime, () =>
        {
            actionForComplete?.Invoke();
        });
    }
    #endregion
}
