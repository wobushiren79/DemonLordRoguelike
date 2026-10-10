using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 终焉议会编辑工具 - 议案页签
/// 用于可视化编辑 excel_doom_council_info[终焉议会信息] 表（左侧议案列表 + 右侧参数编辑）
/// 宿主窗口见 DoomCouncilEditorWindow（菜单：游戏/终焉议会编辑）
/// </summary>
public class DoomCouncilEditorTabBill : DoomCouncilEditorTabBase<DoomCouncilInfoBean>
{
    #region 基类抽象实现

    /// <summary>Excel 文件名</summary>
    protected override string ExcelFileName => "excel_doom_council_info[终焉议会信息].xlsx";

    /// <summary>工作表名称</summary>
    protected override string SheetName => "DoomCouncilInfo";

    /// <summary>多语言 JSON 文件名</summary>
    protected override string LanguageFileName => "Language_DoomCouncilInfo_cn.txt";

    /// <summary>
    /// 列表项显示名：优先取议案名文本ID对应中文，取不到回退备注
    /// </summary>
    protected override string GetItemDisplayName(DoomCouncilInfoBean bean)
    {
        if (languageMap.TryGetValue(bean.name, out LanguageJsonItem item) && !string.IsNullOrEmpty(item.content))
            return item.content;
        return string.IsNullOrEmpty(bean.remark) ? "(未命名)" : bean.remark;
    }

    #endregion

    #region 议案效果实体类（class_entity_name 选项与 class_entity_data 参数提示）

    /// <summary>议案效果实体类名列表（与 Assets/Scripts/Game/DoomCouncil/ 下的实体一一对应）</summary>
    private static readonly string[] EntityClassNames =
    {
        "DoomCouncilEntityMoreCrystal",
        "DoomCouncilEntityMoreExp",
        "DoomCouncilEntityMoreEquip",
        "DoomCouncilEntityMoreDemonLordEquip",
        "DoomCouncilEntityEnemyIntensity",
        "DoomCouncilEntityCreatureLevelDown",
        "DoomCouncilEntityCreatureRarityDown",
        "DoomCouncilEntityReincarnation",
        "DoomCouncilEntityRename",
    };

    /// <summary>各实体类 class_entity_data 的参数格式提示（取自实体源码注释，选错参数实体解析会失败）</summary>
    private static readonly Dictionary<string, string> EntityDataHints = new Dictionary<string, string>
    {
        { "DoomCouncilEntityMoreCrystal", "参数 = 战斗模式枚举名(Test/Infinite/Conquer/DoomCouncil/ChallengeHundred)：该模式掉落魔晶时额外+等量魔晶，该模式战斗结束时移除此议案" },
        { "DoomCouncilEntityMoreExp", "参数 = 战斗模式枚举名(同上)：该模式结算发放经验时阵容追加一份同等经验(经验翻倍)，该模式战斗结束时移除此议案" },
        { "DoomCouncilEntityMoreEquip", "参数留空：下一次征服通关领奖宝箱基础奖励变为「3件全装备」，征服战斗结束(输赢皆然)时移除此议案" },
        { "DoomCouncilEntityMoreDemonLordEquip", "参数留空：下一次征服通关领奖宝箱基础奖励变为「3件全魔王装备」；与「更多装备」同时在列时本议案优先；征服战斗结束时移除此议案" },
        { "DoomCouncilEntityEnemyIntensity", "参数 = 强度倍率(如 2=翻倍强 / 0.5=减半弱)：作用于下一整场征服模式所有关卡(含BOSS)敌人的 生命/护甲/攻击" },
        { "DoomCouncilEntityCreatureLevelDown", "参数 = 1：魔物等级下降1级(下限0级)；0：魔物等级直接归0" },
        { "DoomCouncilEntityCreatureRarityDown", "参数 = 1：魔物稀有度下降1级；0：稀有度直接归0(降到N级)" },
        { "DoomCouncilEntityReincarnation", "参数 = 生物ID(creature_id)：魔王转生为该生物" },
        { "DoomCouncilEntityRename", "参数 = 1：魔物重命名；2：魔王重命名" },
    };

    #endregion

    #region 解锁下拉数据

    /// <summary>解锁ID到备注名的映射（从 UnlockInfo.txt 读取）</summary>
    private Dictionary<long, string> unlockNameMap = new Dictionary<long, string>();

    /// <summary>解锁下拉选项ID列表（升序，与unlockOptionNames一一对应）</summary>
    private List<long> unlockOptionIds = new List<long>();

    /// <summary>解锁下拉选项显示文本（格式 "[id] 备注名"）</summary>
    private List<string> unlockOptionNames = new List<string>();

    /// <summary>
    /// 加载 UnlockInfo 数据（直接从 Json 文件读取，建立解锁ID到备注名的映射）
    /// </summary>
    private void LoadUnlockInfoData()
    {
        unlockNameMap.Clear();
        string unlockJsonPath = jsonFolderPath + "/UnlockInfo.txt";
        if (!File.Exists(unlockJsonPath))
            return;

        try
        {
            string jsonText = File.ReadAllText(unlockJsonPath);
            UnlockInfoBean[] unlockArray = JsonConvert.DeserializeObject<UnlockInfoBean[]>(jsonText);
            if (unlockArray == null)
                return;

            foreach (var unlock in unlockArray)
            {
                unlockNameMap[unlock.id] = unlock.remark;
            }

            // 按ID升序构建下拉选项
            unlockOptionIds.Clear();
            unlockOptionNames.Clear();
            List<long> sortedIds = new List<long>(unlockNameMap.Keys);
            sortedIds.Sort();
            foreach (long id in sortedIds)
            {
                unlockOptionIds.Add(id);
                unlockOptionNames.Add($"[{id}] {unlockNameMap[id]}");
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载UnlockInfo数据失败: {e.Message}");
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 宿主窗口启用时初始化路径和加载数据（基类加载主表与语言，这里追加解锁表）
    /// </summary>
    public override void Init()
    {
        base.Init();
        LoadUnlockInfoData();
    }

    #endregion

    #region UI 绘制 - 字段编辑区

    /// <summary>
    /// 绘制议案字段编辑区（基础信息/费用与通过/效果配置/文本配置/备注）
    /// </summary>
    protected override void DrawEditFields()
    {
        // 基础信息
        DrawSectionTitle("基础信息");
        DrawIdField();
        currentBean.icon_res = DrawStringField(new GUIContent("图标名字", "icon_res：图标资源名"), currentBean.icon_res, "icon_res");
        currentBean.unlock_id = DrawUnlockIdField();

        // 费用与通过
        DrawSectionTitle("费用与通过");
        currentBean.success_rate = DrawFloatField(new GUIContent("通过率", "success_rate：0~1；>=1 直接生效不进议会投票"), currentBean.success_rate, "success_rate");
        currentBean.council_num = DrawStringField(new GUIContent("议会人数", "council_num：min,max 区间随机"), currentBean.council_num, "council_num");
        currentBean.cost_reputation = DrawLongField(new GUIContent("消耗声望", "cost_reputation"), currentBean.cost_reputation, "cost_reputation");
        currentBean.cost_crystal = DrawLongField(new GUIContent("消耗魔晶", "cost_crystal"), currentBean.cost_crystal, "cost_crystal");

        // 效果配置
        DrawSectionTitle("效果配置");
        currentBean.class_entity_name = DrawEntityClassField();
        currentBean.class_entity_data = DrawEntityDataField();

        // 文本配置
        DrawSectionTitle("文本配置");
        currentBean.name = DrawLanguageIdField(new GUIContent("名字文本ID", "name：多语言文本ID，预览为中文议案名"), currentBean.name, "name", false);
        currentBean.details = DrawLanguageIdField(new GUIContent("描述文本ID", "details：多语言文本ID，预览为中文议案描述"), currentBean.details, "details", true);

        // 备注
        DrawSectionTitle("备注");
        currentBean.remark = DrawStringField(new GUIContent("备注", "remark"), currentBean.remark, "remark");
    }

    /// <summary>
    /// 绘制解锁ID字段（手输ID + 下拉按备注名选取，二者等价）
    /// </summary>
    private long DrawUnlockIdField()
    {
        long result = currentBean.unlock_id;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("解锁ID", "unlock_id：研究解锁ID，议案列表按此过滤"), GUILayout.Width(FieldLabelWidth));

        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified("unlock_id")) GUI.backgroundColor = ModifiedBgColor;
        result = EditorGUILayout.LongField(result, GUILayout.Width(110));
        GUI.backgroundColor = prevColor;

        // 下拉按名字选取（当前ID不在选项中时显示空，可配合手动输入使用）
        if (unlockOptionIds.Count > 0)
        {
            int index = unlockOptionIds.IndexOf(result);
            int newIndex = EditorGUILayout.Popup(index, unlockOptionNames.ToArray());
            if (newIndex != index && newIndex >= 0 && newIndex < unlockOptionIds.Count)
            {
                result = unlockOptionIds[newIndex];
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制议案效果实体类字段（手输类名 + 下拉快捷选取已知实体，二者等价）
    /// </summary>
    private string DrawEntityClassField()
    {
        string result = currentBean.class_entity_name ?? "";
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("效果实体类", "class_entity_name：议案效果实体类名(反射实例化)"), GUILayout.Width(FieldLabelWidth));

        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified("class_entity_name")) GUI.backgroundColor = ModifiedBgColor;
        result = EditorGUILayout.TextField(result);
        GUI.backgroundColor = prevColor;

        // 下拉快捷选取（当前值不在已知列表时显示空，手输任意类名也合法）
        int index = Array.IndexOf(EntityClassNames, result);
        int newIndex = EditorGUILayout.Popup(index, EntityClassNames, GUILayout.Width(220));
        if (newIndex != index && newIndex >= 0 && newIndex < EntityClassNames.Length)
        {
            result = EntityClassNames[newIndex];
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制实例参数字段（下方显示当前实体类的参数格式提示）
    /// </summary>
    private string DrawEntityDataField()
    {
        string result = DrawStringField(new GUIContent("实例参数", "class_entity_data：效果实体的参数，格式随实体类而定(见下方提示)"), currentBean.class_entity_data ?? "", "class_entity_data");

        // 按当前选中的实体类显示参数格式提示，避免填错格式实体解析失败
        string entityName = currentBean.class_entity_name ?? "";
        if (EntityDataHints.TryGetValue(entityName, out string hint))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(FieldLabelWidth + 4);
            EditorGUILayout.LabelField(hint, paramHintStyle);
            EditorGUILayout.EndHorizontal();
        }
        return result;
    }

    #endregion
}
