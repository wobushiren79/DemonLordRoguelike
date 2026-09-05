using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NpcCreateEditorWindow 右栏（partial）：Spine 预览子系统。
/// 驱动骨架拷贝改造自 SpineWindow 动画预览页签（SpineWindowPreview.cs）：
/// PreviewRenderUtility + EditorInstantiation.InstantiateSkeletonAnimation 实例化真 SkeletonAnimation 进预览场景（HideAndDontSave + 专用 Layer + 正交相机），
/// EditorApplication.update 手动 Update + Renderer.LateUpdate，Repaint 时 BeginPreview/cam.Render/EndPreview 上屏。
/// 皮肤应用层按本项目数据模型重写（CreatureBean.GetSkinData → FindSkin 合成 → SetSkin → slot 染色），
/// 全程不碰 CreatureHandler/SpineHandler 等运行时单例（编辑器零场景污染）。
/// 双模型：参考模型（生物2001基础皮肤）+ 目标NPC模型；参考模型可关闭。
/// </summary>
public partial class NpcCreateEditorWindow : EditorWindow
{
    #region 预览字段
    /// <summary>预览渲染工具</summary>
    private PreviewRenderUtility previewUtility;
    /// <summary>目标NPC模型（随配置刷新）</summary>
    private SkeletonAnimation previewTargetAnim;
    /// <summary>参考模型（生物2001基础皮肤，创建后不变）</summary>
    private SkeletonAnimation previewNormalAnim;
    /// <summary>预览加载错误信息（null=无错误）</summary>
    private string previewLoadError;
    /// <summary>当前预览目标的 model_id（-1=无目标；用于判断模型切换是否需要重建实例）</summary>
    private long previewModelId = -1;
    /// <summary>当前预览目标装配好的生物数据（外观区调色列表的数据来源；每次刷新重建）</summary>
    private CreatureBean previewCreatureData;
    /// <summary>是否展示装备（影响皮肤合成，不入配置）</summary>
    private bool isShowEquip = true;
    /// <summary>是否显示参考模型</summary>
    private bool isShowReferenceModel = true;

    //预览-动画
    /// <summary>目标模型的动画名列表</summary>
    private readonly List<string> previewAnimNames = new List<string>();
    /// <summary>当前播放的动画索引（-1=未播放）</summary>
    private int previewAnimIndex = -1;
    private bool previewPlaying;
    private bool previewLoop = true;
    private float previewTimeScale = 1f;
    private double previewLastUpdateTime;

    //预览-相机
    private float previewCameraOrtho = 1f;
    private Vector3 previewCameraPos = new Vector3(0, 0, -10);
    /// <summary>预览场景专用 Layer（预览相机只渲染该层）</summary>
    private const int PreviewLayer = 30;
    /// <summary>双模型左右分开的距离</summary>
    private const float ModelSideOffset = 0.9f;

    private GUIStyle previewBackgroundStyle;
    private Vector2 scrollAnimList;
    #endregion

    #region 生命周期
    /// <summary>
    /// 预览子系统启用：订阅编辑器更新与播放模式变化（由主类 OnEnable 调用）
    /// </summary>
    private void PreviewOnEnable()
    {
        EditorApplication.update += OnPreviewEditorUpdate;
        EditorApplication.playModeStateChanged += OnPreviewPlayModeChanged;
    }

    /// <summary>
    /// 预览子系统禁用：退订并销毁预览实例（由主类 OnDisable 调用）
    /// </summary>
    private void PreviewOnDisable()
    {
        EditorApplication.update -= OnPreviewEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPreviewPlayModeChanged;
        DisposePreview();
    }

    /// <summary>
    /// 进入/退出播放模式时销毁预览实例（HideAndDontSave 物体随场景卸载销毁，引用需同步清理）
    /// </summary>
    private void OnPreviewPlayModeChanged(PlayModeStateChange state)
    {
        DisposePreview();
        previewModelId = -1;
    }

    /// <summary>
    /// 编辑器帧更新：播放中手动推进双模型动画并重绘
    /// </summary>
    private void OnPreviewEditorUpdate()
    {
        if (previewTargetAnim == null && previewNormalAnim == null)
            return;
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - previewLastUpdateTime);
        previewLastUpdateTime = now;
        if (!previewPlaying)
            return;
        TickPreviewAnim(previewTargetAnim, dt);
        TickPreviewAnim(previewNormalAnim, dt);
        //非循环动画播放完毕后自动停止
        if (previewTargetAnim != null && previewTargetAnim.AnimationState.GetTrack(0) == null)
            previewPlaying = false;
        Repaint();
    }

    /// <summary>
    /// 推进单个模型动画一帧
    /// </summary>
    private void TickPreviewAnim(SkeletonAnimation sa, float dt)
    {
        if (sa == null)
            return;
        sa.Update(dt * previewTimeScale);
        sa.Renderer.LateUpdate();
    }

    /// <summary>
    /// 域重载/实例销毁后的自愈：有编辑目标但预览实例缺失且无错误时自动重建（由主类 OnGUI 末尾调用）
    /// </summary>
    private void EnsurePreviewAlive()
    {
        if (editingNpcInfo == null || previewTargetAnim != null || !previewLoadError.IsNull())
            return;
        if (editingNpcInfo.creature_id == 0)
            return;
        if (UnityEngine.Event.current != null && UnityEngine.Event.current.type == EventType.Layout)
            RebuildPreview();
    }
    #endregion

    #region 预览构建与销毁
    /// <summary>
    /// 重建预览：重建渲染工具与双模型实例（选中NPC/切换生物模型/刷新配置/自愈时调用）
    /// </summary>
    private void RebuildPreview()
    {
        DisposePreview();
        previewLoadError = null;
        previewAnimNames.Clear();
        previewAnimIndex = -1;
        previewModelId = -1;
        previewCreatureData = null;
        try
        {
            SpineEditorUtilities.ConfirmInitialization();
            //预览相机：正交 + 只渲染 PreviewLayer
            previewUtility = new PreviewRenderUtility(true);
            Camera cam = previewUtility.camera;
            cam.orthographic = true;
            cam.cullingMask = 1 << PreviewLayer;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            cam.orthographicSize = previewCameraOrtho;
            cam.transform.position = previewCameraPos;

            BuildReferenceModel();
            BuildTargetModel();
            FramePreviewCamera();
            previewLastUpdateTime = EditorApplication.timeSinceStartup;
            previewPlaying = true;
        }
        catch (Exception ex)
        {
            previewLoadError = $"加载失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 构建参考模型（生物2001基础皮肤；资源缺失时静默跳过，不影响目标预览）
    /// </summary>
    private void BuildReferenceModel()
    {
        var referenceInfo = CreatureInfoCfg.GetItemData(ReferenceCreatureId);
        if (referenceInfo == null)
            return;
        var modelInfo = CreatureModelCfg.GetItemData(referenceInfo.model_id);
        if (modelInfo == null)
            return;
        var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(modelInfo.res_name);
        if (asset == null)
            return;
        previewNormalAnim = InstantiatePreviewSkeleton(asset);
        if (previewNormalAnim == null)
            return;
        //编辑器安全装配：无参构造+手动赋值（禁止 new CreatureBean(creatureId)，内部读 name_language 会污染场景）
        var referenceData = new CreatureBean();
        referenceData.creatureId = ReferenceCreatureId;
        referenceData.creatureUUId = EditorPreviewUUId;
        referenceData.AddSkinForBase();
        ApplyCreatureToSkeleton(previewNormalAnim, referenceData);
        PlayIdleAnim(previewNormalAnim);
        ApplyModelPositions();
    }

    /// <summary>
    /// 构建目标NPC模型（无编辑目标/creature_id=0 时跳过；资源缺失写错误信息）
    /// </summary>
    private void BuildTargetModel()
    {
        if (editingNpcInfo == null || editingNpcInfo.creature_id == 0)
            return;
        var creatureInfo = CreatureInfoCfg.GetItemData(editingNpcInfo.creature_id);
        if (creatureInfo == null)
        {
            previewLoadError = $"找不到生物配置: {editingNpcInfo.creature_id}";
            return;
        }
        previewModelId = creatureInfo.model_id;
        var modelInfo = CreatureModelCfg.GetItemData(previewModelId);
        if (modelInfo == null)
        {
            previewLoadError = $"找不到生物模型配置: {previewModelId}";
            return;
        }
        var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(modelInfo.res_name);
        if (asset == null)
        {
            previewLoadError = $"找不到Spine资源（Mod资源编辑器无法预览）:\n{modelInfo.res_name}";
            return;
        }
        previewTargetAnim = InstantiatePreviewSkeleton(asset);
        if (previewTargetAnim == null)
        {
            previewLoadError = "无法加载骨架数据（版本不兼容或资源损坏）";
            return;
        }
        //收集动画列表
        SkeletonData data = previewTargetAnim.Skeleton.Data;
        foreach (Spine.Animation anim in data.Animations)
            previewAnimNames.Add(anim.Name);
        RefreshPreviewTarget();
        //默认播放待机动画
        string idleName = GetIdleAnimName(previewTargetAnim);
        previewAnimIndex = previewAnimNames.IndexOf(idleName);
        PlayIdleAnim(previewTargetAnim);
        ApplyModelPositions();
    }

    /// <summary>
    /// 实例化预览用 SkeletonAnimation（与 SpineWindow 预览同款：EditorInstantiation 绕过官方版本检查，HideAndDontSave + 预览层 + 移入预览场景）
    /// </summary>
    private SkeletonAnimation InstantiatePreviewSkeleton(SkeletonDataAsset asset)
    {
        var sa = EditorInstantiation.InstantiateSkeletonAnimation(
            asset, skinName: "", destroyInvalid: true, useObjectFactory: false);
        if (sa == null)
            return null;
        sa.gameObject.hideFlags = HideFlags.HideAndDontSave;
        sa.gameObject.layer = PreviewLayer;
        //关键：URP/新版本中预览相机只渲染预览场景内的物体
        previewUtility.AddSingleGO(sa.gameObject);
        //关键：renderer 必须保持 enabled——4.3 中 LateUpdate 在 renderer 禁用时直接跳过网格重建
        sa.Renderer.LateUpdate();
        return sa;
    }

    /// <summary>
    /// 销毁预览实例与渲染工具
    /// </summary>
    private void DisposePreview()
    {
        previewPlaying = false;
        if (previewTargetAnim != null)
        {
            DestroyImmediate(previewTargetAnim.gameObject);
            previewTargetAnim = null;
        }
        if (previewNormalAnim != null)
        {
            DestroyImmediate(previewNormalAnim.gameObject);
            previewNormalAnim = null;
        }
        previewCreatureData = null;
        if (previewUtility != null)
        {
            previewUtility.Cleanup();
            previewUtility = null;
        }
    }
    #endregion

    #region 生物装配与皮肤应用（编辑器安全，零单例）
    /// <summary>
    /// 刷新预览：外观/生物/体型变更后调用；模型没变只重跑皮肤应用（廉价），模型变了重建实例
    /// </summary>
    private void RefreshPreview()
    {
        if (editingNpcInfo == null)
            return;
        var creatureInfo = CreatureInfoCfg.GetItemData(editingNpcInfo.creature_id);
        long modelId = creatureInfo != null ? creatureInfo.model_id : 0;
        if (modelId != previewModelId || (modelId != 0 && previewTargetAnim == null))
        {
            RebuildPreview();
            return;
        }
        RefreshPreviewTarget();
        Repaint();
    }

    /// <summary>
    /// 刷新目标模型皮肤（重建编辑安全的 CreatureBean 并应用到预览骨架，不重建实例）
    /// </summary>
    private void RefreshPreviewTarget()
    {
        if (previewTargetAnim == null || editingNpcInfo == null)
            return;
        previewCreatureData = BuildCreatureForEditor(editingNpcInfo);
        ApplyCreatureToSkeleton(previewTargetAnim, previewCreatureData);
        //体型/皮肤变化后包围盒可能变化，重新取景
        FramePreviewCamera();
        Repaint();
    }

    /// <summary>
    /// 编辑器安全构建生物数据（手动装配，全程不碰单例）：
    /// 禁止 new CreatureBean(npcInfo)——其内部读 name_language → TextHandler.Instance 会在非Play模式 new GameObject 污染场景
    /// </summary>
    private CreatureBean BuildCreatureForEditor(NpcInfoBean npcInfo)
    {
        var creatureData = new CreatureBean();
        creatureData.creatureId = npcInfo.creature_id;
        creatureData.creatureName = GetNpcNameCn(npcInfo);
        creatureData.creatureUUId = EditorPreviewUUId;
        creatureData.level = npcInfo.level;
        creatureData.rarity = npcInfo.rarity;
        creatureData.bodySizeScale = npcInfo.GetBodySizeRandomScale();
        creatureData.creatureNpcData = new CreatureNpcBean(npcInfo.id);
        //注入编辑副本：绕过 npcInfo getter 的 Cfg 懒加载（未保存的新 id 会查不到，已有 id 会返回缓存原值而非编辑副本）
        creatureData.creatureNpcData.SetNpcInfoForEditor(npcInfo);
        //皮肤：启用随机皮肤时 = 固定皮肤 + 随机池补位（每次刷新重新随机，便于查看随机池各种组合，既定语义）
        var randomInfo = npcInfo.creature_random_id != 0 ? CreatureRandomInfoCfg.GetItemData(npcInfo.creature_random_id) : null;
        var listSkin = new List<long>(listCreatureSkinData);
        if (randomInfo != null)
        {
            //固定皮肤已占用的部位不参与随机（与 NpcInfoBean.GetSkins 同规则）
            var listOccupiedType = new List<CreatureSkinTypeEnum>();
            foreach (long skinId in listCreatureSkinData)
            {
                var skinModelInfo = CreatureModelInfoCfg.GetItemData(skinId);
                if (skinModelInfo != null)
                    listOccupiedType.Add(skinModelInfo.GetPartType());
            }
            listSkin.AddRange(randomInfo.GetRandomData(listOccupiedType));
        }
        creatureData.InitSkin(listSkin);
        creatureData.InitEquip(listCreatureEquipItemIds);
        //随机装备：配置了 equip_random 时从装备随机池抽装备填充空槽位（每次刷新重新随机）
        if (npcInfo.GetEquipRandomPoolId() != 0)
            creatureData.InitRandomEquip(npcInfo);
        //应用手动调色；随机皮肤模式下颜色由随机逻辑决定，不应用手动颜色（固定规则）
        if (randomInfo == null)
            ApplySkinColorsForPreview(creatureData);
        return creatureData;
    }

    /// <summary>
    /// 把手动调色应用到所有已装备的可调色皮肤上；未手动调色的可调色部位先把本次(随机)颜色固化进字典，避免每次刷新重新随机导致颜色抖动
    /// （固化只写字典不写回编辑副本——只有用户手动调色才写回，见 Appearance.ApplyEditSkinColor）
    /// </summary>
    private void ApplySkinColorsForPreview(CreatureBean creatureData)
    {
        foreach (var kv in creatureData.dicSkinData)
        {
            CreatureSkinTypeEnum skinType = kv.Key;
            var skinModelInfo = CreatureModelInfoCfg.GetItemData(kv.Value.skinId);
            if (skinModelInfo == null || skinModelInfo.color_state == 0)
                continue;
            if (!dicSkinColorEdit.ContainsKey(skinType))
                dicSkinColorEdit[skinType] = kv.Value.hasColor && kv.Value.skinColor != null ? kv.Value.skinColor.GetColor() : Color.white;
            creatureData.ChangeSkinColor(skinType, dicSkinColorEdit[skinType]);
        }
    }

    /// <summary>
    /// 把生物皮肤/颜色/缩放应用到预览骨架（等价 SpineHandler.ChangeSkeletonSkin 的纯 Spine API 实现，零单例）
    /// </summary>
    private void ApplyCreatureToSkeleton(SkeletonAnimation sa, CreatureBean creatureData)
    {
        if (sa == null || sa.skeleton == null || creatureData == null)
            return;
        var dicSkin = creatureData.GetSkinData(showType: 0, isNeedWeapon: true, isNeedEquip: isShowEquip);
        Skeleton skeleton = sa.skeleton;
        //合成皮肤：逐皮肤名 FindSkin 后加入组合皮肤
        Skin combined = new Skin("npc-editor-combined");
        foreach (var itemData in dicSkin)
        {
            if (itemData.Key.IsNull())
                continue;
            Skin itemSkin = skeleton.Data.FindSkin(itemData.Key);
            if (itemSkin != null)
                combined.AddSkin(itemSkin);
        }
        skeleton.SetSkin(combined);
        skeleton.SetupPoseSlots();
        //皮肤染色：slot 名 = 皮肤名最后一段 "/" 之后（与 SpineHandler.ChangeSkeletonSkin 同规则）
        foreach (var itemData in dicSkin)
        {
            if (itemData.Key.IsNull())
                continue;
            var itemSkinData = itemData.Value;
            if (!itemSkinData.hasColor)
                continue;
            string slotName = itemData.Key.Substring(itemData.Key.LastIndexOf('/') + 1);
            skeleton.FindSlot(slotName)?.SetColor(itemSkinData.skinColor.GetColor());
        }
        //模型缩放 = 目标大小 size_spine × NPC体型倍率（与 CreatureHandler.SetCreatureData 同规则）
        var model = creatureData.creatureModel;
        float scale = (model != null ? model.size_spine : 1f) * creatureData.GetBodySizeScale();
        sa.transform.localScale = Vector3.one * scale;
        sa.Update(0);
        sa.Renderer.LateUpdate();
    }

    /// <summary>
    /// 应用双模型摆位（参考模型开关切换时调用；关闭参考模型时目标居中、参考模型移出取景范围）
    /// </summary>
    private void ApplyModelPositions()
    {
        if (previewNormalAnim != null)
            previewNormalAnim.transform.position = isShowReferenceModel ? new Vector3(-ModelSideOffset, 0, 0) : new Vector3(-9999, 0, 0);
        if (previewTargetAnim != null)
            previewTargetAnim.transform.position = isShowReferenceModel ? new Vector3(ModelSideOffset, 0, 0) : Vector3.zero;
    }

    /// <summary>
    /// 相机取景：按可见模型的合并包围盒居中并适配大小
    /// </summary>
    private void FramePreviewCamera()
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        hasBounds |= MergeRendererBounds(previewTargetAnim, ref bounds, hasBounds);
        hasBounds |= MergeRendererBounds(previewNormalAnim, ref bounds, hasBounds);
        if (!hasBounds || bounds.size.y < 0.0001f)
        {
            //网格尚未生成时包围盒为空，用默认取景兜底
            previewCameraOrtho = 2f;
            previewCameraPos = new Vector3(0, 1f, -10f);
            return;
        }
        previewCameraOrtho = Mathf.Max(0.01f, bounds.size.y);
        previewCameraPos = bounds.center + new Vector3(0, 0, -10f);
    }

    /// <summary>
    /// 合并模型渲染包围盒（跳过不可见/未生成网格的模型），返回是否成功合并
    /// </summary>
    private bool MergeRendererBounds(SkeletonAnimation sa, ref Bounds bounds, bool hasBounds)
    {
        if (sa == null || !sa.gameObject.activeSelf)
            return false;
        //移出取景范围的参考模型不参与合并
        if (sa.transform.position.x < -1000)
            return false;
        //注意：SkeletonAnimation.Renderer 属性类型是 ISkeletonRenderer（无 bounds），需取 UnityEngine.Renderer 组件
        var renderer = sa.GetComponent<Renderer>();
        if (renderer == null)
            return false;
        Bounds itemBounds = renderer.bounds;
        if (itemBounds.size.y < 0.0001f)
            return false;
        if (!hasBounds)
            bounds = itemBounds;
        else
            bounds.Encapsulate(itemBounds);
        return true;
    }
    #endregion

    #region 动画控制
    /// <summary>
    /// 取模型的待机动画名（SpineAnimationStateCfg.CheckSpineAnim 静态纯数据方法，编辑器可用）
    /// </summary>
    private string GetIdleAnimName(SkeletonAnimation sa)
    {
        var setAnimName = new HashSet<string>();
        foreach (Spine.Animation anim in sa.Skeleton.Data.Animations)
            setAnimName.Add(anim.Name);
        return SpineAnimationStateCfg.CheckSpineAnim(SpineAnimationStateEnum.Idle, setAnimName);
    }

    /// <summary>
    /// 播放模型待机动画（循环；无待机动作时静默跳过）
    /// </summary>
    private void PlayIdleAnim(SkeletonAnimation sa)
    {
        string idleName = GetIdleAnimName(sa);
        if (idleName.IsNull())
            return;
        sa.AnimationState.SetAnimation(0, idleName, true);
    }

    /// <summary>
    /// 目标模型播放指定索引的动画（按当前循环设置）
    /// </summary>
    private void PlayPreviewAnimation(int index)
    {
        if (previewTargetAnim == null || index < 0 || index >= previewAnimNames.Count)
            return;
        previewAnimIndex = index;
        previewTargetAnim.AnimationState.SetAnimation(0, previewAnimNames[index], previewLoop);
        previewPlaying = true;
        previewLastUpdateTime = EditorApplication.timeSinceStartup;
    }

    /// <summary>
    /// 停止播放并回到初始姿势
    /// </summary>
    private void StopPreviewAnimation()
    {
        if (previewTargetAnim == null)
            return;
        previewPlaying = false;
        previewTargetAnim.AnimationState.ClearTracks();
        previewTargetAnim.Skeleton.SetupPose();
        previewTargetAnim.Update(0);
        previewTargetAnim.Renderer.LateUpdate();
        Repaint();
    }
    #endregion

    #region 预览区绘制
    /// <summary>
    /// 绘制右栏预览（预览画面 + 播放控制 + 动画列表）
    /// </summary>
    private void DrawPreviewColumn()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(widthPreview), GUILayout.ExpandHeight(true));
        //标题栏：参考模型开关
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Spine 预览", titleStyle);
        GUILayout.FlexibleSpace();
        bool newShowReference = EditorGUILayout.ToggleLeft("参考模型", isShowReferenceModel, GUILayout.Width(80));
        if (newShowReference != isShowReferenceModel)
        {
            isShowReferenceModel = newShowReference;
            ApplyModelPositions();
            FramePreviewCamera();
        }
        EditorGUILayout.EndHorizontal();

        if (!previewLoadError.IsNull())
            EditorGUILayout.HelpBox(previewLoadError, MessageType.Warning);
        if (editingNpcInfo == null)
        {
            EditorGUILayout.HelpBox("请选择左侧 NPC 后预览。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        if (editingNpcInfo.creature_id == 0)
        {
            EditorGUILayout.HelpBox("当前 NPC 未选择生物（creature_id=0），无实体预览。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        if (previewTargetAnim == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        DrawPreviewArea();
        DrawPreviewControls();
        DrawAnimList();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制预览画面（滚轮缩放、拖拽平移）
    /// </summary>
    private void DrawPreviewArea()
    {
        Rect rect = GUILayoutUtility.GetRect(200, float.MaxValue, 200, float.MaxValue,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        HandlePreviewInput(rect);
        if (UnityEngine.Event.current.type != EventType.Repaint)
            return;
        if (previewUtility == null)
            return;

        previewBackgroundStyle = previewBackgroundStyle ?? new GUIStyle("PreBackground");
        previewUtility.BeginPreview(rect, previewBackgroundStyle);
        Camera cam = previewUtility.camera;
        cam.orthographicSize = previewCameraOrtho;
        cam.transform.position = previewCameraPos;
        cam.Render();
        Texture tex = previewUtility.EndPreview();
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
    }

    /// <summary>
    /// 处理预览区域内的鼠标输入：滚轮缩放、左/右键拖拽平移
    /// </summary>
    private void HandlePreviewInput(Rect rect)
    {
        UnityEngine.Event e = UnityEngine.Event.current;
        if (!rect.Contains(e.mousePosition))
            return;
        if (e.type == EventType.ScrollWheel)
        {
            previewCameraOrtho = Mathf.Max(0.01f, previewCameraOrtho * (1f + e.delta.y * 0.05f));
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 2))
        {
            //像素增量换算成世界单位：orthoSize*2 是视图高度对应的世界高度
            float worldPerPixel = previewCameraOrtho * 2f / rect.height;
            previewCameraPos -= new Vector3(e.delta.x, -e.delta.y, 0) * worldPerPixel;
            e.Use();
            Repaint();
        }
    }

    /// <summary>
    /// 绘制底部播放控制条（播放/暂停、停止、循环、速度、进度、视角复位）
    /// </summary>
    private void DrawPreviewControls()
    {
        if (previewTargetAnim == null)
            return;
        TrackEntry track = previewTargetAnim.AnimationState.GetTrack(0);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(previewPlaying ? "暂停" : "播放", GUILayout.Width(50)))
        {
            if (previewPlaying)
            {
                previewPlaying = false;
            }
            else if (previewAnimIndex >= 0)
            {
                //轨道已被清空（播完）时重新 SetAnimation
                if (track == null)
                    PlayPreviewAnimation(previewAnimIndex);
                else
                    previewPlaying = true;
            }
            else
            {
                string idleName = GetIdleAnimName(previewTargetAnim);
                int idleIndex = previewAnimNames.IndexOf(idleName);
                if (idleIndex >= 0)
                    PlayPreviewAnimation(idleIndex);
            }
        }
        if (GUILayout.Button("停止", GUILayout.Width(50)))
            StopPreviewAnimation();
        bool newLoop = EditorGUILayout.ToggleLeft("循环", previewLoop, GUILayout.Width(50));
        if (newLoop != previewLoop)
        {
            previewLoop = newLoop;
            if (track != null)
                track.Loop = previewLoop;
        }
        GUILayout.Label("速度", GUILayout.Width(30));
        previewTimeScale = EditorGUILayout.Slider(previewTimeScale, 0f, 2f, GUILayout.Width(120));
        if (GUILayout.Button("重置视角", GUILayout.Width(70)))
            FramePreviewCamera();
        EditorGUILayout.EndHorizontal();

        //进度条：拖动可定位到任意帧
        if (track != null)
        {
            float duration = track.Animation.Duration;
            float time = Mathf.Min(track.TrackTime, duration);
            EditorGUILayout.BeginHorizontal();
            float newTime = EditorGUILayout.Slider(time, 0f, duration);
            if (newTime != time)
            {
                track.TrackTime = newTime;
                previewTargetAnim.Update(0);
                previewTargetAnim.Renderer.LateUpdate();
            }
            GUILayout.Label($"{time:F2}/{duration:F2}s", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 绘制动画列表（点击切换目标模型动画，当前项高亮）
    /// </summary>
    private void DrawAnimList()
    {
        DrawSectionHeader($"动画列表 ({previewAnimNames.Count})");
        scrollAnimList = EditorGUILayout.BeginScrollView(scrollAnimList, GUILayout.Height(120));
        Color defaultColor = GUI.backgroundColor;
        for (int i = 0; i < previewAnimNames.Count; i++)
        {
            bool isCurrent = i == previewAnimIndex;
            if (isCurrent)
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button((isCurrent ? "▶ " : "") + previewAnimNames[i], EditorStyles.miniButtonLeft))
                PlayPreviewAnimation(i);
            GUI.backgroundColor = defaultColor;
        }
        EditorGUILayout.EndScrollView();
    }
    #endregion
}
