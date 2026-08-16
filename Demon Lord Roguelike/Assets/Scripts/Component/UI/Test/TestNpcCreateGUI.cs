using System;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using static ExcelUtil;

/// <summary>
/// NPC创建（GUI版，纯代码UI）
/// 与预制版 UITestNpcCreate 功能一致，但全部控件用 IMGUI(OnGUI) 代码创建、Spine 模型用代码在场景中生成，
/// 不依赖任何运行时UI预制，方便直接在脚本里增删改控件。由 LauncherTest.StartNpcCreateGUI 挂到空物体上启动。
/// </summary>
public class TestNpcCreateGUI : MonoBehaviour
{
    #region 预览摆放可调参数（预览不对位时直接改这里）
    private const float ModelDistance = 6f;    //模型距相机的正前方距离
    private const float ModelSideOffset = 1.2f;//两个模型左右分开的距离
    private const float ModelHeightOffset = -1f;//模型相对相机视线的高度偏移
    #endregion

    #region 数据字段
    private CreatureBean creatureData;             //当前生物数据
    private bool isShowEquip = true;               //是否展示装备
    private List<long> listCreatureSkinData = new List<long>();   //自定义皮肤ID列表
    private List<long> listCreatureEquipItemIds = new List<long>();//自定义装备ID列表
    private float previewScale = 5f;               //预览缩放(SetCreatureData每次按配置重置缩放, 刷新后由ApplyPreviewScale统一覆盖)
    private readonly Dictionary<CreatureSkinTypeEnum, Color> dicSkinColorEdit = new Dictionary<CreatureSkinTypeEnum, Color>();//各部位手动调色(仅color_state!=0的皮肤)
    private CreatureSkinTypeEnum editingColorSkinType = CreatureSkinTypeEnum.None;//当前展开调色编辑的部位(None=全部收起)

    /// <summary>皮肤调色盘预设色(发色/唇色等常用色，点选即应用)</summary>
    private static readonly Color[] paletteSkinColors =
    {
        new Color(1.00f, 1.00f, 1.00f), //白
        new Color(0.75f, 0.75f, 0.78f), //银灰
        new Color(0.45f, 0.45f, 0.48f), //灰
        new Color(0.10f, 0.10f, 0.12f), //黑
        new Color(0.35f, 0.20f, 0.10f), //深棕
        new Color(0.55f, 0.35f, 0.18f), //棕
        new Color(0.85f, 0.65f, 0.35f), //亚麻
        new Color(0.95f, 0.85f, 0.45f), //金
        new Color(0.85f, 0.30f, 0.15f), //橙红
        new Color(0.70f, 0.15f, 0.15f), //红
        new Color(0.55f, 0.10f, 0.20f), //酒红
        new Color(0.95f, 0.55f, 0.65f), //粉
        new Color(0.60f, 0.30f, 0.70f), //紫
        new Color(0.25f, 0.35f, 0.75f), //蓝
        new Color(0.30f, 0.70f, 0.80f), //青
        new Color(0.25f, 0.60f, 0.35f), //绿
    };

    private SkeletonAnimation normalModel;         //标准参考模型(创建后不变)
    private SkeletonAnimation targetModel;         //目标编辑模型(随配置刷新)
    #endregion

    #region GUI输入缓存
    private string npcIdInput = "2000000001";      //当前选中的NPC ID
    private string npcDropdownLabel = "请选择NPC"; //NPC下拉按钮当前显示(id+名字)
    private bool isNpcDropdownOpen;                //NPC下拉列表是否展开
    private Vector2 scrollNpcDropdown;             //NPC下拉列表滚动
    private List<SelectItem> listNpcOptions;       //NPC候选列表(懒加载)
    private bool isCreatureDropdownOpen;           //生物(creatureInfo)下拉列表是否展开
    private Vector2 scrollCreatureDropdown;        //生物下拉列表滚动
    private List<SelectItem> listCreatureOptions;  //生物候选列表(懒加载)
    private bool isRandomSkinDropdownOpen;         //随机皮肤下拉列表是否展开
    private Vector2 scrollRandomSkinDropdown;      //随机皮肤下拉列表滚动
    private List<SelectItem> listRandomSkinOptions;//随机皮肤候选列表(懒加载)
    private bool isRandomEquipDropdownOpen;        //随机装备下拉列表是否展开
    private Vector2 scrollRandomEquipDropdown;     //随机装备下拉列表滚动
    private List<SelectItem> listRandomEquipOptions;//随机装备候选列表(懒加载)
    private string inputHP, inputDR, inputMP, inputATK, inputASPD, inputMSPD, inputSearchRange;//属性输入框
    private Vector2 scrollMain;                     //主面板滚动
    private Vector2 scrollSelect;                   //选择面板滚动
    #endregion

    #region 选择面板状态
    private enum SelectMode { None, Skin, Equip }
    private SelectMode selectMode = SelectMode.None;//当前选择模式
    private int selectShowType;                     //当前选择的皮肤类型/装备类型
    private List<SelectItem> listSelectItem = new List<SelectItem>();//候选项

    /// <summary>选择面板候选项(id + 显示名 + 可选图标)</summary>
    private struct SelectItem
    {
        public long id;
        public string label;
        public SpriteAtlasTypeEnum atlasType;   //图标所属图集(iconName为空时无效)
        public string iconName;                 //图标名(空=无图标)
        public SelectItem(long id, string label) : this(id, label, SpriteAtlasTypeEnum.UI, null) { }
        public SelectItem(long id, string label, SpriteAtlasTypeEnum atlasType, string iconName)
        {
            this.id = id;
            this.label = label;
            this.atlasType = atlasType;
            this.iconName = iconName;
        }
    }
    #endregion

    #region 图标缓存
    private readonly Dictionary<string, Sprite> dicIconCache = new Dictionary<string, Sprite>();//图标缓存(key=图集/图标名, null值表示图集无此图)
    private readonly HashSet<string> setIconLoading = new HashSet<string>();                    //加载中的图标key(防重复请求)
    #endregion

    #region 图标加载与绘制
    /// <summary>
    /// 取图标(带缓存)。未加载完成返回null并发起异步加载，完成后下帧自动显示；图集缺图缓存null防反复请求
    /// </summary>
    private Sprite GetIconCached(SpriteAtlasTypeEnum atlasType, string iconName)
    {
        if (iconName.IsNull()) return null;
        string cacheKey = $"{atlasType}/{iconName}";
        if (dicIconCache.TryGetValue(cacheKey, out var cachedSprite))
            return cachedSprite;
        if (setIconLoading.Contains(cacheKey))
            return null;
        setIconLoading.Add(cacheKey);
        IconHandler.Instance.GetIconSprite(atlasType, iconName, (loadedSprite) =>
        {
            setIconLoading.Remove(cacheKey);
            dicIconCache[cacheKey] = loadedSprite;
        });
        return null;
    }

    /// <summary>
    /// 在GUILayout流中绘制图集sprite的子图区域(未加载完成时只占位空白)
    /// </summary>
    private void DrawSpriteIcon(Sprite sprite, float iconSize)
    {
        Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
        if (sprite == null || Event.current.type != EventType.Repaint) return;
        //sprite来自图集，需用textureRect换算UV只绘制子图区域
        Rect texRect = sprite.textureRect;
        Rect texCoords = new Rect(
            texRect.x / sprite.texture.width, texRect.y / sprite.texture.height,
            texRect.width / sprite.texture.width, texRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(iconRect, sprite.texture, texCoords);
    }

    /// <summary>
    /// 解析图标资源名中的 ",图集类型" 后缀(规则同 IconHandler.ParseIconName)，无后缀用默认图集
    /// </summary>
    private void ParseIconRes(string iconRes, SpriteAtlasTypeEnum defaultType, out SpriteAtlasTypeEnum atlasType, out string actualIconName)
    {
        atlasType = defaultType;
        actualIconName = iconRes;
        if (iconRes.IsNull()) return;
        int commaIndex = iconRes.LastIndexOf(',');
        if (commaIndex <= 0 || commaIndex >= iconRes.Length - 1) return;
        if (Enum.TryParse<SpriteAtlasTypeEnum>(iconRes.Substring(commaIndex + 1), out var parsedType))
        {
            atlasType = parsedType;
            actualIconName = iconRes.Substring(0, commaIndex);
        }
    }
    #endregion

    #region GUI样式
    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, sectionStyle;
    #endregion

    #region 生命周期
    /// <summary>
    /// 初始化：生成标准参考模型，等待加载目标NPC
    /// </summary>
    private void Start()
    {
        //标准参考模型(与预制版一致：生物2001基础皮肤)
        CreatureBean creatureNormalTest = new CreatureBean(2001);
        creatureNormalTest.AddSkinForBase();
        normalModel = CreateModel("NpcCreateGUI_NormalModel", -ModelSideOffset);
        CreatureHandler.Instance.SetCreatureData(normalModel, creatureNormalTest);
        SpineHandler.Instance.PlayAnim(normalModel, SpineAnimationStateEnum.Idle, creatureNormalTest, true);
        ApplyPreviewScale();

        //目标模型空壳(加载NPC后填充)
        targetModel = CreateModel("NpcCreateGUI_TargetModel", ModelSideOffset);
    }

    /// <summary>
    /// 销毁时清理生成的模型
    /// </summary>
    private void OnDestroy()
    {
        if (normalModel != null) Destroy(normalModel.gameObject);
        if (targetModel != null) Destroy(targetModel.gameObject);
    }
    #endregion

    #region 模型生成与刷新
    /// <summary>
    /// 在相机正前方生成一个空的Spine模型物体
    /// </summary>
    private SkeletonAnimation CreateModel(string objName, float sideOffset)
    {
        var cam = CameraHandler.Instance.manager.mainCamera;
        GameObject obj = new GameObject(objName);
        //按相机朝向摆放，让扁平Spine正面朝向相机
        if (cam != null)
        {
            Vector3 pos = cam.transform.position
                + cam.transform.forward * ModelDistance
                + cam.transform.right * sideOffset
                + cam.transform.up * ModelHeightOffset;
            obj.transform.position = pos;
            obj.transform.rotation = cam.transform.rotation;
        }
        //Spine 4.3 组件分离结构：必须用 AddToGameObject 同时创建 SkeletonRenderer + SkeletonAnimation
        //(单独 AddComponent<SkeletonAnimation> 会因缺少 SkeletonRenderer 在 Awake 报错；空壳传 null asset，后续由 SetCreatureData 填充)
        return SkeletonAnimation.AddToGameObject(obj, null).skeletonAnimation;
    }

    /// <summary>
    /// 刷新目标模型与所有展示数据(等价预制版 RefreshCreature)
    /// </summary>
    private void RefreshCreature()
    {
        if (creatureData == null) return;
        //从UI更新属性
        UpdateCreatureAttributeFromUI();
        //皮肤: 启用随机皮肤时 = 固定皮肤 + 随机池随机结果(每次刷新重新随机, 便于查看随机池各种组合)
        long randomSkinId = creatureData.creatureNpcData?.npcInfo?.creature_random_id ?? 0;
        var randomInfo = randomSkinId != 0 ? CreatureRandomInfoCfg.GetItemData(randomSkinId) : null;
        if (randomInfo != null)
        {
            //固定皮肤已占用的部位不参与随机(与 NpcInfoBean.GetSkins 同规则)
            var listOccupiedType = new List<CreatureSkinTypeEnum>();
            foreach (var skinId in listCreatureSkinData)
            {
                var skinModelInfo = CreatureModelInfoCfg.GetItemData(skinId);
                if (skinModelInfo != null) listOccupiedType.Add(skinModelInfo.GetPartType());
            }
            List<long> listSkinWithRandom = new List<long>(listCreatureSkinData);
            listSkinWithRandom.AddRange(randomInfo.GetRandomData(listOccupiedType));
            creatureData.InitSkin(listSkinWithRandom);
        }
        else
        {
            creatureData.InitSkin(listCreatureSkinData);
        }
        creatureData.InitEquip(listCreatureEquipItemIds);
        //随机装备: NPC配置了equip_random时, 从装备随机池抽装备填充空槽位(每次刷新重新随机, 便于查看随机池各种组合)
        var npcInfoForRandomEquip = creatureData.creatureNpcData?.npcInfo;
        if (npcInfoForRandomEquip != null && npcInfoForRandomEquip.GetEquipRandomPoolId() != 0)
            creatureData.InitRandomEquip(npcInfoForRandomEquip);
        //应用所有可调色皮肤(color_state!=0)的手动颜色；随机皮肤模式下颜色由随机逻辑决定，不应用手动颜色
        if (randomInfo == null)
            ApplySkinColors();
        //设置spine并播放待机
        CreatureHandler.Instance.SetCreatureData(targetModel, creatureData, isNeedEquip: isShowEquip);
        SpineHandler.Instance.PlayAnim(targetModel, SpineAnimationStateEnum.Idle, creatureData, true);
        ApplyPreviewScale();
    }

    /// <summary>
    /// 把手动调色应用到所有已装备的可调色皮肤上；未手动调色的可调色部位先把本次(随机)颜色固化进字典，避免每次刷新重新随机导致颜色抖动
    /// </summary>
    private void ApplySkinColors()
    {
        foreach (var kv in creatureData.dicSkinData)
        {
            CreatureSkinTypeEnum skinType = kv.Key;
            var skinModelInfo = CreatureModelInfoCfg.GetItemData(kv.Value.skinId);
            if (skinModelInfo == null || skinModelInfo.color_state == 0) continue;
            if (!dicSkinColorEdit.ContainsKey(skinType))
                dicSkinColorEdit[skinType] = kv.Value.hasColor && kv.Value.skinColor != null ? kv.Value.skinColor.GetColor() : Color.white;
            creatureData.ChangeSkinColor(skinType, dicSkinColorEdit[skinType]);
        }
    }

    /// <summary>
    /// 应用预览缩放到两个模型(SetCreatureData每次会按配置重置缩放，需在刷新后重新覆盖)
    /// </summary>
    private void ApplyPreviewScale()
    {
        if (normalModel != null) normalModel.transform.localScale = Vector3.one * previewScale;
        if (targetModel != null) targetModel.transform.localScale = Vector3.one * previewScale;
    }
    #endregion

    #region 加载/属性
    /// <summary>
    /// 用默认程序打开 excel_npc_info 配置表(仅编辑器有效)
    /// </summary>
    private void OpenNpcInfoExcel()
    {
        string fullPath = Application.dataPath + "/Data/Excel/excel_npc_info[NPC信息].xlsx";
        if (!System.IO.File.Exists(fullPath))
        {
            LogUtil.LogError($"找不到NPC配置表: {fullPath}");
            return;
        }
        Application.OpenURL("file:///" + fullPath.Replace("\\", "/"));
    }

    /// <summary>
    /// 懒加载NPC候选下拉列表(id + 名字，按id排序)
    /// </summary>
    private void EnsureNpcOptions()
    {
        if (listNpcOptions != null) return;
        listNpcOptions = new List<SelectItem>();
        var allNpcData = NpcInfoCfg.GetAllArrayData();
        foreach (var npcInfo in allNpcData)
        {
            //先检查多语言行是否存在，避免 name_language 对缺失行刷 LogError
            string npcName;
            if (npcInfo.name == 0)
            {
                //随机议员没有配置名字，走通用命名(NpcInfoBean.GetCouncilorRandomDisplayName 评级称谓名)
                npcName = npcInfo.GetNpcType() == NpcTypeEnum.CouncilorRandom
                    ? npcInfo.GetCouncilorRandomDisplayName()
                    : "(无名字)";
            }
            else if (LanguageCfg.GetItemData(NpcInfoCfg.fileName, npcInfo.name) != null)
            {
                npcName = npcInfo.name_language;
                if (npcName.IsNull()) npcName = "未命名";
            }
            else
            {
                npcName = "(未配置名字)";
            }
            listNpcOptions.Add(new SelectItem(npcInfo.id, $"{npcInfo.id}  {npcName}"));
        }
        listNpcOptions.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 懒加载生物(creatureInfo)候选下拉列表(id + 名字，按id排序)
    /// </summary>
    private void EnsureCreatureOptions()
    {
        if (listCreatureOptions != null) return;
        listCreatureOptions = new List<SelectItem>();
        var allCreatureData = CreatureInfoCfg.GetAllArrayData();
        foreach (var creatureInfo in allCreatureData)
        {
            //先检查多语言行是否存在，避免 name_language 对缺失行刷 LogError
            string creatureName;
            if (creatureInfo.name == 0)
            {
                creatureName = "(无名字)";
            }
            else if (LanguageCfg.GetItemData(CreatureInfoCfg.fileName, creatureInfo.name) != null)
            {
                creatureName = creatureInfo.name_language;
                if (creatureName.IsNull()) creatureName = "未命名";
            }
            else
            {
                creatureName = "(未配置名字)";
            }
            listCreatureOptions.Add(new SelectItem(creatureInfo.id, $"{creatureInfo.id}  {creatureName}"));
        }
        listCreatureOptions.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 懒加载随机皮肤(CreatureRandomInfo)候选下拉列表(id + 备注，含"不使用"项，按id排序；只列皮肤池 random_type=0)
    /// </summary>
    private void EnsureRandomSkinOptions()
    {
        if (listRandomSkinOptions != null) return;
        listRandomSkinOptions = new List<SelectItem>();
        listRandomSkinOptions.Add(new SelectItem(0, "0  (不使用随机皮肤)"));
        var allRandomData = CreatureRandomInfoCfg.GetAllArrayData();
        foreach (var randomInfo in allRandomData)
        {
            //只列皮肤池(装备池走随机装备下拉)
            if (randomInfo.GetRandomType() != CreatureRandomTypeEnum.Skin) continue;
            //无备注时回退显示随机池原始数据
            string label = randomInfo.remark.IsNull() ? randomInfo.skin_random_data : randomInfo.remark;
            listRandomSkinOptions.Add(new SelectItem(randomInfo.id, $"{randomInfo.id}  {label}"));
        }
        listRandomSkinOptions.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 懒加载随机装备(CreatureRandomInfo)候选下拉列表(id + 类型前缀 + 备注，含"不使用"项，按id排序；列装备池 random_type=1 与套装池 random_type=2)
    /// </summary>
    private void EnsureRandomEquipOptions()
    {
        if (listRandomEquipOptions != null) return;
        listRandomEquipOptions = new List<SelectItem>();
        listRandomEquipOptions.Add(new SelectItem(0, "0  (不使用随机装备)"));
        var allRandomData = CreatureRandomInfoCfg.GetAllArrayData();
        foreach (var randomInfo in allRandomData)
        {
            //只列装备池与套装池(同一随机装备下拉, 前缀区分)
            var randomType = randomInfo.GetRandomType();
            if (randomType != CreatureRandomTypeEnum.Equip && randomType != CreatureRandomTypeEnum.Suit) continue;
            string typeTag = randomType == CreatureRandomTypeEnum.Suit ? "[套装]" : "[散件]";
            string label = randomInfo.remark.IsNull() ? randomInfo.equip_random_data : randomInfo.remark;
            listRandomEquipOptions.Add(new SelectItem(randomInfo.id, $"{randomInfo.id}  {typeTag}{label}"));
        }
        listRandomEquipOptions.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 是否已启用随机皮肤(creature_random_id != 0)。启用后固定皮肤/发色选项由随机池接管，不再展示
    /// </summary>
    private bool IsRandomSkinEnabled()
    {
        return (creatureData?.creatureNpcData?.npcInfo?.creature_random_id ?? 0) != 0;
    }

    /// <summary>
    /// 切换随机皮肤：改写 npcInfo.creature_random_id 并刷新模型
    /// </summary>
    private void OnChangeRandomSkin(long randomId)
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null || npcInfo.creature_random_id == randomId) return;
        npcInfo.creature_random_id = randomId;
        //皮肤选择面板基于固定皮肤编辑，随机模式下无意义，直接关闭
        CloseSelect();
        RefreshCreature();
    }

    /// <summary>
    /// 切换随机装备池：改写 npcInfo.equip_random 的池ID段(保留稀有度段)并刷新模型
    /// </summary>
    private void OnChangeRandomEquipPool(long poolId)
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null || npcInfo.GetEquipRandomPoolId() == poolId) return;
        if (poolId == 0)
        {
            npcInfo.SetEquipRandom("");
        }
        else
        {
            //保留已选稀有度段, 未选稀有度时默认N
            var rarities = npcInfo.GetEquipRandomRarities();
            string rarityStr = rarities.Count > 0 ? string.Join(",", rarities) : "N";
            npcInfo.SetEquipRandom($"{poolId},{rarityStr}");
        }
        RefreshCreature();
    }

    /// <summary>
    /// 切换随机装备的稀有度(加入/移出稀有度列表; 至少保留1个)并刷新模型
    /// </summary>
    private void OnToggleEquipRarity(RarityEnum rarity)
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null) return;
        long poolId = npcInfo.GetEquipRandomPoolId();
        if (poolId == 0) return;
        var rarities = new List<RarityEnum>(npcInfo.GetEquipRandomRarities());
        if (rarities.Contains(rarity))
        {
            //至少保留1个稀有度
            if (rarities.Count <= 1) return;
            rarities.Remove(rarity);
        }
        else
        {
            rarities.Add(rarity);
            rarities.Sort();
        }
        npcInfo.SetEquipRandom($"{poolId},{string.Join(",", rarities)}");
        RefreshCreature();
    }

    /// <summary>
    /// 按输入框ID加载NPC并初始化
    /// </summary>
    private void OnClickLoadNpc()
    {
        //NPC id 为 long(议会随机议员等大id超过int上限)
        if (!long.TryParse(npcIdInput, out long npcId))
        {
            LogUtil.LogError($"NPC ID 解析失败: {npcIdInput}");
            return;
        }
        NpcInfoBean npcInfoData = NpcInfoCfg.GetItemData(npcId);
        if (npcInfoData == null)
        {
            LogUtil.LogError($"找不到NPC配置: {npcId}");
            return;
        }
        creatureData = new CreatureBean(npcInfoData);
        var creatureNpcData = creatureData.GetCreatureNpcData();
        listCreatureSkinData = creatureNpcData.npcInfo.skin_data.SplitForListLong('&');
        listCreatureEquipItemIds = creatureNpcData.npcInfo.equip_item_ids.SplitForListLong('&');
        dicSkinColorEdit.Clear();
        //读取已保存的皮肤颜色配置(无配置的部位后续由ApplySkinColors把随机色固化进来)
        foreach (var itemColor in creatureNpcData.npcInfo.GetSkinColorData())
            dicSkinColorEdit[itemColor.Key] = itemColor.Value;
        editingColorSkinType = CreatureSkinTypeEnum.None;
        isCreatureDropdownOpen = false;
        isRandomSkinDropdownOpen = false;
        isRandomEquipDropdownOpen = false;
        InitCreatureAttributeUI();
        RefreshCreature();
    }

    /// <summary>
    /// 切换生物(creatureInfo)：改写creatureId；同模组(model_id相同, spine数据一致)的生物保留皮肤/装备/调色/随机池无需重新选配，
    /// 不同模组则清空不适配新模型的皮肤/装备，刷新模型与编辑区
    /// </summary>
    private void OnChangeCreatureInfo(long creatureId)
    {
        if (creatureData == null || creatureData.creatureId == creatureId) return;
        var newCreatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        if (newCreatureInfo == null)
        {
            LogUtil.LogError($"找不到生物配置: {creatureId}");
            return;
        }
        //同模组判定：spine资源/皮肤池/随机池都按 model_id 取，model_id 一致则选配数据全部沿用
        bool isSameModel = creatureData.creatureInfo.model_id == newCreatureInfo.model_id;
        //creatureInfo属性带自校验(creatureId变更自动重解析)；同步改写npcInfo.creature_id保证保存时落盘一致
        creatureData.creatureId = creatureId;
        if (creatureData.creatureNpcData?.npcInfo != null)
            creatureData.creatureNpcData.npcInfo.creature_id = creatureId;
        if (isSameModel)
        {
            //同模组也可能装备槽配置不同(如是否可装备武器)，过滤掉新生物不支持的装备类型
            var listNewEquipType = newCreatureInfo.GetEquipItemsType();
            listCreatureEquipItemIds.RemoveAll(itemId =>
            {
                var itemInfo = ItemsInfoCfg.GetItemData(itemId);
                return itemInfo == null || !listNewEquipType.Contains(itemInfo.GetItemType());
            });
            //皮肤/装备选择面板候选项基于同一模型仍有效，保持打开
        }
        else
        {
            //旧物种的皮肤/装备不适配新模型，清空回退为新物种基础皮肤
            listCreatureSkinData.Clear();
            listCreatureEquipItemIds.Clear();
            dicSkinColorEdit.Clear();
            editingColorSkinType = CreatureSkinTypeEnum.None;
            //随机皮肤池与旧物种模型绑定，切换生物后一并重置(与清空皮肤/装备同语义)
            if (creatureData.creatureNpcData?.npcInfo != null)
            {
                creatureData.creatureNpcData.npcInfo.creature_random_id = 0;
                //随机装备池同样与物种绑定，一并清空
                creatureData.creatureNpcData.npcInfo.SetEquipRandom("");
            }
            isRandomSkinDropdownOpen = false;
            isRandomEquipDropdownOpen = false;
            //皮肤/装备选择面板的候选项基于旧模型已失效，直接关闭
            CloseSelect();
        }
        RefreshCreature();
    }

    /// <summary>
    /// 用NPC数据初始化属性输入框
    /// </summary>
    private void InitCreatureAttributeUI()
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null) return;
        inputHP = $"{npcInfo.HP}";
        inputDR = $"{npcInfo.DR}";
        inputMP = $"{npcInfo.MP}";
        inputATK = $"{npcInfo.ATK}";
        inputASPD = $"{npcInfo.ASPD}";
        inputMSPD = $"{npcInfo.MSPD}";
        inputSearchRange = $"{npcInfo.attack_search_range}";
    }

    /// <summary>
    /// 从属性输入框写回NPC数据
    /// </summary>
    private void UpdateCreatureAttributeFromUI()
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null) return;
        if (float.TryParse(inputHP, out float hp)) npcInfo.HP = hp;
        if (float.TryParse(inputDR, out float dr)) npcInfo.DR = dr;
        if (float.TryParse(inputMP, out float mp)) npcInfo.MP = mp;
        if (float.TryParse(inputATK, out float atk)) npcInfo.ATK = atk;
        if (float.TryParse(inputASPD, out float aspd)) npcInfo.ASPD = aspd;
        if (float.TryParse(inputMSPD, out float mspd)) npcInfo.MSPD = mspd;
        if (float.TryParse(inputSearchRange, out float range)) npcInfo.attack_search_range = range;
    }
    #endregion

    #region 保存
    /// <summary>
    /// 弹窗确认后把当前配置写回 excel_npc_info 并同步重新生成 NpcInfo.txt(仅编辑器)
    /// </summary>
    private void OnClickSave()
    {
        if (creatureData == null)
        {
            LogUtil.LogError("没有生物数据");
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = $"是否要保存生物数据 npcId:{creatureData.creatureNpcData.npcId}";
        dialogData.actionSubmit = (view, data) =>
        {
            string creatureSkinData = "";
            string creatureEquipItemIds = "";
            foreach (var item in listCreatureSkinData) creatureSkinData += $"{item}&";
            foreach (var item in listCreatureEquipItemIds) creatureEquipItemIds += $"{item}&";
#if UNITY_EDITOR
            UpdateCreatureAttributeFromUI();
            var creatureNpcData = creatureData.GetCreatureNpcData();
            long npciD = creatureNpcData.npcId;
            //序列化皮肤颜色(只保留当前固定皮肤中存在的部位, 随机池接管的部位颜色无意义不保存)
            var dicSaveSkinColor = new Dictionary<CreatureSkinTypeEnum, Color>();
            foreach (var itemColor in dicSkinColorEdit)
            {
                for (int i = 0; i < listCreatureSkinData.Count; i++)
                {
                    var skinModelInfo = CreatureModelInfoCfg.GetItemData(listCreatureSkinData[i]);
                    if (skinModelInfo != null && skinModelInfo.GetPartType() == itemColor.Key)
                    {
                        dicSaveSkinColor[itemColor.Key] = itemColor.Value;
                        break;
                    }
                }
            }
            creatureNpcData.npcInfo.SetSkinColorData(dicSaveSkinColor);
            List<ExcelChangeData> listData = new List<ExcelChangeData>()
            {
                new ExcelChangeData(npciD,"creature_id",$"{creatureNpcData.npcInfo.creature_id}"),
                new ExcelChangeData(npciD,"creature_random_id",$"{creatureNpcData.npcInfo.creature_random_id}"),
                new ExcelChangeData(npciD,"skin_data",creatureSkinData),
                new ExcelChangeData(npciD,"skin_color_data",$"{creatureNpcData.npcInfo.skin_color_data}"),
                new ExcelChangeData(npciD,"equip_item_ids",creatureEquipItemIds),
                new ExcelChangeData(npciD,"equip_random",$"{creatureNpcData.npcInfo.equip_random}"),
                new ExcelChangeData(npciD,"HP",$"{creatureNpcData.npcInfo.HP}"),
                new ExcelChangeData(npciD,"DR",$"{creatureNpcData.npcInfo.DR}"),
                new ExcelChangeData(npciD,"MP",$"{creatureNpcData.npcInfo.MP}"),
                new ExcelChangeData(npciD,"ATK",$"{creatureNpcData.npcInfo.ATK}"),
                new ExcelChangeData(npciD,"ASPD",$"{creatureNpcData.npcInfo.ASPD}"),
                new ExcelChangeData(npciD,"MSPD",$"{creatureNpcData.npcInfo.MSPD}"),
                new ExcelChangeData(npciD,"attack_search_range",$"{creatureNpcData.npcInfo.attack_search_range}"),
            };
            ExcelUtil.SetExcelData("Assets/Data/Excel/excel_npc_info[NPC信息].xlsx", "NpcInfo", listData);
            //同步重新生成运行时JSON(NpcInfo.txt)，避免下次启动读到旧数据
            ExcelUtil.ExcelToJsonItem("Assets/Data/Excel/excel_npc_info[NPC信息].xlsx");
#endif
        };
        dialogData.actionCancel = (view, data) => { };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 关闭GUI并销毁自身
    /// </summary>
    private void CloseSelf()
    {
        Destroy(gameObject);
    }
    #endregion

    #region GUI绘制
    /// <summary>
    /// IMGUI入口，绘制纯代码创建的NPC创建面板
    /// </summary>
    private void OnGUI()
    {
        InitGUIStyle();

        GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20), GUI.skin.box);
        scrollMain = GUILayout.BeginScrollView(scrollMain);

        GUILayout.Label("NPC 创建（GUI版）", titleStyle);
        GUILayout.Space(4);

        //顶部：打开NPC配置表
        if (GUILayout.Button("📂 打开 NPC 表", GUILayout.Height(26)))
            OpenNpcInfoExcel();
        GUILayout.Space(4);

        //加载行：NPC下拉选择 + 开始创建
        EnsureNpcOptions();
        GUILayout.BeginHorizontal();
        GUILayout.Label("NPC", labelStyle, GUILayout.Width(40));
        if (GUILayout.Button(npcDropdownLabel, GUILayout.Height(26)))
            isNpcDropdownOpen = !isNpcDropdownOpen;
        if (GUILayout.Button("开始创建", GUILayout.Width(90), GUILayout.Height(26)))
            OnClickLoadNpc();
        GUILayout.EndHorizontal();

        //下拉展开时的候选列表(列出 id + 名字，当前选中项高亮)
        if (isNpcDropdownOpen)
        {
            scrollNpcDropdown = GUILayout.BeginScrollView(scrollNpcDropdown, GUI.skin.box, GUILayout.Height(220));
            foreach (var option in listNpcOptions)
            {
                bool isCurrent = $"{option.id}" == npcIdInput;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    npcIdInput = $"{option.id}";
                    npcDropdownLabel = option.label;
                    isNpcDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }

        if (creatureData != null)
        {
            GUILayout.Space(6);
            if (GUILayout.Button(isShowEquip ? "装备展示：显示中(点击隐藏)" : "装备展示：隐藏中(点击显示)"))
            {
                isShowEquip = !isShowEquip;
                RefreshCreature();
            }

            //预览大小手动调节(默认5倍; SetCreatureData每次按配置重置缩放, 刷新后由ApplyPreviewScale统一覆盖)
            GUILayout.BeginHorizontal();
            GUILayout.Label("预览大小", labelStyle, GUILayout.Width(70));
            float newPreviewScale = GUILayout.HorizontalSlider(previewScale, 0.5f, 10f);
            GUILayout.Label($"{previewScale:F1}x", labelStyle, GUILayout.Width(45));
            if (!Mathf.Approximately(newPreviewScale, previewScale))
            {
                previewScale = newPreviewScale;
                ApplyPreviewScale();
            }
            GUILayout.EndHorizontal();

            DrawCreatureInfoSection();
            DrawAttributeSection();
            DrawSkinColorSection();
            DrawBodySection();
            DrawEquipSection();
            DrawCardDataSection();

            GUILayout.Space(6);
            if (GUILayout.Button("保存到 Excel", GUILayout.Height(30)))
                OnClickSave();
        }

        GUILayout.Space(6);
        if (GUILayout.Button("关闭", GUILayout.Height(26)))
            CloseSelf();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        //右侧选择面板
        if (selectMode != SelectMode.None)
            DrawSelectPanel();
    }

    /// <summary>
    /// 生物(creatureInfo)切换区：下拉选择新物种(id+名字)，切换后皮肤/装备重置为新物种基础配置
    /// </summary>
    private void DrawCreatureInfoSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 生物(CreatureInfo) ──", sectionStyle);
        EnsureCreatureOptions();
        //当前选中项label从候选列表实时取(自带多语言缺失防护)，随creatureId变化自动更新
        var currentOption = listCreatureOptions.Find(item => item.id == creatureData.creatureId);
        string creatureDropdownLabel = currentOption.id != 0 ? currentOption.label : $"{creatureData.creatureId}  (未知生物)";
        GUILayout.BeginHorizontal();
        GUILayout.Label("生物", labelStyle, GUILayout.Width(40));
        if (GUILayout.Button(creatureDropdownLabel, GUILayout.Height(26)))
            isCreatureDropdownOpen = !isCreatureDropdownOpen;
        GUILayout.EndHorizontal();

        //下拉展开时的候选列表(列出 id + 名字，当前选中项高亮)
        if (isCreatureDropdownOpen)
        {
            scrollCreatureDropdown = GUILayout.BeginScrollView(scrollCreatureDropdown, GUI.skin.box, GUILayout.Height(220));
            foreach (var option in listCreatureOptions)
            {
                bool isCurrent = option.id == creatureData.creatureId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    isCreatureDropdownOpen = false;
                    OnChangeCreatureInfo(option.id);
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 属性编辑区
    /// </summary>
    private void DrawAttributeSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 属性 ──", sectionStyle);
        inputHP = DrawAttrField("HP", inputHP);
        inputDR = DrawAttrField("DR", inputDR);
        inputMP = DrawAttrField("MP", inputMP);
        inputATK = DrawAttrField("ATK", inputATK);
        inputASPD = DrawAttrField("ASPD", inputASPD);
        inputMSPD = DrawAttrField("MSPD", inputMSPD);
        inputSearchRange = DrawAttrField("搜索范围", inputSearchRange);
        if (GUILayout.Button("应用属性"))
            RefreshCreature();
    }

    /// <summary>
    /// 单行属性输入
    /// </summary>
    private string DrawAttrField(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(80));
        value = GUILayout.TextField(value ?? "", GUILayout.Height(24));
        GUILayout.EndHorizontal();
        return value;
    }

    /// <summary>
    /// 皮肤颜色调节区：列出所有已装备且支持调色(color_state!=0)的皮肤部位，点击色块展开/收起该部位的RGB滑条+调色盘；
    /// 选中可调色皮肤时会自动展开对应部位；已启用随机皮肤时颜色由随机池决定，只显示提示
    /// </summary>
    private void DrawSkinColorSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 皮肤颜色 ──", sectionStyle);
        //随机皮肤模式下皮肤来自随机池且颜色随机，手动调色无意义
        if (IsRandomSkinEnabled())
        {
            GUILayout.Label("(已启用随机皮肤，颜色由随机池决定)", labelStyle);
            return;
        }
        //先快照可调色部位：调色操作会触发RefreshCreature重建dicSkinData，不能边遍历字典边刷新
        var listColorableSkin = new List<(CreatureSkinTypeEnum skinType, int colorState)>();
        foreach (var kv in creatureData.dicSkinData)
        {
            var skinModelInfo = CreatureModelInfoCfg.GetItemData(kv.Value.skinId);
            if (skinModelInfo == null || skinModelInfo.color_state == 0) continue;
            listColorableSkin.Add((kv.Key, skinModelInfo.color_state));
        }
        foreach (var colorableSkin in listColorableSkin)
        {
            CreatureSkinTypeEnum skinType = colorableSkin.skinType;
            Color skinColor = GetSkinColor(skinType);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{skinType.GetEnumName()}", labelStyle, GUILayout.Width(90));
            //色块按钮：展示当前颜色，点击展开/收起该部位的调色编辑
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = skinColor;
            if (GUILayout.Button(editingColorSkinType == skinType ? "收起" : "", GUILayout.Width(60), GUILayout.Height(22)))
            {
                GUI.backgroundColor = oldBgColor;
                editingColorSkinType = editingColorSkinType == skinType ? CreatureSkinTypeEnum.None : skinType;
            }
            GUI.backgroundColor = oldBgColor;
            GUILayout.EndHorizontal();
            if (editingColorSkinType == skinType)
                DrawSkinColorEditor(skinType, colorableSkin.colorState == 2);
        }
        if (listColorableSkin.Count == 0)
            GUILayout.Label("(当前皮肤均不支持调色)", labelStyle);
    }

    /// <summary>
    /// 单个部位的颜色编辑器：RGB滑条实时应用，color_state==2(可设置透明颜色)时追加A滑条，下方附调色盘
    /// </summary>
    private void DrawSkinColorEditor(CreatureSkinTypeEnum skinType, bool isSupportAlpha)
    {
        Color skinColor = GetSkinColor(skinType);
        Color newSkinColor = skinColor;
        newSkinColor.r = DrawColorSlider("R", newSkinColor.r);
        newSkinColor.g = DrawColorSlider("G", newSkinColor.g);
        newSkinColor.b = DrawColorSlider("B", newSkinColor.b);
        if (isSupportAlpha)
            newSkinColor.a = DrawColorSlider("A", newSkinColor.a);
        if (newSkinColor != skinColor)
        {
            dicSkinColorEdit[skinType] = newSkinColor;
            RefreshCreature();
        }
        DrawSkinColorPalette(skinType, skinColor);
    }

    /// <summary>
    /// 取某部位当前的调色(无记录时默认白色=不染色)
    /// </summary>
    private Color GetSkinColor(CreatureSkinTypeEnum skinType)
    {
        return dicSkinColorEdit.TryGetValue(skinType, out var color) ? color : Color.white;
    }

    /// <summary>
    /// 皮肤调色盘区：预设颜色块点选即应用，当前颜色所在色块显示✔
    /// </summary>
    private void DrawSkinColorPalette(CreatureSkinTypeEnum skinType, Color skinColor)
    {
        GUILayout.Space(2);
        GUILayout.Label("调色盘(点选即应用):", labelStyle);
        const int paletteColumns = 8;
        for (int i = 0; i < paletteSkinColors.Length; i++)
        {
            if (i % paletteColumns == 0)
                GUILayout.BeginHorizontal();
            Color paletteColor = paletteSkinColors[i];
            bool isCurrentColor = IsApproximatelyColor(skinColor, paletteColor);
            //用backgroundColor给空按钮着色成色块
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = paletteColor;
            if (GUILayout.Button(isCurrentColor ? "✔" : "", GUILayout.Width(34), GUILayout.Height(28)))
            {
                GUI.backgroundColor = oldBgColor;
                dicSkinColorEdit[skinType] = paletteColor;
                RefreshCreature();
            }
            GUI.backgroundColor = oldBgColor;
            if (i % paletteColumns == paletteColumns - 1 || i == paletteSkinColors.Length - 1)
                GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 判断两个颜色RGB是否近似相等(用于调色盘当前色高亮)
    /// </summary>
    private bool IsApproximatelyColor(Color colorA, Color colorB)
    {
        return Mathf.Approximately(colorA.r, colorB.r)
            && Mathf.Approximately(colorA.g, colorB.g)
            && Mathf.Approximately(colorA.b, colorB.b);
    }

    /// <summary>
    /// 单条颜色滑条
    /// </summary>
    private float DrawColorSlider(string label, float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(30));
        value = GUILayout.HorizontalSlider(value, 0f, 1f);
        GUILayout.Label($"{value:F2}", labelStyle, GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return value;
    }

    /// <summary>
    /// 身体皮肤部件区(装备驱动部位不列出：帽子/衣服/裤子/鼻环/武器等由装备换皮，走装备区，数据驱动判定见 ItemsInfoCfg.GetEquipDrivenSkinPartTypes；
    /// 顶部为随机皮肤下拉，启用随机皮肤后固定皮肤选项不再展示)
    /// </summary>
    private void DrawBodySection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 身体皮肤 ──", sectionStyle);
        DrawRandomSkinDropdown();
        //启用随机皮肤后固定皮肤由随机池接管，相关选项不再展示
        if (IsRandomSkinEnabled())
        {
            GUILayout.Label("(已启用随机皮肤，固定皮肤选项隐藏)", labelStyle);
            return;
        }
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureData.creatureInfo.model_id);
        //装备驱动部位(帽子/衣服/裤子/鼻环/武器等)的换皮由装备决定，不提供手动皮肤选择
        var setEquipDrivenPart = ItemsInfoCfg.GetEquipDrivenSkinPartTypes(creatureData.creatureInfo.model_id);
        foreach (var kv in dicAllSkins)
        {
            CreatureSkinTypeEnum skinType = kv.Key;
            if (setEquipDrivenPart.Contains(skinType)) continue;
            //武器皮肤(>=90)由装备的武器皮肤决定，不提供手动选择(装备驱动判定未覆盖时的兜底)
            if ((int)skinType >= 90) continue;
            long currentId = GetCurrentSkinId(skinType);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{skinType.GetEnumName()}: {currentId}", labelStyle);
            if (GUILayout.Button("选择", GUILayout.Width(70), GUILayout.Height(22)))
                OpenSkinSelect(skinType);
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 随机皮肤下拉区：选择 CreatureRandomInfo 随机池(id+备注)，0=不使用
    /// </summary>
    private void DrawRandomSkinDropdown()
    {
        EnsureRandomSkinOptions();
        long currentRandomId = creatureData.creatureNpcData?.npcInfo?.creature_random_id ?? 0;
        //当前选中项label从候选列表实时取，随creature_random_id变化自动更新
        var currentOption = listRandomSkinOptions.Find(item => item.id == currentRandomId);
        string randomDropdownLabel = currentOption.label ?? $"{currentRandomId}  (未知随机池)";
        GUILayout.BeginHorizontal();
        GUILayout.Label("随机皮肤", labelStyle, GUILayout.Width(70));
        if (GUILayout.Button(randomDropdownLabel, GUILayout.Height(26)))
            isRandomSkinDropdownOpen = !isRandomSkinDropdownOpen;
        GUILayout.EndHorizontal();

        //下拉展开时的候选列表(列出 id + 备注，当前选中项高亮)
        if (isRandomSkinDropdownOpen)
        {
            scrollRandomSkinDropdown = GUILayout.BeginScrollView(scrollRandomSkinDropdown, GUI.skin.box, GUILayout.Height(160));
            foreach (var option in listRandomSkinOptions)
            {
                bool isCurrent = option.id == currentRandomId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    isRandomSkinDropdownOpen = false;
                    OnChangeRandomSkin(option.id);
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 装备槽区(顶部为随机装备下拉+稀有度开关；配置随机装备后每次刷新从池中重抽填充空槽)
    /// </summary>
    private void DrawEquipSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 装备 ──", sectionStyle);
        DrawRandomEquipDropdown();
        DrawEquipRarityToggles();
        List<ItemTypeEnum> listEquipType = creatureData.creatureInfo.GetEquipItemsType();
        foreach (var equipType in listEquipType)
        {
            long currentId = GetCurrentEquipId(equipType);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{equipType.GetEnumName()}: {currentId}", labelStyle);
            if (GUILayout.Button("选择", GUILayout.Width(70), GUILayout.Height(22)))
                OpenEquipSelect(equipType);
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 随机装备下拉区：选择 CreatureRandomInfo 装备池(id+备注)，0=不使用
    /// </summary>
    private void DrawRandomEquipDropdown()
    {
        EnsureRandomEquipOptions();
        long currentPoolId = creatureData.creatureNpcData?.npcInfo?.GetEquipRandomPoolId() ?? 0;
        //当前选中项label从候选列表实时取，随equip_random变化自动更新
        var currentOption = listRandomEquipOptions.Find(item => item.id == currentPoolId);
        string randomEquipLabel = currentOption.label ?? $"{currentPoolId}  (未知装备池)";
        GUILayout.BeginHorizontal();
        GUILayout.Label("随机装备", labelStyle, GUILayout.Width(70));
        if (GUILayout.Button(randomEquipLabel, GUILayout.Height(26)))
            isRandomEquipDropdownOpen = !isRandomEquipDropdownOpen;
        GUILayout.EndHorizontal();

        //下拉展开时的候选列表(列出 id + 备注，当前选中项高亮)
        if (isRandomEquipDropdownOpen)
        {
            scrollRandomEquipDropdown = GUILayout.BeginScrollView(scrollRandomEquipDropdown, GUI.skin.box, GUILayout.Height(160));
            foreach (var option in listRandomEquipOptions)
            {
                bool isCurrent = option.id == currentPoolId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    isRandomEquipDropdownOpen = false;
                    OnChangeRandomEquipPool(option.id);
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 随机装备稀有度开关行：点选加入/移出稀有度列表(至少保留1个)，选中项绿色高亮
    /// </summary>
    private void DrawEquipRarityToggles()
    {
        var npcInfo = creatureData.creatureNpcData?.npcInfo;
        if (npcInfo == null || npcInfo.GetEquipRandomPoolId() == 0) return;
        var rarities = npcInfo.GetEquipRandomRarities();
        GUILayout.BeginHorizontal();
        GUILayout.Label("稀有度", labelStyle, GUILayout.Width(70));
        foreach (RarityEnum rarity in System.Enum.GetValues(typeof(RarityEnum)))
        {
            bool isSelected = rarities.Contains(rarity);
            Color oldColor = GUI.color;
            if (isSelected) GUI.color = Color.green;
            if (GUILayout.Button(rarity.ToString(), GUILayout.Width(48), GUILayout.Height(22)))
            {
                GUI.color = oldColor;
                OnToggleEquipRarity(rarity);
                break;
            }
            GUI.color = oldColor;
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 卡片数据区(纯文本列出，不还原卡面美术)
    /// </summary>
    private void DrawCardDataSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 卡片数据 ──", sectionStyle);
        GUILayout.Label($"名字: {creatureData.creatureName}", labelStyle);
        GUILayout.Label($"等级: {creatureData.level}   稀有度: {creatureData.rarity}", labelStyle);
        GUILayout.Label($"HP:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.HP)}  " +
                        $"MP:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.MP)}  " +
                        $"DR:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.DR)}", labelStyle);
        GUILayout.Label($"ATK:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.ATK)}  " +
                        $"ASPD:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.ASPD)}  " +
                        $"MSPD:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.MSPD)}", labelStyle);
        GUILayout.Label($"皮肤: {string.Join(",", listCreatureSkinData)}", labelStyle);
        GUILayout.Label($"装备: {string.Join(",", listCreatureEquipItemIds)}", labelStyle);
        GUILayout.Label($"随机装备: {creatureData.creatureNpcData?.npcInfo?.equip_random}", labelStyle);
    }

    /// <summary>
    /// 右侧候选选择面板
    /// </summary>
    private void DrawSelectPanel()
    {
        GUILayout.BeginArea(new Rect(420, 10, 320, Screen.height - 20), GUI.skin.box);
        GUILayout.Label(selectMode == SelectMode.Skin ? "选择皮肤" : "选择装备", titleStyle);
        if (GUILayout.Button("关闭选择"))
            CloseSelect();
        scrollSelect = GUILayout.BeginScrollView(scrollSelect);
        //当前该部位/该槽位已选中的id，用于列表内高亮
        long currentId = selectMode == SelectMode.Skin
            ? GetCurrentSkinId((CreatureSkinTypeEnum)selectShowType)
            : GetCurrentEquipId((ItemTypeEnum)selectShowType);
        foreach (var item in listSelectItem)
        {
            //选中项前加勾并变色，方便随时对照切换
            bool isCurrent = item.id == currentId;
            Color oldColor = GUI.color;
            if (isCurrent) GUI.color = Color.green;
            string label = isCurrent ? $"✔ {item.label}" : item.label;
            GUILayout.BeginHorizontal();
            //有图标的选项先画图标预览(异步加载完成前显示空白占位)；无图标项留空位对齐文字
            if (!item.iconName.IsNull())
                DrawSpriteIcon(GetIconCached(item.atlasType, item.iconName), 26);
            else
                GUILayout.Space(30);
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                GUI.color = oldColor;
                GUILayout.EndHorizontal();
                if (selectMode == SelectMode.Skin) OnSelectSkin(item.id);
                else OnSelectEquip(item.id);
                break;
            }
            GUI.color = oldColor;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    #endregion

    #region 选择逻辑
    /// <summary>
    /// 取某皮肤部位当前选中的皮肤id
    /// </summary>
    private long GetCurrentSkinId(CreatureSkinTypeEnum skinType)
    {
        foreach (var id in listCreatureSkinData)
        {
            var info = CreatureModelInfoCfg.GetItemData(id);
            if (info != null && info.GetPartType() == skinType)
                return id;
        }
        return 0;
    }

    /// <summary>
    /// 取某装备类型当前选中的装备id
    /// </summary>
    private long GetCurrentEquipId(ItemTypeEnum itemType)
    {
        foreach (var id in listCreatureEquipItemIds)
        {
            var info = ItemsInfoCfg.GetItemData(id);
            if (info != null && info.GetItemType() == itemType)
                return id;
        }
        return 0;
    }

    /// <summary>
    /// 打开皮肤候选列表
    /// </summary>
    private void OpenSkinSelect(CreatureSkinTypeEnum skinType)
    {
        selectMode = SelectMode.Skin;
        selectShowType = (int)skinType;
        listSelectItem.Clear();
        listSelectItem.Add(new SelectItem(0, "取消(不放置)"));
        var creatureModelData = CreatureModelCfg.GetItemData(creatureData.creatureInfo.model_id);
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureData.creatureInfo.model_id);
        //穿戴类皮肤(帽子/衣服/裤子等)的贴图不在 Skins 图集，而是作为装备图标打进 Items 图集：
        //按 ItemsInfo.creature_model_info_id 反查装备，改用装备的 icon_res 加载(与 IconHandler.SetItemIcon 同逻辑)
        var dicSkinItemIconRes = new Dictionary<long, string>();
        var listItemInfo = ItemsInfoCfg.GetDataByCreatureModelId(creatureModelData.id);
        if (listItemInfo != null)
        {
            foreach (var itemInfo in listItemInfo)
            {
                if (itemInfo.creature_model_info_id != 0 && !itemInfo.icon_res.IsNull())
                    dicSkinItemIconRes[itemInfo.creature_model_info_id] = itemInfo.icon_res;
            }
        }
        if (dicAllSkins.TryGetValue(skinType, out var listSkinData))
        {
            foreach (var info in listSkinData)
            {
                SpriteAtlasTypeEnum atlasType;
                string iconName;
                if (dicSkinItemIconRes.TryGetValue(info.id, out string itemIconRes))
                {
                    //装备驱动皮肤：走装备图标逻辑(默认 Items 图集, 支持 ",图集类型" 后缀)
                    ParseIconRes(itemIconRes, SpriteAtlasTypeEnum.Items, out atlasType, out iconName);
                }
                else
                {
                    //普通皮肤图标名由表记录拼接: {CreatureModel.mark_name}_Atlas_{CreatureModelInfo.res_name(/转_)}
                    //(与生成器 SpineWindow.ExtractAndSaveTextures 的 {spineAtlasAsset.name}_{skin.Name} 命名对应)
                    atlasType = SpriteAtlasTypeEnum.Skins;
                    iconName = $"{creatureModelData.mark_name}_Atlas_{info.res_name.Replace("/", "_")}";
                }
                listSelectItem.Add(new SelectItem(info.id, $"{info.id}  {info.res_name}", atlasType, iconName));
            }
        }
        scrollSelect = Vector2.zero;
    }

    /// <summary>
    /// 打开装备候选列表
    /// </summary>
    private void OpenEquipSelect(ItemTypeEnum itemType)
    {
        selectMode = SelectMode.Equip;
        selectShowType = (int)itemType;
        listSelectItem.Clear();
        listSelectItem.Add(new SelectItem(0, "取消(不放置)"));
        var creatureModelData = CreatureModelCfg.GetItemData(creatureData.creatureInfo.model_id);
        var listItemInfo = ItemsInfoCfg.GetDataByCreatureModelId(creatureModelData.id);
        foreach (var itemInfo in listItemInfo)
        {
            if (itemInfo.GetItemType() == itemType)
            {
                //装备图标: icon_res 支持 ",图集类型" 后缀，默认 Items 图集
                ParseIconRes(itemInfo.icon_res, SpriteAtlasTypeEnum.Items, out SpriteAtlasTypeEnum atlasType, out string iconName);
                listSelectItem.Add(new SelectItem(itemInfo.id, $"{itemInfo.id}  {GetItemShowName(itemInfo)}", atlasType, iconName));
            }
        }
        scrollSelect = Vector2.zero;
    }

    /// <summary>
    /// 取装备显示名(带多语言缺失防护，缺配置时不刷 LogError)
    /// </summary>
    private string GetItemShowName(ItemsInfoBean itemInfo)
    {
        if (itemInfo.name != 0 && LanguageCfg.GetItemData(ItemsInfoCfg.fileName, itemInfo.name) != null)
        {
            string itemName = itemInfo.name_language;
            return itemName.IsNull() ? "未命名" : itemName;
        }
        return "(未配置名字)";
    }

    /// <summary>
    /// 选中皮肤：替换同部位皮肤后刷新(不关闭选择面板，方便随时切换)；新皮肤支持调色(color_state!=0)时自动展开该部位的颜色选择
    /// </summary>
    private void OnSelectSkin(long showId)
    {
        var skinType = (CreatureSkinTypeEnum)selectShowType;
        //移除同部位旧皮肤
        for (int i = 0; i < listCreatureSkinData.Count; i++)
        {
            var info = CreatureModelInfoCfg.GetItemData(listCreatureSkinData[i]);
            if (info != null && info.GetPartType() == skinType)
            {
                listCreatureSkinData.RemoveAt(i);
                i--;
            }
        }
        if (showId != 0)
        {
            listCreatureSkinData.Add(showId);
            //支持调色的皮肤选中后自动打开颜色选择；同部位换皮肤时保留已调颜色(与 UIMainCreate 同规则)
            var skinModelInfo = CreatureModelInfoCfg.GetItemData(showId);
            if (skinModelInfo != null && skinModelInfo.color_state != 0)
                editingColorSkinType = skinType;
        }
        else if (editingColorSkinType == skinType)
        {
            //取消放置的部位若正在调色，收起编辑器
            editingColorSkinType = CreatureSkinTypeEnum.None;
        }
        RefreshCreature();
    }

    /// <summary>
    /// 选中装备：替换同类型装备后刷新(不关闭选择面板，方便随时切换)
    /// </summary>
    private void OnSelectEquip(long showId)
    {
        var itemType = (ItemTypeEnum)selectShowType;
        //移除同类型旧装备
        for (int i = 0; i < listCreatureEquipItemIds.Count; i++)
        {
            var info = ItemsInfoCfg.GetItemData(listCreatureEquipItemIds[i]);
            if (info != null && info.GetItemType() == itemType)
            {
                listCreatureEquipItemIds.RemoveAt(i);
                i--;
            }
        }
        if (showId != 0)
            listCreatureEquipItemIds.Add(showId);
        RefreshCreature();
    }

    /// <summary>
    /// 关闭选择面板
    /// </summary>
    private void CloseSelect()
    {
        selectMode = SelectMode.None;
        listSelectItem.Clear();
    }
    #endregion

    #region GUI样式
    /// <summary>
    /// 懒初始化GUI样式，只初始化一次
    /// </summary>
    private void InitGUIStyle()
    {
        if (guiStyleInited) return;
        guiStyleInited = true;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
        sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
    }
    #endregion
}
