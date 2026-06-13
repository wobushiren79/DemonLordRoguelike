/*
* FileName: SaveDataEditorWindow
* Author: Claude
* CreateTime: 2026-06-08
* Desc: 游戏层存档编辑器。选择 3 个存档槽位之一加载，按字段/列表展开编辑全部存档数据，
*       支持保存(二次确认)与清除(二次确认)。
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 存档编辑器窗口
/// 通过 Newtonsoft JToken 树对存档 JSON 做通用化展开编辑，
/// 加载/保存/删除走 UserDataService（自动处理主存档备份与 解锁/成就/背包道具/背包生物 拆分文件）。
/// </summary>
public class SaveDataEditorWindow : EditorWindow
{
    #region 常量与字段
    /// <summary>存档槽位总数</summary>
    private const int SlotCount = 3;

    /// <summary>主存档区块在编辑树中的路径前缀</summary>
    private const string PathMain = "main";
    /// <summary>解锁数据区块在编辑树中的路径前缀</summary>
    private const string PathUnlock = "unlock";
    /// <summary>成就数据区块在编辑树中的路径前缀</summary>
    private const string PathAchievement = "achievement";
    /// <summary>背包道具区块在编辑树中的路径前缀</summary>
    private const string PathBackpackItem = "backpackItem";
    /// <summary>背包生物区块在编辑树中的路径前缀</summary>
    private const string PathBackpackCreature = "backpackCreature";

    /// <summary>存档读写服务</summary>
    private UserDataService dataService;

    /// <summary>当前加载的槽位下标，-1 表示未加载</summary>
    private int currentSlot = -1;

    /// <summary>是否已成功加载某个存档</summary>
    private bool isLoaded;

    /// <summary>主存档数据编辑树</summary>
    private JToken mainToken;
    /// <summary>解锁数据编辑树</summary>
    private JToken unlockToken;
    /// <summary>成就数据编辑树</summary>
    private JToken achievementToken;
    /// <summary>背包道具数据编辑树</summary>
    private JToken backpackItemToken;
    /// <summary>背包生物数据编辑树</summary>
    private JToken backpackCreatureToken;

    /// <summary>各折叠区块的展开状态，按路径缓存</summary>
    private readonly Dictionary<string, bool> dicFoldout = new Dictionary<string, bool>();

    /// <summary>字段名 → 中文说明（从 Bean 源码注释自动采集，静态缓存）</summary>
    private static Dictionary<string, string> dicFieldLabel;

    /// <summary>内容区滚动位置</summary>
    private Vector2 scrollPos;
    #endregion

    #region 生命周期
    /// <summary>
    /// 打开存档编辑器窗口
    /// </summary>
    [MenuItem("游戏/存档编辑")]
    private static void ShowWindow()
    {
        var window = GetWindow<SaveDataEditorWindow>();
        window.titleContent = new GUIContent("存档编辑");
        window.minSize = new Vector2(560, 480);
        window.Show();
    }

    /// <summary>
    /// 绘制窗口
    /// </summary>
    private void OnGUI()
    {
        DrawSlotSelector();
        EditorGUILayout.Space();

        if (!isLoaded)
        {
            EditorGUILayout.HelpBox("请选择并加载一个存档槽位。", MessageType.Info);
            return;
        }

        DrawToolbar();
        EditorGUILayout.Space();
        DrawDataTree();
    }
    #endregion

    #region 顶部：槽位选择
    /// <summary>
    /// 绘制 3 个存档槽位的选择/加载按钮
    /// </summary>
    private void DrawSlotSelector()
    {
        EditorGUILayout.LabelField("存档槽位", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int slot = 0; slot < SlotCount; slot++)
        {
            bool exists = IsSlotExists(slot);
            bool isCurrent = isLoaded && currentSlot == slot;

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = isCurrent ? new Color(0.4f, 0.8f, 0.4f) : (exists ? Color.white : new Color(0.7f, 0.7f, 0.7f));
            string label = $"存档 {slot}\n{(exists ? "（已有数据）" : "（空）")}";
            if (GUILayout.Button(label, GUILayout.Height(44)))
            {
                LoadSlot(slot);
            }
            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndHorizontal();

        if (isLoaded)
            EditorGUILayout.LabelField($"当前编辑：存档 {currentSlot}", EditorStyles.miniBoldLabel);
    }
    #endregion

    #region 工具栏：保存 / 清除
    /// <summary>
    /// 绘制保存与清除按钮
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        Color old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("保存存档", GUILayout.Height(30)))
        {
            SaveSlot();
        }

        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
        if (GUILayout.Button("清除存档", GUILayout.Height(30)))
        {
            ClearSlot();
        }
        GUI.backgroundColor = old;

        if (GUILayout.Button("重新加载", GUILayout.Height(30), GUILayout.Width(90)))
        {
            LoadSlot(currentSlot);
        }

        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region 数据树绘制
    /// <summary>
    /// 绘制存档全部数据（主存档 / 解锁 / 成就 / 背包道具 / 背包生物 五大区块）
    /// </summary>
    private void DrawDataTree()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (mainToken != null)
            DrawToken(PathMain, "主存档数据 (UserData)", mainToken);
        if (unlockToken != null)
            DrawToken(PathUnlock, "解锁数据 (UserUnlock)", unlockToken);
        if (achievementToken != null)
            DrawToken(PathAchievement, "成就/统计数据 (UserAchievement)", achievementToken);
        if (backpackItemToken != null)
            DrawToken(PathBackpackItem, "背包道具 (UserBackpackItem)", backpackItemToken);
        if (backpackCreatureToken != null)
            DrawToken(PathBackpackCreature, "背包生物 (UserBackpackCreature)", backpackCreatureToken);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 递归绘制一个 JToken 节点
    /// </summary>
    /// <param name="path">当前节点的唯一路径，用于折叠状态缓存</param>
    /// <param name="label">显示名称</param>
    /// <param name="token">节点</param>
    private void DrawToken(string path, string label, JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                DrawObject(path, label, (JObject)token);
                break;
            case JTokenType.Array:
                DrawArray(path, label, (JArray)token);
                break;
            default:
                DrawValue(label, token as JValue);
                break;
        }
    }

    /// <summary>
    /// 绘制对象节点（可折叠，逐字段递归）
    /// </summary>
    private void DrawObject(string path, string label, JObject obj)
    {
        bool fold = GetFoldout(path, $"{label}  {{{obj.Count}}}");
        if (!fold)
            return;

        EditorGUI.indentLevel++;
        foreach (JProperty prop in new List<JProperty>(obj.Properties()))
        {
            DrawToken($"{path}/{prop.Name}", ToDisplayLabel(prop.Name), prop.Value);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制数组/列表节点（可折叠，按元素列出，支持增删）
    /// </summary>
    private void DrawArray(string path, string label, JArray arr)
    {
        bool fold = GetFoldout(path, $"{label}  [列表 {arr.Count}]");
        if (!fold)
            return;

        EditorGUI.indentLevel++;
        int removeIndex = -1;
        for (int i = 0; i < arr.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                removeIndex = i;
            }
            GUI.backgroundColor = old;

            EditorGUILayout.BeginVertical();
            DrawToken($"{path}[{i}]", $"[{i}]", arr[i]);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            arr.RemoveAt(removeIndex);
            Repaint();
        }

        if (GUILayout.Button("+ 添加元素", GUILayout.Width(120)))
        {
            JToken template = arr.Count > 0 ? arr[arr.Count - 1].DeepClone() : new JValue("");
            arr.Add(template);
            Repaint();
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制叶子值节点（按类型给出对应输入控件，原地写回 JValue.Value）
    /// </summary>
    private void DrawValue(string label, JValue value)
    {
        if (value == null)
        {
            EditorGUILayout.LabelField(label, "(未知)");
            return;
        }

        switch (value.Type)
        {
            case JTokenType.Integer:
                long curLong = Convert.ToInt64(value.Value ?? 0L);
                long newLong = EditorGUILayout.LongField(label, curLong);
                if (newLong != curLong)
                    value.Value = newLong;
                break;
            case JTokenType.Float:
                double curDouble = Convert.ToDouble(value.Value ?? 0d);
                double newDouble = EditorGUILayout.DoubleField(label, curDouble);
                if (!newDouble.Equals(curDouble))
                    value.Value = newDouble;
                break;
            case JTokenType.Boolean:
                bool curBool = Convert.ToBoolean(value.Value ?? false);
                bool newBool = EditorGUILayout.Toggle(label, curBool);
                if (newBool != curBool)
                    value.Value = newBool;
                break;
            case JTokenType.String:
                string curStr = value.Value as string ?? string.Empty;
                string newStr = EditorGUILayout.TextField(label, curStr);
                if (newStr != curStr)
                    value.Value = newStr;
                break;
            case JTokenType.Null:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, "(null)");
                string nullInput = EditorGUILayout.TextField(string.Empty);
                if (!string.IsNullOrEmpty(nullInput))
                    value.Value = nullInput;
                EditorGUILayout.EndHorizontal();
                break;
            default:
                string curOther = value.ToString();
                string newOther = EditorGUILayout.TextField(label, curOther);
                if (newOther != curOther)
                    value.Value = newOther;
                break;
        }
    }

    /// <summary>
    /// 读取并绘制折叠头，返回是否展开
    /// </summary>
    private bool GetFoldout(string path, string label)
    {
        dicFoldout.TryGetValue(path, out bool cur);
        bool next = EditorGUILayout.Foldout(cur, label, true);
        if (next != cur)
            dicFoldout[path] = next;
        return next;
    }
    #endregion

    #region 加载 / 保存 / 清除
    /// <summary>
    /// 加载指定槽位存档到编辑树
    /// </summary>
    private void LoadSlot(int slot)
    {
        EnsureFieldLabelMap();
        dataService ??= new UserDataService();
        dataService.ChangeSlot(slot);
        UserDataBean data = dataService.Load(false);

        if (data == null)
        {
            isLoaded = false;
            currentSlot = -1;
            mainToken = unlockToken = achievementToken = backpackItemToken = backpackCreatureToken = null;
            EditorUtility.DisplayDialog("提示", $"存档 {slot} 不存在（空存档），无可编辑数据。", "确定");
            return;
        }

        try
        {
            // 主存档已 [JsonIgnore] 排除拆分字段，单独序列化拆分数据以便完整展示与编辑
            mainToken = JToken.Parse(JsonUtil.ToJson(data, JsonTypeEnum.Net));
            unlockToken = JToken.Parse(JsonUtil.ToJson(data.GetUserUnlockData(), JsonTypeEnum.Net));
            achievementToken = JToken.Parse(JsonUtil.ToJson(data.GetUserAchievementData(), JsonTypeEnum.Net));
            backpackItemToken = JToken.Parse(JsonUtil.ToJson(data.GetUserBackpackItemsData(), JsonTypeEnum.Net));
            backpackCreatureToken = JToken.Parse(JsonUtil.ToJson(data.GetUserBackpackCreatureData(), JsonTypeEnum.Net));
        }
        catch (Exception e)
        {
            isLoaded = false;
            EditorUtility.DisplayDialog("加载失败", $"存档 {slot} 解析失败：\n{e.Message}", "确定");
            return;
        }

        currentSlot = slot;
        isLoaded = true;
        dicFoldout.Clear();
        scrollPos = Vector2.zero;
    }

    /// <summary>
    /// 保存当前编辑树到对应槽位（二次确认；UserDataService 自动备份并落盘拆分文件）
    /// </summary>
    private void SaveSlot()
    {
        if (!isLoaded)
            return;

        if (!EditorUtility.DisplayDialog("保存存档", $"确认将修改保存到 存档 {currentSlot}？\n该操作会覆盖现有存档（原存档会自动生成备份）。", "确认保存", "取消"))
            return;
        if (!EditorUtility.DisplayDialog("再次确认", $"【二次确认】确定要覆盖 存档 {currentSlot} 吗？此操作不可撤销。", "确定覆盖", "取消"))
            return;

        try
        {
            UserDataBean data = JsonUtil.FromJson<UserDataBean>(mainToken.ToString(), JsonTypeEnum.Net);
            data.userUnlockData = JsonUtil.FromJson<UserUnlockBean>(unlockToken.ToString(), JsonTypeEnum.Net);
            data.userAchievementData = JsonUtil.FromJson<UserAchievementBean>(achievementToken.ToString(), JsonTypeEnum.Net);
            data.userBackpackItemsData = JsonUtil.FromJson<UserBackpackItemsBean>(backpackItemToken.ToString(), JsonTypeEnum.Net) ?? new UserBackpackItemsBean();
            data.userBackpackCreatureData = JsonUtil.FromJson<UserBackpackCreatureBean>(backpackCreatureToken.ToString(), JsonTypeEnum.Net) ?? new UserBackpackCreatureBean();
            data.saveIndex = currentSlot;

            dataService ??= new UserDataService();
            dataService.ChangeSlot(currentSlot);
            dataService.Save(data);

            EditorUtility.DisplayDialog("保存成功", $"存档 {currentSlot} 已保存。", "确定");
            // 重新加载以反映服务端规整后的数据（如备份索引等）
            LoadSlot(currentSlot);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("保存失败", $"序列化或写入失败：\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 清除当前槽位存档（二次确认；删除主存档及拆分文件）
    /// </summary>
    private void ClearSlot()
    {
        if (!isLoaded)
            return;

        if (!EditorUtility.DisplayDialog("清除存档", $"确认要清除 存档 {currentSlot} 吗？\n将删除该槽位的主存档及解锁/成就/背包数据。", "确认清除", "取消"))
            return;
        if (!EditorUtility.DisplayDialog("再次确认", $"【二次确认】存档 {currentSlot} 删除后无法恢复，确定继续？", "确定删除", "取消"))
            return;

        dataService ??= new UserDataService();
        dataService.ChangeSlot(currentSlot);
        dataService.Delete();

        int cleared = currentSlot;
        isLoaded = false;
        currentSlot = -1;
        mainToken = unlockToken = achievementToken = backpackItemToken = backpackCreatureToken = null;
        dicFoldout.Clear();

        EditorUtility.DisplayDialog("清除成功", $"存档 {cleared} 已清除。", "确定");
    }
    #endregion

    #region 字段中文说明采集
    /// <summary>
    /// 待扫描的 Bean 源码根目录（相对项目根）
    /// </summary>
    private static readonly string[] BeanSourceRoots =
    {
        "Assets/Scripts/Bean",
        "Assets/FrameWork/Scripts/Bean",
    };

    /// <summary>
    /// 将字段名转为带中文说明的显示标签：有说明显示「中文 (字段名)」，否则原样返回
    /// 字典的键（枚举名/数字/UUID 等）通常不在字段表中，会原样显示
    /// </summary>
    private static string ToDisplayLabel(string fieldName)
    {
        if (dicFieldLabel != null && dicFieldLabel.TryGetValue(fieldName, out string cn) && !string.IsNullOrEmpty(cn))
            return $"{cn}  ({fieldName})";
        return fieldName;
    }

    /// <summary>
    /// 懒加载构建「字段名 → 中文说明」映射：扫描 Bean 源码，解析字段上方/行尾的 // 注释
    /// </summary>
    private static void EnsureFieldLabelMap()
    {
        if (dicFieldLabel != null)
            return;
        dicFieldLabel = new Dictionary<string, string>();
        foreach (string root in BeanSourceRoots)
        {
            if (!Directory.Exists(root))
                continue;
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                ParseBeanFile(file);
            }
        }
    }

    /// <summary>
    /// 解析单个 Bean 源码文件，提取字段注释写入映射表
    /// 字段判定：以 public 开头、声明头部（= 或 ; 之前）不含 { 与 ( ，以排除属性与方法
    /// </summary>
    private static void ParseBeanFile(string file)
    {
        string[] lines;
        try { lines = File.ReadAllLines(file); }
        catch { return; }

        string pending = null;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0)
            {
                pending = null;
                continue;
            }
            // 特性行，保留上方累计的注释
            if (line.StartsWith("["))
                continue;
            // 注释行，累计为待用说明
            if (line.StartsWith("//"))
            {
                string c = CleanComment(line);
                pending = string.IsNullOrEmpty(pending) ? c : pending + c;
                continue;
            }
            if (line.StartsWith("public "))
            {
                int eqIdx = line.IndexOf('=');
                int semiIdx = line.IndexOf(';');
                string head = eqIdx >= 0 ? line.Substring(0, eqIdx)
                            : (semiIdx >= 0 ? line.Substring(0, semiIdx) : line);
                // 头部含 ( 视为方法，含 { 视为属性，均跳过
                if (!head.Contains("(") && !head.Contains("{") && semiIdx >= 0)
                {
                    Match m = Regex.Match(head.TrimEnd(), @"(\w+)\s*$");
                    if (m.Success)
                    {
                        string name = m.Groups[1].Value;
                        string trailing = ExtractTrailingComment(line);
                        string label = !string.IsNullOrEmpty(trailing) ? trailing : pending;
                        if (!string.IsNullOrEmpty(label) && !dicFieldLabel.ContainsKey(name))
                            dicFieldLabel[name] = label;
                    }
                }
            }
            pending = null;
        }
    }

    /// <summary>
    /// 清洗注释文本：去掉前导斜杠与 summary 标签并 Trim
    /// </summary>
    private static string CleanComment(string commentLine)
    {
        string c = commentLine;
        while (c.StartsWith("/"))
            c = c.Substring(1);
        c = c.Replace("<summary>", string.Empty).Replace("</summary>", string.Empty);
        return c.Trim();
    }

    /// <summary>
    /// 提取行尾 // 注释，无则返回 null
    /// </summary>
    private static string ExtractTrailingComment(string line)
    {
        int idx = line.IndexOf("//", StringComparison.Ordinal);
        if (idx < 0)
            return null;
        return CleanComment(line.Substring(idx));
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 判断指定槽位是否已有存档文件
    /// </summary>
    private bool IsSlotExists(int slot)
    {
        string path = $"{Application.persistentDataPath}/UserData_{slot}/UserData_{slot}";
        return System.IO.File.Exists(path);
    }
    #endregion
}
