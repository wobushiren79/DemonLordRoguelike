using Spine;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;

public partial class SpineHandler
{
    public TrackEntry PlayAnim(
        SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum animationCreatureState, CreatureBean creatureData, bool isLoop,
        float mixDuration = -1, float animStartTime = 0, float animSpeed = 1)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("播放动画失败 缺少skeletonAnimation资源");
            return null;
        }
        if (creatureData == null)
        {
            LogUtil.LogError("播放动画失败 缺少creatureData资源");
            return null;
        }
        var animNameAppoint = GetAnimNameAppoint(animationCreatureState, creatureData);
        var animData = PlayAnim(skeletonAnimation, animationCreatureState, isLoop, animNameAppoint: animNameAppoint, animStartTime: animStartTime, animSpeed: animSpeed);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    public TrackEntry PlayAnim(
        SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum animationCreatureState, CreatureBean creatureData, bool isLoop,
        float mixDuration = -1, float animStartTime = 0, float animSpeed = 1)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("播放动画失败 缺少skeletonAnimation资源");
            return null;
        }
        if (creatureData == null)
        {
            LogUtil.LogError("播放动画失败 缺少creatureData资源");
            return null;
        }
        var animNameAppoint = GetAnimNameAppoint(animationCreatureState, creatureData);
        var animData = PlayAnim(skeletonGraphic, animationCreatureState, isLoop, animNameAppoint: animNameAppoint, animStartTime: animStartTime, animSpeed: animSpeed);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    protected string GetAnimNameAppoint(SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        //幻化整骨替换后原生物 anim_* 配置名不适用于新骨架:不指定动画名,交框架按目标骨架实际动画列表解析(缺失仅日志不播,避免 SetAnimation 抛异常)
        if (creatureData.GetTransformSpineRes() != null)
        {
            //映射动画例外:幻化药配置了 show 骨架替代动画(other_data 的 idle_anim/walk_anim/attack_anim/dead_anim 键,生成期检测骨架无标准候选时写入)时优先按名直播
            switch (spineAnimationState)
            {
                case SpineAnimationStateEnum.Idle:
                    string transformIdleAnim = creatureData.GetTransformIdleAnim();
                    if (!transformIdleAnim.IsNull())
                        return transformIdleAnim;
                    break;
                case SpineAnimationStateEnum.Walk:
                    string transformWalkAnim = creatureData.GetTransformWalkAnim();
                    if (!transformWalkAnim.IsNull())
                        return transformWalkAnim;
                    break;
                case SpineAnimationStateEnum.Attack:
                    string transformAttackAnim = creatureData.GetTransformAttackAnim();
                    if (!transformAttackAnim.IsNull())
                        return transformAttackAnim;
                    break;
                case SpineAnimationStateEnum.Dead:
                    string transformDeadAnim = creatureData.GetTransformDeadAnim();
                    if (!transformDeadAnim.IsNull())
                        return transformDeadAnim;
                    break;
            }
            return null;
        }
        string animNameAppoint = null;
        switch (spineAnimationState)
        {
            case SpineAnimationStateEnum.Idle:
                if (!creatureData.creatureInfo.anim_idle.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_idle;
                break;
            case SpineAnimationStateEnum.Attack:
                if (!creatureData.creatureInfo.anim_attack.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_attack;
                break;
            case SpineAnimationStateEnum.Walk:
                if (!creatureData.creatureInfo.anim_walk.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_walk;
                break;
            case SpineAnimationStateEnum.Dead:
                if (!creatureData.creatureInfo.anim_dead.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_dead;
                break;
        }
        return animNameAppoint;
    }

    /// <summary>
    /// 设置动画到第一帧（静态姿势，不播放）
    /// </summary>
    public void SetAnimFirstFrame(SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        if (skeletonAnimation == null || creatureData == null)
            return;
        var trackEntry = PlayAnim(skeletonAnimation, spineAnimationState, creatureData, false, animSpeed: 0);
        if (trackEntry != null)
        {
            trackEntry.TrackTime = 0;
            skeletonAnimation.Update(0);
        }
    }

    /// <summary>
    /// 设置动画到第一帧（静态姿势，不播放，SkeletonGraphic 版）
    /// </summary>
    public void SetAnimFirstFrame(SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        //内部动画组件为空=骨架数据未设置成功(资源缺失等),保持原姿势不报错
        if (skeletonGraphic == null || creatureData == null || skeletonGraphic.Animation == null)
            return;
        var trackEntry = PlayAnim(skeletonGraphic, spineAnimationState, creatureData, false, animSpeed: 0);
        if (trackEntry != null)
        {
            trackEntry.TrackTime = 0;
            ((SkeletonAnimation)skeletonGraphic.Animation).Update(0);
        }
    }

    #region 场景亮度调整(幻化药 show_brightness, <1调暗/>1调亮)

    /// <summary>调暗材质缓存：key=(原图集材质, 系数量化千分位)，value=克隆调暗材质（会话级复用，上界≈图集数×系数种数，可忽略）</summary>
    protected static readonly Dictionary<(Material src, int kQuant), Material> sceneDimMaterialCache = new Dictionary<(Material, int), Material>();
    /// <summary>全部调暗克隆材质识别集合（ClearSceneDimOverride 按值识别残留键用）</summary>
    protected static readonly HashSet<Material> sceneDimMaterialClones = new HashSet<Material>();
    /// <summary>调暗用的主项目 shader 资产缓存（懒加载自 Resources 的 dummy 材质）</summary>
    protected static Shader sceneDimShaderCache;

    /// <summary>
    /// 获取调暗用的主项目 shader 资产：bundle 内嵌 shader 在构建时被剥离未引用变体（_COLOR_ADJUST 缺失致 EnableKeyword 静默失效），
    /// 统一换成主项目 shader 资产（其 _COLOR_ADJUST 变体由 Resources/Materials/SpineSpriteURP_DimDummy.mat 保住，PC 包同样有效）。
    /// </summary>
    /// <returns>URP/Spine/Sprite shader 资产（加载失败返回 null，调用方保持原 shader）</returns>
    protected static Shader GetSceneDimShader()
    {
        if (sceneDimShaderCache == null)
        {
            Material dummyMat = Resources.Load<Material>("Materials/SpineSpriteURP_DimDummy");
            if (dummyMat != null)
                sceneDimShaderCache = dummyMat.shader;
        }
        return sceneDimShaderCache;
    }

    /// <summary>
    /// 应用场景亮度覆盖：把场景实例的普通页图集材质经 CustomMaterialOverride 替换为克隆调亮暗材质（_COLOR_ADJUST+_Brightness，HSV 只缩明度不碰 alpha，PMA 安全）；
    /// 仅作用于世界空间 SkeletonAnimation（UI 的 SkeletonGraphic 走另一渲染路径不受影响）；混合页(-Multiply/-Screen)材质跳过（混合公式不同，调亮暗语义不成立）。
    /// </summary>
    /// <param name="skeletonAnimation">目标场景生物</param>
    /// <param name="brightness">亮度系数（other_data 的 show_brightness 键值，∈ (0,2]，&lt;1 调暗，&gt;1 调亮；shader _Brightness 属性域 Range(0,2) 原生支持）</param>
    public void ApplySceneDimOverride(SkeletonAnimation skeletonAnimation, float brightness)
    {
        if (skeletonAnimation == null)
            return;
        //4.3: SkeletonAnimation 与 SkeletonRenderer 已分离，渲染属性经 Renderer 访问
        var renderer = skeletonAnimation.Renderer as SkeletonRenderer;
        if (renderer == null || skeletonAnimation.SkeletonDataAsset == null)
            return;
        var customOverride = renderer.CustomMaterialOverride;
        //先清旧调暗键(对象池复用后图集可能整套换掉, 按值识别与当前图集无关)
        RemoveSceneDimEntries(customOverride);
        int kQuant = Mathf.RoundToInt(brightness * 1000);
        //键取图集资产的权威材质清单(普通页)——不读渲染器当前 sharedMaterials(SetSkeletonDataAsset 后渲染器材质可能未刷新, 键会错配致覆盖不生效)
        var atlasAssets = skeletonAnimation.SkeletonDataAsset.atlasAssets;
        for (int a = 0; a < atlasAssets.Length; a++)
        {
            if (atlasAssets[a] == null || atlasAssets[a].Materials == null)
                continue;
            foreach (Material srcMat in atlasAssets[a].Materials)
            {
                if (srcMat == null || srcMat.name.EndsWith("-Multiply") || srcMat.name.EndsWith("-Screen"))
                    continue;
                if (!sceneDimMaterialCache.TryGetValue((srcMat, kQuant), out Material dimMat) || dimMat == null)
                {
                    dimMat = new Material(srcMat);
                    //bundle 内嵌 shader 变体被裁剪(_COLOR_ADJUST 缺失), 统一换主项目 shader 资产再开关键字(变体由 Resources dummy 材质保住)
                    Shader dimShader = GetSceneDimShader();
                    if (dimShader != null)
                        dimMat.shader = dimShader;
                    dimMat.EnableKeyword("_COLOR_ADJUST");
                    dimMat.SetFloat("_Brightness", brightness);
                    dimMat.name = srcMat.name + "_Dim" + kQuant;
                    sceneDimMaterialCache[(srcMat, kQuant)] = dimMat;
                    sceneDimMaterialClones.Add(dimMat);
                }
                //字典赋值即生效(SkeletonRenderer.CustomMaterialOverride getter 自带 materialsNeedUpdate, 先例 CreatureSpineOutlineFollow)
                customOverride[srcMat] = dimMat;
            }
        }
    }

    /// <summary>
    /// 清除场景调暗覆盖（幻化药无 show_brightness 键/幻原药恢复/对象池复用时调用，幂等）
    /// </summary>
    /// <param name="skeletonAnimation">目标场景生物</param>
    public void ClearSceneDimOverride(SkeletonAnimation skeletonAnimation)
    {
        if (skeletonAnimation == null || skeletonAnimation.Renderer == null || sceneDimMaterialClones.Count == 0)
            return;
        //4.3: SkeletonAnimation 与 SkeletonRenderer 已分离，渲染属性经 Renderer 访问
        RemoveSceneDimEntries(((SkeletonRenderer)skeletonAnimation.Renderer).CustomMaterialOverride);
    }

    /// <summary>
    /// 从覆盖字典移除全部调暗键（按值∈克隆集合识别；禁 Clear()——防误删描边系统等同字典写入者的键）
    /// </summary>
    /// <param name="customOverride">目标渲染器的 CustomMaterialOverride 字典</param>
    protected void RemoveSceneDimEntries(Dictionary<Material, Material> customOverride)
    {
        if (customOverride.Count == 0)
            return;
        List<Material> dimKeys = null;
        foreach (var pair in customOverride)
        {
            if (pair.Value != null && sceneDimMaterialClones.Contains(pair.Value))
                (dimKeys ??= new List<Material>()).Add(pair.Key);
        }
        if (dimKeys == null)
            return;
        for (int i = 0; i < dimKeys.Count; i++)
            customOverride.Remove(dimKeys[i]);
    }

    #endregion
}