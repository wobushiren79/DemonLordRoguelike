using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// NpcCreateEditorWindow 中栏下区（partial）：外观编辑全套。
/// 差异点：
/// 1) 编辑目标从运行时 creatureData 改为编辑副本 editingNpcInfo，所有变更即时写回（皮肤列表/装备列表/随机池/调色），由主类快照比对统一判脏；
/// 2) 图标加载从 IconHandler（Addressables 运行时单例）改为 AssetDatabase 直读 .spriteatlas；
/// 3) 模型刷新从 RefreshCreature 改为 RefreshPreview（Preview partial 重建编辑安全的 CreatureBean 并应用到预览骨架）；
/// 4) 调色字典的随机色固化只发生在预览装配时（防抖动），不写回编辑副本——只有用户手动调色才写回（保存时再按固定皮肤过滤序列化，固定规则）。
/// </summary>
public partial class NpcCreateEditorWindow : EditorWindow
{
    #region 外观编辑状态
    /// <summary>当前固定皮肤ID列表（与 editingNpcInfo.skin_data 同步）</summary>
    private List<long> listCreatureSkinData = new List<long>();
    /// <summary>当前固定装备ID列表（与 editingNpcInfo.equip_item_ids 同步）</summary>
    private List<long> listCreatureEquipItemIds = new List<long>();
    /// <summary>各部位手动调色（仅 color_state!=0 的皮肤；随机色固化也存这里防抖动）</summary>
    private readonly Dictionary<CreatureSkinTypeEnum, Color> dicSkinColorEdit = new Dictionary<CreatureSkinTypeEnum, Color>();
    /// <summary>当前展开调色编辑的部位（None=全部收起）</summary>
    private CreatureSkinTypeEnum editingColorSkinType = CreatureSkinTypeEnum.None;
    /// <summary>外观编辑区滚动位置</summary>
    private Vector2 scrollAppearance;

    /// <summary>皮肤调色盘预设色（发色/唇色等常用色，点选即应用）</summary>
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
    #endregion

    #region 选择面板状态
    /// <summary>选择面板模式</summary>
    private enum SelectMode { None, Skin, Equip }
    /// <summary>当前选择模式</summary>
    private SelectMode selectMode = SelectMode.None;
    /// <summary>当前选择的皮肤部位/装备类型</summary>
    private int selectShowType;
    /// <summary>选择面板候选项</summary>
    private readonly List<SelectItem> listSelectItem = new List<SelectItem>();
    /// <summary>选择面板滚动位置</summary>
    private Vector2 scrollSelect;

    /// <summary>选择面板候选项（id + 显示名 + 可选图标）</summary>
    private struct SelectItem
    {
        public long id;
        public string label;
        public SpriteAtlasTypeEnum atlasType;
        public string iconName;
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

    #region 图标加载（编辑器版：AssetDatabase 直读图集）
    /// <summary>图标缓存（key=图集/图标名，null值表示图集无此图）</summary>
    private readonly Dictionary<string, Sprite> dicIconCache = new Dictionary<string, Sprite>();
    /// <summary>图集缓存（key=图集路径，null值表示图集缺失）</summary>
    private readonly Dictionary<string, SpriteAtlas> dicAtlasCache = new Dictionary<string, SpriteAtlas>();

    /// <summary>
    /// 取图标（带缓存）：编辑器不能用 IconHandler（Addressables 运行时单例），改按路径约定直读图集后 GetSprite；图集缺失/缺图缓存 null 防反复加载
    /// </summary>
    private Sprite GetIconCached(SpriteAtlasTypeEnum atlasType, string iconName)
    {
        if (iconName.IsNull())
            return null;
        string cacheKey = $"{atlasType}/{iconName}";
        if (dicIconCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            return cachedSprite;
        string atlasPath = $"{IconManager.PathSpriteAtlas}/AtlasFor{atlasType.GetEnumName()}.spriteatlas";
        if (!dicAtlasCache.TryGetValue(atlasPath, out SpriteAtlas atlas))
        {
            atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            dicAtlasCache[atlasPath] = atlas;
        }
        Sprite sprite = atlas != null ? atlas.GetSprite(iconName) : null;
        dicIconCache[cacheKey] = sprite;
        return sprite;
    }

    /// <summary>
    /// 在GUILayout流中绘制图集sprite的子图区域（缺图时只占位空白）
    /// </summary>
    private void DrawSpriteIcon(Sprite sprite, float iconSize)
    {
        Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
        if (sprite == null || Event.current.type != EventType.Repaint)
            return;
        //sprite来自图集，需用textureRect换算UV只绘制子图区域
        Rect texRect = sprite.textureRect;
        Rect texCoords = new Rect(
            texRect.x / sprite.texture.width, texRect.y / sprite.texture.height,
            texRect.width / sprite.texture.width, texRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(iconRect, sprite.texture, texCoords);
    }

    /// <summary>
    /// 解析图标资源名中的「,图集类型」后缀（规则同 IconHandler.ParseIconName），无后缀用默认图集
    /// </summary>
    private void ParseIconRes(string iconRes, SpriteAtlasTypeEnum defaultType, out SpriteAtlasTypeEnum atlasType, out string actualIconName)
    {
        atlasType = defaultType;
        actualIconName = iconRes;
        if (iconRes.IsNull())
            return;
        int commaIndex = iconRes.LastIndexOf(',');
        if (commaIndex <= 0 || commaIndex >= iconRes.Length - 1)
            return;
        if (System.Enum.TryParse<SpriteAtlasTypeEnum>(iconRes.Substring(commaIndex + 1), out var parsedType))
        {
            atlasType = parsedType;
            actualIconName = iconRes.Substring(0, commaIndex);
        }
    }
    #endregion

    #region 外观状态初始化与写回
    /// <summary>
    /// 从编辑副本初始化外观编辑状态（加载NPC/新建/切换时调用）
    /// </summary>
    private void InitAppearanceStateFromEditing()
    {
        listCreatureSkinData = editingNpcInfo.skin_data.SplitForListLong('&');
        listCreatureEquipItemIds = editingNpcInfo.equip_item_ids.SplitForListLong('&');
        dicSkinColorEdit.Clear();
        foreach (var itemColor in editingNpcInfo.GetSkinColorData())
            dicSkinColorEdit[itemColor.Key] = itemColor.Value;
        editingColorSkinType = CreatureSkinTypeEnum.None;
    }

    /// <summary>
    /// 皮肤列表写回编辑副本（& 分隔，无尾缀）
    /// </summary>
    private void WriteBackSkinData()
    {
        editingNpcInfo.skin_data = string.Join("&", listCreatureSkinData);
    }

    /// <summary>
    /// 装备列表写回编辑副本（& 分隔，无尾缀）
    /// </summary>
    private void WriteBackEquipItemIds()
    {
        editingNpcInfo.equip_item_ids = string.Join("&", listCreatureEquipItemIds);
    }

    /// <summary>
    /// 调色写回编辑副本（只保留当前固定皮肤中存在的部位，随机池接管的部位颜色无意义不保存——既定过滤规则）
    /// </summary>
    private void WriteBackSkinColorData()
    {
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
        editingNpcInfo.SetSkinColorData(dicSaveSkinColor);
    }

    /// <summary>
    /// 是否已启用随机皮肤（creature_random_id != 0；启用后固定皮肤/调色选项由随机池接管，不再展示）
    /// </summary>
    private bool IsRandomSkinEnabled()
    {
        return editingNpcInfo != null && editingNpcInfo.creature_random_id != 0;
    }
    #endregion

    #region 外观区绘制
    /// <summary>
    /// 绘制外观编辑区（装备展示开关 + 随机皮肤 + 皮肤颜色 + 身体皮肤 + 装备）
    /// </summary>
    private void DrawAppearanceSection()
    {
        DrawSectionHeader("外观");
        if (editingNpcInfo.creature_id == 0)
        {
            EditorGUILayout.HelpBox("未选择生物（creature_id=0）：无实体NPC仅配置头像即可，无需外观编辑。", MessageType.Info);
            return;
        }
        var creatureInfo = CreatureInfoCfg.GetItemData(editingNpcInfo.creature_id);
        if (creatureInfo == null)
        {
            EditorGUILayout.HelpBox($"找不到生物配置: {editingNpcInfo.creature_id}", MessageType.Error);
            return;
        }
        //装备展示开关（影响预览，不入配置）
        if (GUILayout.Button(isShowEquip ? "装备展示：显示中(点击隐藏)" : "装备展示：隐藏中(点击显示)"))
        {
            isShowEquip = !isShowEquip;
            RefreshPreview();
        }
        DrawRandomSkinPopup();
        DrawSkinColorSection();
        DrawBodySection(creatureInfo);
        DrawEquipSection(creatureInfo);
    }

    /// <summary>
    /// 身体皮肤部件区（装备驱动部位与武器位不列出；启用随机皮肤后隐藏固定皮肤选项）
    /// </summary>
    private void DrawBodySection(CreatureInfoBean creatureInfo)
    {
        DrawSectionHeader("身体皮肤");
        if (IsRandomSkinEnabled())
        {
            EditorGUILayout.LabelField("(已启用随机皮肤，固定皮肤选项隐藏)", EditorStyles.miniLabel);
            return;
        }
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureInfo.model_id);
        if (dicAllSkins == null)
        {
            EditorGUILayout.LabelField($"(模型 {creatureInfo.model_id} 无皮肤数据)", EditorStyles.miniLabel);
            return;
        }
        //装备驱动部位(帽子/衣服/裤子/鼻环等)的换皮由装备决定，不提供手动皮肤选择
        var setEquipDrivenPart = ItemsInfoCfg.GetEquipDrivenSkinPartTypes(creatureInfo.model_id);
        foreach (var kv in dicAllSkins)
        {
            CreatureSkinTypeEnum skinType = kv.Key;
            if (setEquipDrivenPart.Contains(skinType))
                continue;
            //武器皮肤(>=90)由装备的武器皮肤决定，不提供手动选择（装备驱动判定未覆盖时的兜底）
            if ((int)skinType >= 90)
                continue;
            long currentId = GetCurrentSkinId(skinType);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{skinType.GetEnumName()}: {(currentId == 0 ? "默认" : $"{currentId}")}");
            if (GUILayout.Button("选择", GUILayout.Width(50)))
                OpenSkinSelect(skinType);
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 皮肤颜色调节区：列出所有已装备且支持调色(color_state!=0)的部位，点击色块展开/收起 RGB(A) 滑条+调色盘
    /// </summary>
    private void DrawSkinColorSection()
    {
        DrawSectionHeader("皮肤颜色");
        //随机皮肤模式下皮肤来自随机池且颜色随机，手动调色无意义
        if (IsRandomSkinEnabled())
        {
            EditorGUILayout.LabelField("(已启用随机皮肤，颜色由随机池决定)", EditorStyles.miniLabel);
            return;
        }
        if (previewCreatureData == null)
            return;
        //先快照可调色部位：调色操作会触发RefreshPreview重建dicSkinData，不能边遍历字典边刷新
        var listColorableSkin = new List<(CreatureSkinTypeEnum skinType, int colorState)>();
        foreach (var kv in previewCreatureData.dicSkinData)
        {
            var skinModelInfo = CreatureModelInfoCfg.GetItemData(kv.Value.skinId);
            if (skinModelInfo == null || skinModelInfo.color_state == 0)
                continue;
            listColorableSkin.Add((kv.Key, skinModelInfo.color_state));
        }
        foreach (var colorableSkin in listColorableSkin)
        {
            CreatureSkinTypeEnum skinType = colorableSkin.skinType;
            Color skinColor = GetSkinColor(skinType);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{skinType.GetEnumName()}", GUILayout.Width(90));
            //色块按钮：展示当前颜色，点击展开/收起该部位的调色编辑
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = skinColor;
            if (GUILayout.Button(editingColorSkinType == skinType ? "收起" : "", GUILayout.Width(60)))
                editingColorSkinType = editingColorSkinType == skinType ? CreatureSkinTypeEnum.None : skinType;
            GUI.backgroundColor = oldBgColor;
            EditorGUILayout.EndHorizontal();
            if (editingColorSkinType == skinType)
                DrawSkinColorEditor(skinType, colorableSkin.colorState == 2);
        }
        if (listColorableSkin.Count == 0)
            EditorGUILayout.LabelField("(当前皮肤均不支持调色)", EditorStyles.miniLabel);
    }

    /// <summary>
    /// 单个部位的颜色编辑器：RGB滑条实时应用，color_state==2(可透明)时追加A滑条，下方附调色盘
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
            ApplyEditSkinColor(skinType, newSkinColor);
        DrawSkinColorPalette(skinType, skinColor);
    }

    /// <summary>
    /// 皮肤调色盘区：预设颜色块点选即应用，当前颜色所在色块显示✔
    /// </summary>
    private void DrawSkinColorPalette(CreatureSkinTypeEnum skinType, Color skinColor)
    {
        EditorGUILayout.LabelField("调色盘(点选即应用):", EditorStyles.miniLabel);
        const int paletteColumns = 8;
        for (int i = 0; i < paletteSkinColors.Length; i++)
        {
            if (i % paletteColumns == 0)
                EditorGUILayout.BeginHorizontal();
            Color paletteColor = paletteSkinColors[i];
            bool isCurrentColor = IsApproximatelyColor(skinColor, paletteColor);
            //用backgroundColor给空按钮着色成色块
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = paletteColor;
            if (GUILayout.Button(isCurrentColor ? "✔" : "", GUILayout.Width(30), GUILayout.Height(24)))
            {
                GUI.backgroundColor = oldBgColor;
                ApplyEditSkinColor(skinType, paletteColor);
            }
            GUI.backgroundColor = oldBgColor;
            if (i % paletteColumns == paletteColumns - 1 || i == paletteSkinColors.Length - 1)
                EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 用户手动调色：写调色字典 + 写回编辑副本 + 刷新预览（随机色固化不走这里，见 Preview 装配）
    /// </summary>
    private void ApplyEditSkinColor(CreatureSkinTypeEnum skinType, Color color)
    {
        dicSkinColorEdit[skinType] = color;
        WriteBackSkinColorData();
        RefreshPreview();
    }

    /// <summary>
    /// 取某部位当前的调色（无记录时默认白色=不染色）
    /// </summary>
    private Color GetSkinColor(CreatureSkinTypeEnum skinType)
    {
        return dicSkinColorEdit.TryGetValue(skinType, out var color) ? color : Color.white;
    }

    /// <summary>
    /// 判断两个颜色RGB是否近似相等（用于调色盘当前色高亮）
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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(20));
        value = EditorGUILayout.Slider(value, 0f, 1f);
        EditorGUILayout.EndHorizontal();
        return value;
    }

    /// <summary>
    /// 装备槽区（随机装备下拉+稀有度开关 + 固定装备按槽位选择）
    /// </summary>
    private void DrawEquipSection(CreatureInfoBean creatureInfo)
    {
        DrawSectionHeader("装备");
        DrawRandomEquipPopup();
        DrawEquipRarityToggles();
        var listEquipType = creatureInfo.GetEquipItemsType();
        if (listEquipType.IsNull())
        {
            EditorGUILayout.LabelField("(该生物无可装备槽位)", EditorStyles.miniLabel);
            return;
        }
        foreach (var equipType in listEquipType)
        {
            long currentId = GetCurrentEquipId(equipType);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{equipType.GetEnumName()}: {(currentId == 0 ? "空" : $"{currentId}")}");
            if (GUILayout.Button("选择", GUILayout.Width(50)))
                OpenEquipSelect(equipType);
            EditorGUILayout.EndHorizontal();
        }
    }
    #endregion

    #region 随机池下拉
    /// <summary>随机皮肤池候选 id（懒加载；首项0=不使用）</summary>
    private long[] randomSkinOptionIds;
    /// <summary>随机皮肤池候选显示名</summary>
    private string[] randomSkinOptionLabels;
    /// <summary>随机装备池候选 id（懒加载；首项0=不使用）</summary>
    private long[] randomEquipOptionIds;
    /// <summary>随机装备池候选显示名</summary>
    private string[] randomEquipOptionLabels;

    /// <summary>
    /// 懒加载随机皮肤池候选（CreatureRandomInfo 中 random_type=0 的皮肤池，id+备注，按id排序）
    /// </summary>
    private void EnsureRandomSkinOptions()
    {
        if (randomSkinOptionIds != null)
            return;
        var listId = new List<long> { 0 };
        var listLabel = new List<string> { "0  (不使用随机皮肤)" };
        var allRandomData = CreatureRandomInfoCfg.GetAllArrayData();
        if (allRandomData != null)
        {
            var listPool = new List<CreatureRandomInfoBean>(allRandomData);
            listPool.Sort((a, b) => a.id.CompareTo(b.id));
            foreach (var randomInfo in listPool)
            {
                //只列皮肤池（装备池走随机装备下拉）
                if (randomInfo.GetRandomType() != CreatureRandomTypeEnum.Skin)
                    continue;
                //无备注时回退显示随机池原始数据
                string label = randomInfo.remark.IsNull() ? randomInfo.skin_random_data : randomInfo.remark;
                listId.Add(randomInfo.id);
                listLabel.Add($"{randomInfo.id}  {label}");
            }
        }
        randomSkinOptionIds = listId.ToArray();
        randomSkinOptionLabels = listLabel.ToArray();
    }

    /// <summary>
    /// 懒加载随机装备池候选（random_type=1 散件池与 2 套装池，id+类型前缀+备注，按id排序）
    /// </summary>
    private void EnsureRandomEquipOptions()
    {
        if (randomEquipOptionIds != null)
            return;
        var listId = new List<long> { 0 };
        var listLabel = new List<string> { "0  (不使用随机装备)" };
        var allRandomData = CreatureRandomInfoCfg.GetAllArrayData();
        if (allRandomData != null)
        {
            var listPool = new List<CreatureRandomInfoBean>(allRandomData);
            listPool.Sort((a, b) => a.id.CompareTo(b.id));
            foreach (var randomInfo in listPool)
            {
                var randomType = randomInfo.GetRandomType();
                if (randomType != CreatureRandomTypeEnum.Equip && randomType != CreatureRandomTypeEnum.Suit)
                    continue;
                string typeTag = randomType == CreatureRandomTypeEnum.Suit ? "[套装]" : "[散件]";
                string label = randomInfo.remark.IsNull() ? randomInfo.equip_random_data : randomInfo.remark;
                listId.Add(randomInfo.id);
                listLabel.Add($"{randomInfo.id}  {typeTag}{label}");
            }
        }
        randomEquipOptionIds = listId.ToArray();
        randomEquipOptionLabels = listLabel.ToArray();
    }

    /// <summary>
    /// 随机皮肤下拉：选择随机皮肤池（0=不使用；当前值不在候选内时显示首项但不回写，等用户主动选择才写）
    /// </summary>
    private void DrawRandomSkinPopup()
    {
        EnsureRandomSkinOptions();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("随机皮肤", "CreatureRandomInfo 皮肤池；启用后固定皮肤/调色由随机池接管"), GUILayout.Width(70));
        int currentIndex = System.Array.IndexOf(randomSkinOptionIds, editingNpcInfo.creature_random_id);
        if (currentIndex < 0)
            currentIndex = 0;
        int newIndex = EditorGUILayout.Popup(currentIndex, randomSkinOptionLabels);
        EditorGUILayout.EndHorizontal();
        //仅当用户实际切换了选项才回写（当前值不在候选内被钳到首项时不应静默改写数据）
        if (newIndex == currentIndex)
            return;
        long newRandomId = randomSkinOptionIds[newIndex];
        if (newRandomId != editingNpcInfo.creature_random_id)
        {
            editingNpcInfo.creature_random_id = newRandomId;
            //皮肤选择面板基于固定皮肤编辑，随机模式下无意义，直接关闭
            CloseSelect();
            RefreshPreview();
        }
    }

    /// <summary>
    /// 随机装备下拉：选择装备随机池（0=不使用；切换时保留已选稀有度段；当前值不在候选内时显示首项但不回写，等用户主动选择才写）
    /// </summary>
    private void DrawRandomEquipPopup()
    {
        EnsureRandomEquipOptions();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("随机装备", "CreatureRandomInfo 装备池；配置后创建时从池中抽装备填充空槽位"), GUILayout.Width(70));
        long currentPoolId = editingNpcInfo.GetEquipRandomPoolId();
        int currentIndex = System.Array.IndexOf(randomEquipOptionIds, currentPoolId);
        if (currentIndex < 0)
            currentIndex = 0;
        int newIndex = EditorGUILayout.Popup(currentIndex, randomEquipOptionLabels);
        EditorGUILayout.EndHorizontal();
        //仅当用户实际切换了选项才回写（当前值不在候选内被钳到首项时不应静默改写数据）
        if (newIndex == currentIndex)
            return;
        long newPoolId = randomEquipOptionIds[newIndex];
        if (newPoolId != currentPoolId)
        {
            if (newPoolId == 0)
            {
                editingNpcInfo.SetEquipRandom("");
            }
            else
            {
                //保留已选稀有度段，未选稀有度时默认N
                var rarities = editingNpcInfo.GetEquipRandomRarities();
                string rarityStr = rarities.Count > 0 ? string.Join(",", rarities) : "N";
                editingNpcInfo.SetEquipRandom($"{newPoolId},{rarityStr}");
            }
            RefreshPreview();
        }
    }

    /// <summary>
    /// 随机装备稀有度开关行：点选加入/移出稀有度列表（至少保留1个），选中项绿色高亮
    /// </summary>
    private void DrawEquipRarityToggles()
    {
        if (editingNpcInfo.GetEquipRandomPoolId() == 0)
            return;
        var rarities = editingNpcInfo.GetEquipRandomRarities();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("稀有度", GUILayout.Width(70));
        foreach (RarityEnum rarity in System.Enum.GetValues(typeof(RarityEnum)))
        {
            //魔王标记值不是真实稀有度档位，不参与随机池
            if (rarity == RarityEnum.DemonLord)
                continue;
            bool isSelected = rarities.Contains(rarity);
            Color oldColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button(rarity.ToString(), EditorStyles.miniButton, GUILayout.Width(42)))
            {
                GUI.backgroundColor = oldColor;
                OnToggleEquipRarity(rarity);
            }
            GUI.backgroundColor = oldColor;
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 切换随机装备的稀有度（加入/移出稀有度列表；至少保留1个）并刷新预览
    /// </summary>
    private void OnToggleEquipRarity(RarityEnum rarity)
    {
        long poolId = editingNpcInfo.GetEquipRandomPoolId();
        if (poolId == 0)
            return;
        var rarities = new List<RarityEnum>(editingNpcInfo.GetEquipRandomRarities());
        if (rarities.Contains(rarity))
        {
            //至少保留1个稀有度
            if (rarities.Count <= 1)
                return;
            rarities.Remove(rarity);
        }
        else
        {
            rarities.Add(rarity);
            rarities.Sort();
        }
        editingNpcInfo.SetEquipRandom($"{poolId},{string.Join(",", rarities)}");
        RefreshPreview();
    }
    #endregion

    #region 选择面板栏
    /// <summary>
    /// 绘制选择面板栏（皮肤/装备候选，固定宽，位于中栏与预览栏之间）
    /// </summary>
    private void DrawSelectPanelColumn()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(WidthSelectPanel), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(selectMode == SelectMode.Skin ? "选择皮肤" : "选择装备", titleStyle);
        if (GUILayout.Button("关闭", EditorStyles.miniButton, GUILayout.Width(40)))
            CloseSelect();
        EditorGUILayout.EndHorizontal();
        //当前该部位/该槽位已选中的id，用于列表内高亮
        long currentId = selectMode == SelectMode.Skin
            ? GetCurrentSkinId((CreatureSkinTypeEnum)selectShowType)
            : GetCurrentEquipId((ItemTypeEnum)selectShowType);
        scrollSelect = EditorGUILayout.BeginScrollView(scrollSelect, GUILayout.ExpandHeight(true));
        foreach (var item in listSelectItem)
        {
            //选中项前加勾并变色，方便随时对照切换
            bool isCurrent = item.id == currentId;
            Color oldColor = GUI.backgroundColor;
            if (isCurrent)
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            EditorGUILayout.BeginHorizontal();
            //有图标的选项先画图标预览；无图标项留空位对齐文字
            if (!item.iconName.IsNull())
                DrawSpriteIcon(GetIconCached(item.atlasType, item.iconName), 26);
            else
                GUILayout.Space(30);
            if (GUILayout.Button((isCurrent ? "✔ " : "") + item.label, EditorStyles.miniButtonLeft))
            {
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
                if (selectMode == SelectMode.Skin)
                    OnSelectSkin(item.id);
                else
                    OnSelectEquip(item.id);
                continue;
            }
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
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

    #region 选择逻辑
    /// <summary>
    /// 取某皮肤部位当前选中的皮肤id
    /// </summary>
    private long GetCurrentSkinId(CreatureSkinTypeEnum skinType)
    {
        foreach (long id in listCreatureSkinData)
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
        foreach (long id in listCreatureEquipItemIds)
        {
            var info = ItemsInfoCfg.GetItemData(id);
            if (info != null && info.GetItemType() == itemType)
                return id;
        }
        return 0;
    }

    /// <summary>
    /// 打开皮肤候选列表（穿戴类皮肤按装备反查 icon_res 走 Items 图集，普通皮肤按命名规则走 Skins 图集）
    /// </summary>
    private void OpenSkinSelect(CreatureSkinTypeEnum skinType)
    {
        selectMode = SelectMode.Skin;
        selectShowType = (int)skinType;
        listSelectItem.Clear();
        listSelectItem.Add(new SelectItem(0, "取消(不放置)"));
        var creatureInfo = CreatureInfoCfg.GetItemData(editingNpcInfo.creature_id);
        var creatureModelData = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        if (creatureModelData == null)
            return;
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureInfo.model_id);
        //穿戴类皮肤(帽子/衣服/裤子等)的贴图不在 Skins 图集，而是作为装备图标打进 Items 图集：
        //按 ItemsInfo.creature_model_info_id 反查装备，改用装备的 icon_res 加载（与 IconHandler.SetItemIcon 同逻辑）
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
        if (dicAllSkins != null && dicAllSkins.TryGetValue(skinType, out var listSkinData))
        {
            foreach (var info in listSkinData)
            {
                SpriteAtlasTypeEnum atlasType;
                string iconName;
                if (dicSkinItemIconRes.TryGetValue(info.id, out string itemIconRes))
                {
                    //装备驱动皮肤：走装备图标逻辑（默认 Items 图集, 支持「,图集类型」后缀）
                    ParseIconRes(itemIconRes, SpriteAtlasTypeEnum.Items, out atlasType, out iconName);
                }
                else
                {
                    //普通皮肤图标名由表记录拼接: {CreatureModel.mark_name}_Atlas_{CreatureModelInfo.res_name(/转_)}
                    //（与生成器 SpineWindow.ExtractAndSaveTextures 的 {spineAtlasAsset.name}_{skin.Name} 命名对应）
                    atlasType = SpriteAtlasTypeEnum.Skins;
                    iconName = $"{creatureModelData.mark_name}_Atlas_{info.res_name.Replace("/", "_")}";
                }
                listSelectItem.Add(new SelectItem(info.id, $"{info.id}  {info.res_name}", atlasType, iconName));
            }
        }
        scrollSelect = Vector2.zero;
    }

    /// <summary>
    /// 打开装备候选列表（按槽位类型过滤，带装备图标）
    /// </summary>
    private void OpenEquipSelect(ItemTypeEnum itemType)
    {
        selectMode = SelectMode.Equip;
        selectShowType = (int)itemType;
        listSelectItem.Clear();
        listSelectItem.Add(new SelectItem(0, "取消(不放置)"));
        var creatureInfo = CreatureInfoCfg.GetItemData(editingNpcInfo.creature_id);
        var listItemInfo = ItemsInfoCfg.GetDataByCreatureModelId(creatureInfo.model_id);
        if (listItemInfo != null)
        {
            foreach (var itemInfo in listItemInfo)
            {
                if (itemInfo.GetItemType() == itemType)
                {
                    //装备图标: icon_res 支持「,图集类型」后缀，默认 Items 图集
                    ParseIconRes(itemInfo.icon_res, SpriteAtlasTypeEnum.Items, out SpriteAtlasTypeEnum atlasType, out string iconName);
                    listSelectItem.Add(new SelectItem(itemInfo.id, $"{itemInfo.id}  {GetItemShowName(itemInfo)}", atlasType, iconName));
                }
            }
        }
        scrollSelect = Vector2.zero;
    }

    /// <summary>
    /// 取装备显示名（直读中文映射，不走 name_language；缺配置时给占位）
    /// </summary>
    private string GetItemShowName(ItemsInfoBean itemInfo)
    {
        if (itemInfo.name != 0 && dicItemNameCn.TryGetValue(itemInfo.name, out string itemName))
            return itemName.IsNull() ? "未命名" : itemName;
        return "(未配置名字)";
    }

    /// <summary>
    /// 选中皮肤：替换同部位皮肤后写回并刷新（不关闭选择面板，方便随时切换）；
    /// 新皮肤支持调色(color_state!=0)时自动展开该部位的颜色选择；同部位换皮肤保留已调颜色（与 UIMainCreate 同规则）
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
            //支持调色的皮肤选中后自动打开颜色选择
            var skinModelInfo = CreatureModelInfoCfg.GetItemData(showId);
            if (skinModelInfo != null && skinModelInfo.color_state != 0)
                editingColorSkinType = skinType;
        }
        else if (editingColorSkinType == skinType)
        {
            //取消放置的部位若正在调色，收起编辑器
            editingColorSkinType = CreatureSkinTypeEnum.None;
        }
        WriteBackSkinData();
        //换皮肤后调色数据按固定皮肤重新过滤（移除的部位调色不再保留）
        WriteBackSkinColorData();
        RefreshPreview();
    }

    /// <summary>
    /// 选中装备：替换同类型装备后写回并刷新（不关闭选择面板，方便随时切换）
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
        WriteBackEquipItemIds();
        RefreshPreview();
    }
    #endregion
}
