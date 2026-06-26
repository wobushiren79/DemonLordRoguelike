using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 场上魔物 Spine 描边高亮：挂在共享单例描边预览的 Spine 节点上(与 SkeletonAnimation 同物体)，
/// 负责整套显示逻辑——加载目标骨骼、应用亮蓝 OutlineOnly 描边材质，并逐帧跟上目标的骨骼动画/位置/朝向，
/// 使描边轮廓与正在播放动画的本体保持一致。
/// <para>由 CreatureManager 懒加载预览预制后取本组件，外部统一通过 <see cref="Show"/> / <see cref="Hide"/> 调用。</para>
/// </summary>
[RequireComponent(typeof(SkeletonAnimation))]
public class CreatureSpineOutlineFollow : MonoBehaviour
{
    #region 数据
    /// <summary>自身(预览)骨骼动画</summary>
    SkeletonAnimation selfSkeletonAnimation;
    /// <summary>自身网格渲染器</summary>
    MeshRenderer selfRenderer;
    /// <summary>描边材质运行时实例(从预制材质克隆，按目标图集纹理填充 _MainTex)</summary>
    Material matOutline;
    /// <summary>当前已灌入的生物数据(切换不同生物才重建骨骼)</summary>
    CreatureBean creatureDataCurrent;
    /// <summary>目标骨骼动画(被高亮的场上生物)</summary>
    SkeletonAnimation targetSkeletonAnimation;
    /// <summary>目标根节点(取位置)</summary>
    Transform targetRoot;
    /// <summary>是否已订阅自身 UpdateLocal</summary>
    bool subscribed;
    #endregion

    #region 生命周期
    /// <summary>缓存自身骨骼动画/渲染器，并从预制材质克隆描边材质实例(须在 SetCreatureData 替换材质前)</summary>
    void Awake()
    {
        selfSkeletonAnimation = GetComponent<SkeletonAnimation>();
        selfRenderer = GetComponent<MeshRenderer>();
        //此刻骨骼数据未设置，渲染器材质仍是预制上的描边材质，克隆为运行时实例
        if (selfRenderer != null && selfRenderer.sharedMaterial != null)
            matOutline = new Material(selfRenderer.sharedMaterial);
    }

    /// <summary>启用时订阅</summary>
    void OnEnable()
    {
        Subscribe();
    }

    /// <summary>禁用时退订(预览隐藏即停止跟随)</summary>
    void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>释放克隆的描边材质实例</summary>
    void OnDestroy()
    {
        if (matOutline != null)
            Destroy(matOutline);
    }

    /// <summary>逐帧贴合目标的位置与大小/朝向(目标移动/翻转时同步)</summary>
    void LateUpdate()
    {
        if (targetRoot != null && transform.parent != null)
            transform.parent.position = targetRoot.position;
        if (targetSkeletonAnimation != null)
            transform.localScale = targetSkeletonAnimation.transform.localScale;
    }
    #endregion

    #region 显示/隐藏
    /// <summary>
    /// 显示对目标场上魔物的描边高亮：灌目标骨骼(切换生物才重建) → 应用描边材质 → 贴合位置/大小/朝向 → 激活并开始逐帧跟随。
    /// </summary>
    /// <param name="targetEntity">已上场的目标战斗生物</param>
    public void Show(FightCreatureEntity targetEntity)
    {
        if (targetEntity == null || targetEntity.creatureObj == null || targetEntity.creatureSkeletionAnimation == null)
            return;
        Transform root = transform.parent;
        if (root != null)
            CameraHandler.Instance.ChangeAngleForCamera(root);

        CreatureBean creatureData = targetEntity.fightCreatureData.creatureData;
        //切换不同生物时重建骨骼数据(同一生物→同一骨架，逐帧骨骼复制才对应得上；同一生物复用避免重复重建)
        if (creatureDataCurrent == null || creatureData != creatureDataCurrent)
        {
            CreatureHandler.Instance.SetCreatureData(selfSkeletonAnimation, creatureData);
            creatureDataCurrent = creatureData;
        }
        //应用描边材质(按目标图集纹理填充 _MainTex)
        RefreshMaterial(targetEntity);
        //贴合目标位置 + 大小/朝向(初始一帧；之后由 LateUpdate 逐帧维持)
        if (root != null)
            root.position = targetEntity.creatureObj.transform.position;
        transform.localScale = targetEntity.creatureSkeletionAnimation.transform.localScale;
        SetTarget(targetEntity);
        if (root != null)
            root.gameObject.SetActive(true);
    }

    /// <summary>隐藏描边高亮：清除跟随目标并隐藏预览</summary>
    public void Hide()
    {
        ClearTarget();
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
    }

    /// <summary>
    /// 应用描边材质：以目标生物的图集材质为键建立 CustomMaterialOverride，替换为描边材质并填充 _MainTex；同时贴合排序层级。
    /// <para>描边按 _MainTex 的 alpha 取轮廓；描边颜色由材质资源决定，不在代码里写死。</para>
    /// </summary>
    /// <param name="targetEntity">目标战斗生物</param>
    void RefreshMaterial(FightCreatureEntity targetEntity)
    {
        if (matOutline == null || selfSkeletonAnimation == null)
            return;
        MeshRenderer targetRenderer = targetEntity.creatureSkeletionAnimation.GetComponent<MeshRenderer>();
        if (targetRenderer == null)
            return;
        //逐图集材质建立替换映射(单图集生物仅一项)
        var customOverride = selfSkeletonAnimation.CustomMaterialOverride;
        customOverride.Clear();
        Material[] srcMats = targetRenderer.sharedMaterials;
        for (int i = 0; i < srcMats.Length; i++)
        {
            Material srcMat = srcMats[i];
            if (srcMat == null)
                continue;
            matOutline.SetTexture("_MainTex", srcMat.mainTexture);
            customOverride[srcMat] = matOutline;
        }
        //排序贴合目标并置于其后一层，使描边像光环般环绕本体
        if (selfRenderer != null)
        {
            selfRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            selfRenderer.sortingOrder = targetRenderer.sortingOrder - 1;
        }
    }
    #endregion

    #region 跟随
    /// <summary>设置跟随目标(目标战斗生物)</summary>
    /// <param name="targetEntity">被高亮的场上战斗生物</param>
    void SetTarget(FightCreatureEntity targetEntity)
    {
        if (targetEntity == null)
        {
            ClearTarget();
            return;
        }
        targetSkeletonAnimation = targetEntity.creatureSkeletionAnimation;
        targetRoot = targetEntity.creatureObj != null ? targetEntity.creatureObj.transform : null;
        Subscribe();
    }

    /// <summary>清除跟随目标(避免持有已回收生物的引用)</summary>
    void ClearTarget()
    {
        targetSkeletonAnimation = null;
        targetRoot = null;
    }

    /// <summary>订阅自身 UpdateLocal</summary>
    void Subscribe()
    {
        if (subscribed || selfSkeletonAnimation == null)
            return;
        selfSkeletonAnimation.UpdateLocal += HandleUpdateLocal;
        subscribed = true;
    }

    /// <summary>退订自身 UpdateLocal</summary>
    void Unsubscribe()
    {
        if (!subscribed || selfSkeletonAnimation == null)
            return;
        selfSkeletonAnimation.UpdateLocal -= HandleUpdateLocal;
        subscribed = false;
    }

    /// <summary>
    /// 在自身骨骼"应用动画后、计算世界变换前"逐帧把目标骨骼的本地姿态(SRT)复制过来，使描边轮廓与目标当前动画帧一致。
    /// <para>同一生物同一骨架，骨骼数量/顺序一致才逐根复制。</para>
    /// </summary>
    void HandleUpdateLocal(ISkeletonAnimation animation)
    {
        if (targetSkeletonAnimation == null || targetSkeletonAnimation.skeleton == null || selfSkeletonAnimation.skeleton == null)
            return;
        var targetBones = targetSkeletonAnimation.skeleton.Bones;
        var selfBones = selfSkeletonAnimation.skeleton.Bones;
        if (targetBones.Count != selfBones.Count)
            return;
        for (int i = 0; i < selfBones.Count; i++)
        {
            Bone tb = targetBones.Items[i];
            Bone sb = selfBones.Items[i];
            sb.X = tb.X;
            sb.Y = tb.Y;
            sb.Rotation = tb.Rotation;
            sb.ScaleX = tb.ScaleX;
            sb.ScaleY = tb.ScaleY;
            sb.ShearX = tb.ShearX;
            sb.ShearY = tb.ShearY;
        }
    }
    #endregion
}
