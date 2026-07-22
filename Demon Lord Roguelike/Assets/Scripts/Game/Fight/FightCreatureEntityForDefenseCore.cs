using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 战斗生物实体-魔王（防守核心）专属逻辑
/// <para>魔王：被防守的核心生物（CreatureFightTypeEnum.FightDefenseCore），魔王死亡则战斗失败。</para>
/// <para>魔力(MP)显示：魔王预制下的 MPShow 进度条（Mat_Creature_Mana_1，新版 FrameWork/URP/MeshProgressBar 圆形进度，单一 _Progress 无护盾层）+ MPText 文本（当前/上限格式）。</para>
/// <para>渲染层级：MPText 在预制体里使用 Overlay 着色器材质(MatTMP_MPTextOverlay，TMP_SDF Overlay：ZTest Always + Overlay 队列)，
/// 不做深度测试，保证文本始终渲染在不透明 3D 地面/场景几何体之上——这是文本压过地面的真正机制；
/// 代码里的 sortingOrder 仅作透明队列内部排序的补充（单纯 sortingOrder 压不过不透明地面写入的深度缓冲）。</para>
/// </summary>
public partial class FightCreatureEntity
{
    #region 魔王-魔力显示
    /// <summary>
    /// 魔力条显示（仅魔王核心预制下有MPShow节点 MeshRenderer+Quad Mat_Creature_Mana_1 新版圆形 MeshProgressBar 材质）
    /// </summary>
    public MeshRenderer creatureMPShow;
    /// <summary>
    /// 魔力值文本（MPShow/MPText 以 当前/上限 格式显示）
    /// </summary>
    public TextMeshPro creatureMPText;
    /// <summary>
    /// 上一次显示的魔力值（避免每帧重设文本产生开销）
    /// </summary>
    int lastMPShowCurrent = -1;
    /// <summary>
    /// 上一次显示的魔力上限（避免每帧重设文本产生开销）
    /// </summary>
    int lastMPShowMax = -1;
    /// <summary>
    /// 魔力文本的渲染排序值（设到足够高，在同队列内排到最上层；防地面遮挡的核心靠 Overlay 着色器材质，见类注释）
    /// </summary>
    const int MPTextSortingOrder = 9999;

    /// <summary>
    /// 数据初始化-魔王（由 SetData 统一调用）
    /// <para>挂接魔力显示节点并重置显示缓存（非核心生物预制下无MPShow节点 查找为空自动跳过显示）。</para>
    /// </summary>
    public void SetDataForDefenseCore()
    {
        //获取魔力值显示（仅魔王核心有 创建魔物消耗魔力）
        creatureMPShow = creatureObj.transform.Find("MPShow")?.GetComponent<MeshRenderer>();
        creatureMPText = creatureObj.transform.Find("MPShow/MPText")?.GetComponent<TextMeshPro>();
        //补充设置魔力文本的渲染排序（透明队列内部排序用；不被地面遮挡的关键是预制体上的 Overlay 着色器材质 ZTest Always）
        if (creatureMPText != null)
        {
            var mpTextRenderer = creatureMPText.GetComponent<MeshRenderer>();
            if (mpTextRenderer != null)
                mpTextRenderer.sortingOrder = MPTextSortingOrder;
        }
        lastMPShowCurrent = -1;
        lastMPShowMax = -1;
        RefreshMPShow();
        //初始化深渊馈赠环绕图标（仅魔王核心内部会执行 其他生物直接跳过）
        InitAbyssalBlessingOrbit();
    }

    /// <summary>
    /// 刷新魔力显示（魔王核心专用 与防守生物的LifeShow一样在数值变化时通知刷新）
    /// <para>魔力条进度实时刷新；MPText 文本仅在整数值变化时重设，格式为 当前/上限（如 100/100）。</para>
    /// <para>非魔王核心生物（预制下无MPShow节点）调用时直接跳过。</para>
    /// </summary>
    public void RefreshMPShow()
    {
        if (creatureMPShow == null)
            return;
        float MPMax = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.MP);
        //设置魔力条进度（新版 MeshProgressBar 单一 _Progress，0~1）
        if (MPMax > 0)
        {
            creatureMPShow.material.SetFloat("_Progress", fightCreatureData.MPCurrent / MPMax);
        }
        else
        {
            creatureMPShow.material.SetFloat("_Progress", 0);
        }
        //设置魔力文本（仅在显示的整数值变化时重设 避免每帧产生文本开销）
        if (creatureMPText != null)
        {
            int MPCurrentInt = (int)fightCreatureData.MPCurrent;
            int MPMaxInt = (int)MPMax;
            if (MPCurrentInt != lastMPShowCurrent || MPMaxInt != lastMPShowMax)
            {
                lastMPShowCurrent = MPCurrentInt;
                lastMPShowMax = MPMaxInt;
                creatureMPText.text = $"{MPCurrentInt}/{MPMaxInt}";
            }
        }
    }
    #endregion

    #region 魔王-死亡相关
    /// <summary>
    /// 死亡意图切换-魔王（由 SetCreatureDead 统一分发 非魔王核心自动跳过）
    /// </summary>
    public void SetCreatureDeadForDefenseCore()
    {
        if (aiEntity is AIDefenseCoreCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefenseCoreCreatureDead);
        }
    }
    #endregion

    #region 魔王-深渊馈赠环绕图标(GPU单Mesh)
    /// <summary>
    /// 环绕图标数据（一条馈赠实例对应一个图标）
    /// </summary>
    protected class AbyssalOrbitIconData
    {
        public string blessingUUID;//馈赠实例UUID(对账键)
        public Sprite sprite;//已加载图集sprite(null=加载中)
        public float phase;//浮动相位(错开各图标起伏)
        public float spawnTime;//入场时间(Time.timeSinceLevelLoad轴 与shader的_Time.y同轴)
    }

    /// <summary>环绕图标列表（与 BuffManager.dicAbyssalBlessingBuffsActivie.ListKey 全量对账）</summary>
    protected readonly List<AbyssalOrbitIconData> listAbyssalOrbitIcons = new List<AbyssalOrbitIconData>();
    /// <summary>环绕容器（魔王预制 FightCreature_DefCore_1 下已配好的 AbyssalBlessingOrbit 节点，MeshRenderer/MeshFilter/材质球由编辑器设置）</summary>
    protected GameObject abyssalOrbitObj;
    /// <summary>环绕渲染器（缓存 避免每次GetComponent）</summary>
    protected MeshRenderer abyssalOrbitMeshRenderer;
    /// <summary>环绕共享Mesh（全局一份复用 避免每局新建泄漏；mesh内容按馈赠数量重建）</summary>
    protected static Mesh abyssalOrbitMesh;
    /// <summary>环绕材质属性块（_MainTex/_OrbitCount经MPB写入 不污染预制共享材质资产）</summary>
    protected static MaterialPropertyBlock abyssalOrbitMPB;

    /// <summary>
    /// 初始化环绕图标（由 SetDataForDefenseCore 统一调用；仅魔王核心执行 其他生物直接跳过）
    /// <para>实现形态：预制节点(编辑器配好MeshRenderer/MeshFilter/材质球 FrameWork/URP/MeshOrbit shader) + 单Mesh装N个图标quad，
    /// 公转/浮动/入场缩放全在vertex shader按_Time.y匀速计算(不随游戏倍速)，每帧CPU零开销、1个drawcall；代码只负责按馈赠列表重建mesh顶点与写MPB。</para>
    /// </summary>
    public void InitAbyssalBlessingOrbit()
    {
        if (fightCreatureData.creatureFightType != CreatureFightTypeEnum.FightDefenseCore)
            return;
        //查找预制下已配好的环绕节点（与MPShow同惯例：非魔王预制无该节点 查找为空自动跳过）
        abyssalOrbitObj = creatureObj.transform.Find("AbyssalBlessingOrbit")?.gameObject;
        if (abyssalOrbitObj == null)
            return;
        abyssalOrbitMeshRenderer = abyssalOrbitObj.GetComponent<MeshRenderer>();
        var orbitMeshFilter = abyssalOrbitObj.GetComponent<MeshFilter>();
        if (abyssalOrbitMeshRenderer == null || orbitMeshFilter == null)
        {
            LogUtil.LogError("魔王预制 AbyssalBlessingOrbit 节点缺少 MeshRenderer/MeshFilter");
            abyssalOrbitObj = null;
            return;
        }
        //共享mesh首次创建（后续每局复用；编辑器退出Play后Unity会销毁静态引用的资产，靠==null识别重建）
        if (abyssalOrbitMesh == null)
            abyssalOrbitMesh = new Mesh { name = "AbyssalBlessingOrbit" };
        orbitMeshFilter.sharedMesh = abyssalOrbitMesh;
        if (abyssalOrbitMPB == null)
            abyssalOrbitMPB = new MaterialPropertyBlock();
        abyssalOrbitMeshRenderer.enabled = false;//无图标前不渲染
        //拉取已有馈赠（征服模式关卡间衔接：进战斗前已选的馈赠）
        RefreshAbyssalBlessingOrbit();
    }

    /// <summary>
    /// 刷新环绕图标（深渊馈赠变化事件 Buff_AbyssalBlessingChange 驱动，由 GameFightLogic.EventForAbyssalBlessingChange 调用）
    /// <para>与激活列表全量对账：新key补建、消失的key移除——升级替换"先删旧级后加新级"由对账天然兼容，无需区分增删事件。</para>
    /// </summary>
    public void RefreshAbyssalBlessingOrbit()
    {
        if (abyssalOrbitObj == null)
            return;
        var buffManager = BuffHandler.Instance.manager;
        if (buffManager == null)
            return;
        var activeKeys = buffManager.dicAbyssalBlessingBuffsActivie.ListKey;
        //移除已消失的馈赠
        listAbyssalOrbitIcons.RemoveAll(data =>
        {
            for (int i = 0; i < activeKeys.Count; i++)
            {
                if (activeKeys[i].abyssalBlessingUUID == data.blessingUUID)
                    return false;
            }
            return true;
        });
        //补建新增的馈赠
        for (int i = 0; i < activeKeys.Count; i++)
        {
            var key = activeKeys[i];
            if (key == null || key.abyssalBlessingInfo == null)
                continue;
            if (listAbyssalOrbitIcons.Exists(data => data.blessingUUID == key.abyssalBlessingUUID))
                continue;
            var iconData = new AbyssalOrbitIconData
            {
                blessingUUID = key.abyssalBlessingUUID,
                phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                spawnTime = Time.timeSinceLevelLoad,
            };
            listAbyssalOrbitIcons.Add(iconData);
            //异步加载图集sprite（命中缓存近同步；框架层加载失败自动兜底unknow图）
            IconHandler.Instance.GetIconSprite(SpriteAtlasTypeEnum.AbyssalBlessing, key.abyssalBlessingInfo.icon_res, (sprite) =>
            {
                iconData.sprite = sprite;
                RebuildAbyssalOrbitMesh();
            });
        }
        RebuildAbyssalOrbitMesh();
    }

    /// <summary>
    /// 重建环绕Mesh（每图标4顶点：POSITION=单位quad角点, UV=图集textureRect换算, UV2=序号+相位, UV3.x=入场时间）
    /// <para>sprite异步加载完成回调时也会触发重建；回调若晚于实体销毁（战斗结束），靠abyssalOrbitObj假null早退。</para>
    /// </summary>
    protected void RebuildAbyssalOrbitMesh()
    {
        if (abyssalOrbitMesh == null || abyssalOrbitObj == null)
            return;
        abyssalOrbitMesh.Clear();
        //只建sprite已加载完成的图标
        var loadedIcons = new List<AbyssalOrbitIconData>();
        for (int i = 0; i < listAbyssalOrbitIcons.Count; i++)
        {
            if (listAbyssalOrbitIcons[i].sprite != null)
                loadedIcons.Add(listAbyssalOrbitIcons[i]);
        }
        if (loadedIcons.Count == 0)
        {
            abyssalOrbitMeshRenderer.enabled = false;
            return;
        }
        int count = loadedIcons.Count;
        Vector3[] verts = new Vector3[count * 4];
        Vector2[] uvs = new Vector2[count * 4];
        Vector2[] orbitDatas = new Vector2[count * 4];
        Vector2[] spawnTimes = new Vector2[count * 4];
        int[] tris = new int[count * 6];
        Texture atlasTex = null;
        for (int i = 0; i < count; i++)
        {
            var iconData = loadedIcons[i];
            int v = i * 4, t = i * 6;
            //单位quad四角（±0.5，shader里乘图标尺寸并billboard展开）
            verts[v + 0] = new Vector3(-0.5f, -0.5f, 0);
            verts[v + 1] = new Vector3(0.5f, -0.5f, 0);
            verts[v + 2] = new Vector3(0.5f, 0.5f, 0);
            verts[v + 3] = new Vector3(-0.5f, 0.5f, 0);
            //图集UV（textureRect像素矩形→UV；SpriteAtlas图块不旋转，矩形换算安全）
            atlasTex = iconData.sprite.texture;
            Rect rect = iconData.sprite.textureRect;
            float u0 = rect.x / atlasTex.width, u1 = (rect.x + rect.width) / atlasTex.width;
            float v0 = rect.y / atlasTex.height, v1 = (rect.y + rect.height) / atlasTex.height;
            uvs[v + 0] = new Vector2(u0, v0);
            uvs[v + 1] = new Vector2(u1, v0);
            uvs[v + 2] = new Vector2(u1, v1);
            uvs[v + 3] = new Vector2(u0, v1);
            //序号+相位（公转均分/浮动错开）与入场时间
            for (int f = 0; f < 4; f++)
            {
                orbitDatas[v + f] = new Vector2(i, iconData.phase);
                spawnTimes[v + f] = new Vector2(iconData.spawnTime, 0);
            }
            tris[t + 0] = v + 0; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
            tris[t + 3] = v + 0; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
        }
        abyssalOrbitMesh.vertices = verts;
        abyssalOrbitMesh.uv = uvs;
        abyssalOrbitMesh.uv2 = orbitDatas;
        abyssalOrbitMesh.uv3 = spawnTimes;
        abyssalOrbitMesh.triangles = tris;
        //图集纹理与图标数量经MPB写入（全部图标同一图集 任一sprite的texture即是；MPB不污染预制共享材质资产）
        abyssalOrbitMPB.SetTexture("_MainTex", atlasTex);
        abyssalOrbitMPB.SetFloat("_OrbitCount", count);
        abyssalOrbitMeshRenderer.SetPropertyBlock(abyssalOrbitMPB);
        abyssalOrbitMeshRenderer.enabled = true;
    }
    #endregion
}
