using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NpcCreateEditorWindow 左栏（partial）：NPC 列表（搜索/筛选/排序）+ 新建/删除入口。
/// 列表数据来自 NpcInfoCfg（ReloadAllCfg 时重建）；新建项与删除登记仅存在于内存，保存时才落盘。
/// </summary>
public partial class NpcCreateEditorWindow : EditorWindow
{
    #region 列表绘制
    /// <summary>
    /// 绘制左栏（NPC列表 + 新建/删除）
    /// </summary>
    private void DrawNpcListColumn()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(widthNpcList), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("NPC 列表", titleStyle);
        EditorGUILayout.Space(2);

        //搜索框：id包含 或 中文名模糊
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索", GUILayout.Width(34));
        searchText = EditorGUILayout.TextField(searchText);
        EditorGUILayout.EndHorizontal();

        //筛选行：类型 + 稀有度
        EditorGUILayout.BeginHorizontal();
        filterNpcTypeIndex = EditorGUILayout.Popup(filterNpcTypeIndex, filterNpcTypeLabels, GUILayout.MinWidth(80));
        filterRarityIndex = EditorGUILayout.Popup(filterRarityIndex, filterRarityLabels, GUILayout.MinWidth(70));
        EditorGUILayout.EndHorizontal();

        //排序按钮：点击循环切换排序模式
        if (GUILayout.Button($"排序: {sortModeLabels[sortMode]}", EditorStyles.miniButton))
            sortMode = (sortMode + 1) % sortModeLabels.Length;

        EditorGUILayout.Space(2);
        DrawNpcListItems();
        EditorGUILayout.Space(2);
        DrawListBottomButtons();
        if (isCreatingNew)
            DrawCreateNewPanel();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制NPC列表项（搜索/筛选/排序后；新建项置顶显示[新]，删除登记项置灰）
    /// </summary>
    private void DrawNpcListItems()
    {
        scrollNpcList = EditorGUILayout.BeginScrollView(scrollNpcList, GUILayout.ExpandHeight(true));
        //未保存的新建项不在Cfg里，单独置顶绘制
        if (isNewEntry && editingNpcInfo != null)
            DrawNpcListItem(editingNpcInfo.id, $"[新] {editingNpcInfo.id}  {GetNpcNameCn(editingNpcInfo)}", false);
        foreach (var npcInfo in GetFilteredNpcList())
        {
            bool isDeleted = deletedNpcIds.Contains(npcInfo.id);
            string label = $"{npcInfo.id}  {GetNpcNameCn(npcInfo)}";
            DrawNpcListItem(npcInfo.id, isDeleted ? $"❌ {label}" : label, isDeleted);
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制单个NPC列表项（选中高亮，删除登记项置灰且点击取消登记）
    /// </summary>
    private void DrawNpcListItem(long npcId, string label, bool isDeleted)
    {
        bool isSelected = editingNpcInfo != null && editingNpcInfo.id == npcId;
        Color oldColor = GUI.backgroundColor;
        if (isDeleted)
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f);
        else if (isSelected)
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button((isSelected ? "▶ " : "") + label, EditorStyles.miniButtonLeft))
        {
            if (isDeleted)
                deletedNpcIds.Remove(npcId);
            else
                SelectNpc(npcId);
        }
        GUI.backgroundColor = oldColor;
    }

    /// <summary>
    /// 取搜索/筛选/排序后的NPC列表
    /// </summary>
    private List<NpcInfoBean> GetFilteredNpcList()
    {
        var listData = new List<NpcInfoBean>(listAllNpc);
        int filterType = filterNpcTypeValues[filterNpcTypeIndex];
        if (filterType >= 0)
            listData.RemoveAll(item => item.npc_type != filterType);
        if (filterRarityIndex > 0)
            listData.RemoveAll(item => (item.rarity <= 0 ? 1 : item.rarity) != filterRarityIndex);
        if (!searchText.IsNull())
        {
            string search = searchText.Trim();
            listData.RemoveAll(item =>
                !$"{item.id}".Contains(search)
                && !(GetNpcNameCn(item)?.Contains(search) ?? false));
        }
        if (sortMode == 1)
            listData.Sort((a, b) => b.id.CompareTo(a.id));
        else if (sortMode == 2)
            listData.Sort((a, b) => (b.rarity <= 0 ? 1 : b.rarity).CompareTo(a.rarity <= 0 ? 1 : a.rarity));
        else
            listData.Sort((a, b) => a.id.CompareTo(b.id));
        return listData;
    }
    #endregion

    #region 选中与加载
    /// <summary>
    /// 选中NPC加载进编辑区（脏数据三选保护；选中新 id 会重建预览）
    /// </summary>
    private void SelectNpc(long npcId)
    {
        //已在编辑该项（含未保存的新建项）时不重复加载
        if (editingNpcInfo != null && editingNpcInfo.id == npcId)
            return;
        if (!ConfirmDiscardIfDirty())
            return;
        var npcInfo = listAllNpc.Find(item => item.id == npcId);
        if (npcInfo == null)
            return;
        LoadNpcToEditor(npcInfo, false, GetNpcNameCnRaw(npcInfo));
    }

    /// <summary>
    /// 取NPC中文名原始值（供编辑框；语言行缺失返回空串，不带给列表展示的占位符）
    /// </summary>
    private string GetNpcNameCnRaw(NpcInfoBean npcInfo)
    {
        if (npcInfo == null || npcInfo.name == 0)
            return "";
        return dicNpcNameCn.TryGetValue(npcInfo.name, out string npcName) ? npcName : "";
    }

    /// <summary>
    /// 把NPC配置装配为编辑副本并加载（初始化外观编辑状态 + 提交快照 + 重建预览）
    /// </summary>
    /// <param name="source">Cfg中的NPC配置（已有NPC）或新建Bean</param>
    /// <param name="isNew">是否新建项</param>
    /// <param name="nameCn">中文名（已有NPC取语言表，新建取输入框）</param>
    private void LoadNpcToEditor(NpcInfoBean source, bool isNew, string nameCn)
    {
        editingNpcInfo = isNew ? source : DeepCopyNpc(source);
        editingNameCn = nameCn ?? "";
        isNewEntry = isNew;
        InitAppearanceStateFromEditing();
        CommitSnapshot();
        CloseSelect();
        RebuildPreview();
    }
    #endregion

    #region 新建
    /// <summary>
    /// 绘制栏底新建/删除按钮
    /// </summary>
    private void DrawListBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isCreatingNew ? "收起新建" : "＋ 新建NPC"))
        {
            isCreatingNew = !isCreatingNew;
            if (isCreatingNew && inputNewId.IsNull())
                inputNewId = $"{GetSuggestNewNpcId()}";
        }
        bool canDelete = editingNpcInfo != null && !isNewEntry && !deletedNpcIds.Contains(editingNpcInfo.id);
        GUI.enabled = canDelete;
        if (GUILayout.Button("－ 删除选中"))
            RegisterDeleteSelected();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制新建面板（id输入+查重/建议值 + 模板复制 + 中文名 + 确认/取消）
    /// </summary>
    private void DrawCreateNewPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("新建 NPC", sectionStyle);

        //id输入 + 建议值按钮
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID", GUILayout.Width(20));
        inputNewId = EditorGUILayout.TextField(inputNewId);
        if (GUILayout.Button("建议值", EditorStyles.miniButton, GUILayout.Width(50)))
            inputNewId = $"{GetSuggestNewNpcId()}";
        EditorGUILayout.EndHorizontal();

        //id合法性实时校验
        string idError = ValidateNewId(inputNewId, out long newId);
        if (!idError.IsNull())
            EditorGUILayout.HelpBox(idError, MessageType.Error);

        //模板复制下拉（0=空白，其余为已有NPC）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("模板", GUILayout.Width(34));
        string[] templateLabels = GetTemplateLabels();
        newTemplateIndex = Mathf.Clamp(newTemplateIndex, 0, templateLabels.Length - 1);
        newTemplateIndex = EditorGUILayout.Popup(newTemplateIndex, templateLabels);
        EditorGUILayout.EndHorizontal();

        //中文名输入
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("名字", GUILayout.Width(34));
        inputNewName = EditorGUILayout.TextField(inputNewName);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("其他语种请到语言表补录（textId=NPC id）", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = idError.IsNull();
        if (GUILayout.Button("确认创建"))
            CreateNewNpc(newId);
        GUI.enabled = true;
        if (GUILayout.Button("取消"))
            isCreatingNew = false;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 校验新建id（空/非数字/≤0/与存量冲突），合法返回 null 并输出 id
    /// </summary>
    private string ValidateNewId(string input, out long newId)
    {
        newId = 0;
        if (input.IsNull() || !long.TryParse(input.Trim(), out newId))
            return "请输入合法的 long 型 id";
        if (newId <= 0)
            return "id 必须大于 0";
        //lambda 不能捕获 out 参数（CS1628），先落到局部变量再查重
        long parsedId = newId;
        if (listAllNpc.Exists(item => item.id == parsedId))
            return $"id {newId} 已存在";
        return null;
    }

    /// <summary>
    /// 取建议的新建id（当前最大id+1）
    /// </summary>
    private long GetSuggestNewNpcId()
    {
        long maxId = 0;
        foreach (var item in listAllNpc)
            if (item.id > maxId)
                maxId = item.id;
        return maxId + 1;
    }

    /// <summary>
    /// 构建模板下拉显示名（首项=空白模板，其余 id+中文名）
    /// </summary>
    private string[] GetTemplateLabels()
    {
        var labels = new List<string> { "(空白模板)" };
        foreach (var item in listAllNpc)
            labels.Add($"{item.id}  {GetNpcNameCn(item)}");
        return labels.ToArray();
    }

    /// <summary>
    /// 创建新NPC（空白或按模板深拷贝；name 恒等于 id 遵循 textId 约定；进入编辑并重建预览）
    /// </summary>
    private void CreateNewNpc(long newId)
    {
        if (!ConfirmDiscardIfDirty())
            return;
        NpcInfoBean newBean;
        if (newTemplateIndex > 0 && newTemplateIndex - 1 < listAllNpc.Count)
        {
            newBean = DeepCopyNpc(listAllNpc[newTemplateIndex - 1]);
        }
        else
        {
            newBean = new NpcInfoBean();
            //字符串字段初始化空串，避免 null 影响序列化比对与 Excel 写回
            newBean.skin_data = "";
            newBean.equip_item_ids = "";
            newBean.equip_random = "";
            newBean.title_data = "";
            newBean.body_size = "";
            newBean.attack_mode_ext = "";
            newBean.skin_color_data = "";
            newBean.region = "";
            newBean.icon_res = "";
            newBean.remark = "";
        }
        newBean.id = newId;
        newBean.name = newId;
        isCreatingNew = false;
        string newName = inputNewName.Trim();
        inputNewId = "";
        inputNewName = "";
        newTemplateIndex = 0;
        LoadNpcToEditor(newBean, true, newName);
    }
    #endregion

    #region 删除
    /// <summary>
    /// 登记删除当前选中的NPC（确认后仅登记，保存时才真正删行；编辑中的修改一并放弃）
    /// </summary>
    private void RegisterDeleteSelected()
    {
        if (editingNpcInfo == null || isNewEntry)
            return;
        long targetId = editingNpcInfo.id;
        if (!EditorUtility.DisplayDialog(
                "删除NPC",
                $"确定删除 NPC [{targetId}  {GetNpcNameCn(editingNpcInfo)}] 吗？\n删除登记后需点「保存」才真正生效（同时删除语言表名字行）。",
                "登记删除", "取消"))
            return;
        deletedNpcIds.Add(targetId);
        //被删项退出编辑（若有未保存修改视为放弃）
        editingNpcInfo = null;
        editingNameCn = "";
        CloseSelect();
        RebuildPreview();
    }
    #endregion
}
