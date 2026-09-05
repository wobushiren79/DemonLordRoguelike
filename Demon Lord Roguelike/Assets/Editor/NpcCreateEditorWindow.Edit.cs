using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NpcCreateEditorWindow 中栏上区（partial）：基础字段与属性编辑。
/// 所有控件直接改写编辑副本 editingNpcInfo（脏判定由主类快照比对覆盖）；
/// 只有影响外观的字段（生物/体型）变更时才刷新预览。
/// </summary>
public partial class NpcCreateEditorWindow : EditorWindow
{
    #region 字段
    /// <summary>基础字段区滚动位置</summary>
    private Vector2 scrollEditBase;
    /// <summary>生物下拉候选 id 缓存（懒加载，ReloadAllCfg 后失效重建）</summary>
    private long[] creatureOptionIds;
    /// <summary>生物下拉候选显示名缓存</summary>
    private string[] creatureOptionLabels;
    /// <summary>NPC类型下拉值（与显示名同序）</summary>
    private static readonly int[] npcTypeValues = { 0, 1, 2, 3 };
    /// <summary>NPC类型下拉显示名</summary>
    private static readonly string[] npcTypeLabels = { "0 默认", "1 战斗", "2 议会固定", "3 议会随机" };
    /// <summary>稀有度下拉显示名（序号即稀有度值，0=未配置按N计）</summary>
    private static readonly string[] rarityLabels = { "0 未配置(N)", "1 N", "2 R", "3 SR", "4 SSR", "5 UR", "6 L" };
    #endregion

    #region 下拉候选缓存
    /// <summary>
    /// 使下拉候选缓存失效（ReloadAllCfg 时调用；外观区的随机池候选一并失效）
    /// </summary>
    private void InvalidateOptionsCache()
    {
        creatureOptionIds = null;
        creatureOptionLabels = null;
        randomSkinOptionIds = null;
        randomSkinOptionLabels = null;
        randomEquipOptionIds = null;
        randomEquipOptionLabels = null;
        dicIconCache.Clear();
    }

    /// <summary>
    /// 懒加载生物(CreatureInfo)下拉候选（id+中文名，按id排序）
    /// </summary>
    private void EnsureCreatureOptions()
    {
        if (creatureOptionIds != null)
            return;
        var allCreatureData = CreatureInfoCfg.GetAllArrayData();
        var listId = new List<long>();
        var listLabel = new List<string>();
        if (allCreatureData != null)
        {
            foreach (var creatureInfo in allCreatureData)
            {
                listId.Add(creatureInfo.id);
                string creatureName = dicCreatureNameCn.TryGetValue(creatureInfo.name, out string nameCn) && !nameCn.IsNull()
                    ? nameCn
                    : "(未命名)";
                listLabel.Add($"{creatureInfo.id}  {creatureName}");
            }
        }
        //按id排序（labels跟随）
        var indices = new List<int>();
        for (int i = 0; i < listId.Count; i++)
            indices.Add(i);
        indices.Sort((a, b) => listId[a].CompareTo(listId[b]));
        creatureOptionIds = new long[listId.Count];
        creatureOptionLabels = new string[listId.Count];
        for (int i = 0; i < indices.Count; i++)
        {
            creatureOptionIds[i] = listId[indices[i]];
            creatureOptionLabels[i] = listLabel[indices[i]];
        }
    }
    #endregion

    #region 中栏绘制
    /// <summary>
    /// 绘制中栏（弹性宽；上区基础字段定高滚动 + 下区外观编辑弹性滚动）
    /// </summary>
    private void DrawEditColumn()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (editingNpcInfo == null)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("← 请在左侧选择或新建 NPC", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            return;
        }
        //上区：基础字段+属性
        scrollEditBase = EditorGUILayout.BeginScrollView(scrollEditBase, GUILayout.Height(330));
        DrawBaseFieldSection();
        DrawAttributeSection();
        EditorGUILayout.EndScrollView();
        //下区：外观编辑（Appearance partial）
        scrollAppearance = EditorGUILayout.BeginScrollView(scrollAppearance, GUILayout.ExpandHeight(true));
        DrawAppearanceSection();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制基础字段区（id/名字/生物/类型/稀有度/等级/评级 + 各字符串配置字段）
    /// </summary>
    private void DrawBaseFieldSection()
    {
        DrawSectionHeader(isNewEntry ? $"基础字段  [新建·id {editingNpcInfo.id}]" : $"基础字段  [id {editingNpcInfo.id}]");

        //中文名（写语言表 content_cn，NPC行 name 列保存时恒写 id）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("中文名", GUILayout.Width(90));
        editingNameCn = EditorGUILayout.TextField(editingNameCn);
        EditorGUILayout.EndHorizontal();

        DrawCreaturePopup();
        DrawIntPopup("NPC类型", editingNpcInfo.npc_type, npcTypeValues, npcTypeLabels, v => editingNpcInfo.npc_type = v);
        DrawIntPopup("稀有度", editingNpcInfo.rarity, null, rarityLabels, v => editingNpcInfo.rarity = v);

        //议会评级（仅议会类型显示）
        if (editingNpcInfo.npc_type == (int)NpcTypeEnum.Councilor || editingNpcInfo.npc_type == (int)NpcTypeEnum.CouncilorRandom)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("议会评级", "1~5，议会NPC生效"), GUILayout.Width(90));
            editingNpcInfo.councilor_ratings = EditorGUILayout.IntSlider(editingNpcInfo.councilor_ratings, 1, 5);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("等级", GUILayout.Width(90));
        editingNpcInfo.level = EditorGUILayout.IntField(editingNpcInfo.level);
        EditorGUILayout.EndHorizontal();

        //体型（影响预览缩放，变更后刷新预览）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("体型 body_size", "空或0=默认1倍；\"0.9,1.1\"=区间随机；\"1.1\"=固定倍数"), GUILayout.Width(90));
        string newBodySize = EditorGUILayout.TextField(editingNpcInfo.body_size);
        if (newBodySize != editingNpcInfo.body_size)
        {
            editingNpcInfo.body_size = newBodySize;
            RefreshPreview();
        }
        EditorGUILayout.EndHorizontal();

        DrawTextFieldWithHint("称号", "title_data", "称号id，多个用 & 分隔（TitleInfo）", v => editingNpcInfo.title_data = v, editingNpcInfo.title_data);
        DrawAttackModeExtField();
        DrawTextFieldWithHint("地区限制", "region", "空=不限；语言代码如 cn 或 cn,en（仅终焉议会议员生成生效）", v => editingNpcInfo.region = v, editingNpcInfo.region);
        DrawIconResField();
        DrawTextFieldWithHint("备注", "remark", "仅策划查看，不影响游戏", v => editingNpcInfo.remark = v, editingNpcInfo.remark);
    }

    /// <summary>
    /// 绘制生物(CreatureInfo)下拉（id+中文名），切换时按同模组/不同模组处理选配数据
    /// </summary>
    private void DrawCreaturePopup()
    {
        EnsureCreatureOptions();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("生物", "生物原型（CreatureInfo），决定Spine模型与可装备槽位"), GUILayout.Width(90));
        int currentIndex = Array.IndexOf(creatureOptionIds, editingNpcInfo.creature_id);
        //creature_id=0（无实体NPC，仅对话用）或未找到时显示自定义项
        string[] labels = currentIndex < 0
            ? PrependOption(creatureOptionLabels, $"{editingNpcInfo.creature_id}  (未选择/未知)")
            : creatureOptionLabels;
        if (currentIndex < 0)
            currentIndex = 0;
        int newIndex = EditorGUILayout.Popup(currentIndex, labels);
        EditorGUILayout.EndHorizontal();
        //prependOffset=1 表示头部插入了兜底项，序号需偏移；选中兜底项（index 0）视为不切换
        int prependOffset = labels.Length - creatureOptionIds.Length;
        if (newIndex < prependOffset)
            return;
        long newCreatureId = creatureOptionIds[newIndex - prependOffset];
        if (newCreatureId != editingNpcInfo.creature_id)
            OnChangeCreatureInfo(newCreatureId);
    }

    /// <summary>
    /// 数组头部插入一个选项（用于当前值不在候选列表时的兜底显示）
    /// </summary>
    private string[] PrependOption(string[] options, string first)
    {
        var listData = new List<string> { first };
        listData.AddRange(options);
        return listData.ToArray();
    }

    /// <summary>
    /// 切换生物：同模组(model_id相同)保留皮肤/装备/调色/随机池仅过滤不支持槽位的装备；不同模组清空全部选配
    /// </summary>
    private void OnChangeCreatureInfo(long creatureId)
    {
        if (editingNpcInfo == null || editingNpcInfo.creature_id == creatureId)
            return;
        var newCreatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        if (newCreatureInfo == null)
        {
            EditorUtility.DisplayDialog("切换失败", $"找不到生物配置: {creatureId}", "确定");
            return;
        }
        var oldCreatureInfo = CreatureInfoCfg.GetItemData(editingNpcInfo.creature_id);
        bool isSameModel = oldCreatureInfo != null && oldCreatureInfo.model_id == newCreatureInfo.model_id;
        editingNpcInfo.creature_id = creatureId;
        if (isSameModel)
        {
            //同模组也可能装备槽配置不同，过滤掉新生物不支持的装备类型
            var listNewEquipType = newCreatureInfo.GetEquipItemsType();
            listCreatureEquipItemIds.RemoveAll(itemId =>
            {
                var itemInfo = ItemsInfoCfg.GetItemData(itemId);
                return itemInfo == null || !listNewEquipType.Contains(itemInfo.GetItemType());
            });
            WriteBackEquipItemIds();
        }
        else
        {
            //旧物种的皮肤/装备/调色/随机池都与模型绑定，一并清空
            listCreatureSkinData.Clear();
            WriteBackSkinData();
            listCreatureEquipItemIds.Clear();
            WriteBackEquipItemIds();
            dicSkinColorEdit.Clear();
            WriteBackSkinColorData();
            editingColorSkinType = CreatureSkinTypeEnum.None;
            editingNpcInfo.creature_random_id = 0;
            editingNpcInfo.SetEquipRandom("");
            CloseSelect();
        }
        RefreshPreview();
    }

    /// <summary>
    /// 绘制属性编辑区（直接绑定编辑副本 float 字段）
    /// </summary>
    private void DrawAttributeSection()
    {
        DrawSectionHeader("属性");
        editingNpcInfo.HP = DrawAttrFloat("生命值 HP", editingNpcInfo.HP);
        editingNpcInfo.MP = DrawAttrFloat("魔力 MP", editingNpcInfo.MP);
        editingNpcInfo.DR = DrawAttrFloat("护甲 DR", editingNpcInfo.DR);
        editingNpcInfo.ATK = DrawAttrFloat("攻击力 ATK", editingNpcInfo.ATK);
        editingNpcInfo.ASPD = DrawAttrFloat("攻击速度 ASPD", editingNpcInfo.ASPD);
        editingNpcInfo.MSPD = DrawAttrFloat("移动速度 MSPD", editingNpcInfo.MSPD);
        editingNpcInfo.attack_search_range = DrawAttrFloat("搜索范围", editingNpcInfo.attack_search_range);
    }

    /// <summary>
    /// 单行浮点属性输入
    /// </summary>
    private float DrawAttrFloat(string label, float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(110));
        value = EditorGUILayout.FloatField(value);
        EditorGUILayout.EndHorizontal();
        return value;
    }
    #endregion

    #region 字段控件辅助
    /// <summary>
    /// 绘制 int Popup 字段（values 为 null 时序号即值，从0开始；当前值不在候选内时显示首项但不回写，等用户主动选择才写）
    /// </summary>
    private void DrawIntPopup(string label, int currentValue, int[] values, string[] labels, Action<int> actionSet)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(90));
        int currentIndex = values != null ? Array.IndexOf(values, currentValue) : Mathf.Clamp(currentValue, 0, labels.Length - 1);
        if (currentIndex < 0)
            currentIndex = 0;
        int newIndex = EditorGUILayout.Popup(currentIndex, labels);
        EditorGUILayout.EndHorizontal();
        //仅当用户实际切换了选项才回写（当前值非法被钳到首项时不应静默改写数据）
        if (newIndex != currentIndex)
            actionSet(values != null ? values[newIndex] : newIndex);
    }

    /// <summary>
    /// 绘制带格式提示的文本字段（提示以小字显示在输入框下方）
    /// </summary>
    private void DrawTextFieldWithHint(string label, string tooltip, string hint, Action<string> actionSet, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, hint), GUILayout.Width(90));
        string newValue = EditorGUILayout.TextField(value ?? "");
        EditorGUILayout.EndHorizontal();
        if (newValue != value)
            actionSet(newValue);
    }

    /// <summary>
    /// 绘制Boss额外技能字段（attack_mode_ext，逗号分隔 AttackModeExtInfo id）+ 校验按钮
    /// </summary>
    private void DrawAttackModeExtField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("额外技能", "Boss额外技能（AttackModeExtInfo的id，多个用英文逗号分隔）"), GUILayout.Width(90));
        string newValue = EditorGUILayout.TextField(editingNpcInfo.attack_mode_ext ?? "");
        EditorGUILayout.EndHorizontal();
        if (newValue != editingNpcInfo.attack_mode_ext)
            editingNpcInfo.attack_mode_ext = newValue;
        if (GUILayout.Button("校验技能id", EditorStyles.miniButton, GUILayout.Width(80)))
            ValidateAttackModeExt();
    }

    /// <summary>
    /// 校验 attack_mode_ext 配置的 id 是否都存在于 AttackModeExtInfo 表
    /// </summary>
    private void ValidateAttackModeExt()
    {
        if (editingNpcInfo.attack_mode_ext.IsNull())
        {
            EditorUtility.DisplayDialog("校验通过", "未配置额外技能。", "确定");
            return;
        }
        var listId = editingNpcInfo.attack_mode_ext.SplitForListLong(',');
        var listInvalid = new List<long>();
        foreach (long extId in listId)
            if (AttackModeExtInfoCfg.GetItemData(extId) == null)
                listInvalid.Add(extId);
        if (listInvalid.Count == 0)
            EditorUtility.DisplayDialog("校验通过", $"{listId.Count} 个技能 id 全部存在。", "确定");
        else
            EditorUtility.DisplayDialog("校验失败", $"以下 id 在 AttackModeExtInfo 表中不存在:\n{string.Join(",", listInvalid)}", "确定");
    }

    /// <summary>
    /// 绘制头像字段（icon_res，支持「名,图集」后缀）+ 32px 图标预览
    /// </summary>
    private void DrawIconResField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("头像 icon_res", "UI图集sprite名（支持「名,图集」后缀；空=用spine形象；无spine资源的NPC配置此字段）"), GUILayout.Width(90));
        string newValue = EditorGUILayout.TextField(editingNpcInfo.icon_res ?? "");
        if (newValue != editingNpcInfo.icon_res)
            editingNpcInfo.icon_res = newValue;
        ParseIconRes(editingNpcInfo.icon_res, SpriteAtlasTypeEnum.UI, out SpriteAtlasTypeEnum atlasType, out string iconName);
        DrawSpriteIcon(GetIconCached(atlasType, iconName), 32);
        EditorGUILayout.EndHorizontal();
    }
    #endregion
}
