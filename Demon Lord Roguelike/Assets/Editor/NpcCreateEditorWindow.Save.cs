using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NpcCreateEditorWindow 保存闭环（partial）：校验 → 变更摘要确认 → Excel 占用探测 →
/// EPPlus 单会话写回业务表（删行→修改→新增）与语言表（content_cn）→ ExcelToJsonItem 重导 JSON → 清 Cfg 缓存 → 重载列表。
/// 写回模式参照 StoryEditorWindow.SaveAll/SaveLanguageFile；Excel 为唯一真实源，窗口不直写 JSON。
/// </summary>
public partial class NpcCreateEditorWindow : EditorWindow
{
    #region 保存入口
    /// <summary>
    /// 保存全部变更（校验失败的错误阻断保存；返回 true=保存成功或无变更，供脏数据三选判断是否可以继续后续操作）
    /// </summary>
    private bool SaveAll()
    {
        if (!HasAnyChange())
            return true;
        var errors = new List<string>();
        var warnings = new List<string>();
        ValidateAll(errors, warnings);
        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("保存失败", $"存在以下错误，已阻断保存：\n{string.Join("\n", errors)}", "确定");
            return false;
        }
        //变更摘要确认
        string summary = BuildSaveSummary();
        if (warnings.Count > 0)
            summary += $"\n\n警告（不影响保存）：\n{string.Join("\n", warnings)}";
        if (!EditorUtility.DisplayDialog("保存NPC配置", summary, "保存", "取消"))
            return false;
        //Excel 占用探测（ExcelToJsonItem 对占用文件只 LogError 静默跳过，不探测会出现「保存成功但 JSON 没更新」的隐性事故）
        if (!CheckExcelWritable(ExcelPathNpc) || !CheckExcelWritable(ExcelPathLanguage))
            return false;

        try
        {
            WriteNpcExcel();
            WriteLanguageExcel();
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"NPC创建编辑器保存失败: {ex}");
            EditorUtility.DisplayDialog("保存失败", $"写回 Excel 出错：\n{ex.Message}\n\n请确认 Excel 文件未被占用。", "确定");
            return false;
        }
        //重导运行时 JSON（ExcelToJsonItem 内部自带 AssetDatabase.Refresh）
        ExcelUtil.ExcelToJsonItem(ExcelPathNpc);
        ExcelUtil.ExcelToJsonItem(ExcelPathLanguage);

        //状态收口：记住当前编辑id，重载后恢复选中
        long savedId = editingNpcInfo != null ? editingNpcInfo.id : 0;
        isNewEntry = false;
        deletedNpcIds.Clear();
        ReloadAllCfg();
        if (savedId != 0)
        {
            var savedInfo = listAllNpc.Find(item => item.id == savedId);
            if (savedInfo != null)
                LoadNpcToEditor(savedInfo, false, GetNpcNameCnRaw(savedInfo));
        }
        else
        {
            RebuildPreview();
        }
        LogUtil.Log($"NPC创建编辑器保存完成 id:{savedId}");
        return true;
    }

    /// <summary>
    /// 构建变更摘要文本（修改/新增/删除条数 + 明细）
    /// </summary>
    private string BuildSaveSummary()
    {
        var lines = new List<string>();
        if (editingNpcInfo != null && IsEditingDirty())
            lines.Add(isNewEntry
                ? $"新增 NPC：[{editingNpcInfo.id}]  {editingNameCn}"
                : $"修改 NPC：[{editingNpcInfo.id}]  {editingNameCn}");
        if (deletedNpcIds.Count > 0)
        {
            lines.Add($"删除 NPC × {deletedNpcIds.Count}：");
            foreach (long deletedId in deletedNpcIds)
            {
                var deletedInfo = listAllNpc.Find(item => item.id == deletedId);
                lines.Add($"　[{deletedId}]  {(deletedInfo != null ? GetNpcNameCn(deletedInfo) : "")}");
            }
        }
        return $"将写回 excel_npc_info 与语言表（NpcInfo sheet）：\n\n{string.Join("\n", lines)}";
    }

    /// <summary>
    /// 探测 Excel 文件是否可写（被占用时弹提示并返回 false）
    /// </summary>
    private bool CheckExcelWritable(string excelPath)
    {
        try
        {
            using (File.Open(excelPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            return true;
        }
        catch
        {
            EditorUtility.DisplayDialog("文件被占用", $"请先关闭 Excel 文件再保存：\n{excelPath}", "确定");
            return false;
        }
    }
    #endregion

    #region 保存前校验
    /// <summary>
    /// 保存前校验（错误阻断，警告可继续）
    /// </summary>
    private void ValidateAll(List<string> errors, List<string> warnings)
    {
        if (editingNpcInfo != null && IsEditingDirty())
            ValidateEditingNpc(errors, warnings);
        foreach (long deletedId in deletedNpcIds)
        {
            if (!listAllNpc.Exists(item => item.id == deletedId))
                errors.Add($"删除的 NPC [{deletedId}] 在配置表中不存在");
        }
    }

    /// <summary>
    /// 校验当前编辑副本
    /// </summary>
    private void ValidateEditingNpc(List<string> errors, List<string> warnings)
    {
        var npcInfo = editingNpcInfo;
        long npcId = npcInfo.id;
        if (npcId <= 0)
            errors.Add($"NPC id [{npcId}] 非法（必须大于0）");
        //新建 id 与存量冲突（保存前再兜底查一次）
        if (isNewEntry && listAllNpc.Exists(item => item.id == npcId))
            errors.Add($"新建 id [{npcId}] 与存量 NPC 冲突");
        //生物存在性
        if (npcInfo.creature_id != 0 && CreatureInfoCfg.GetItemData(npcInfo.creature_id) == null)
            errors.Add($"生物 creature_id [{npcInfo.creature_id}] 在 CreatureInfo 表中不存在");
        //随机皮肤池存在性与类型
        if (npcInfo.creature_random_id != 0)
        {
            var randomInfo = CreatureRandomInfoCfg.GetItemData(npcInfo.creature_random_id);
            if (randomInfo == null)
                errors.Add($"随机皮肤池 [{npcInfo.creature_random_id}] 在 CreatureRandomInfo 表中不存在");
            else if (randomInfo.GetRandomType() != CreatureRandomTypeEnum.Skin)
                errors.Add($"随机皮肤池 [{npcInfo.creature_random_id}] 不是皮肤池（random_type=0）");
        }
        //随机装备池存在性与类型
        long poolId = npcInfo.GetEquipRandomPoolId();
        if (poolId != 0)
        {
            var poolInfo = CreatureRandomInfoCfg.GetItemData(poolId);
            if (poolInfo == null)
                errors.Add($"随机装备池 [{poolId}] 在 CreatureRandomInfo 表中不存在");
            else if (poolInfo.GetRandomType() == CreatureRandomTypeEnum.Skin)
                errors.Add($"随机装备池 [{poolId}] 不是装备池/套装池（random_type=1/2）");
        }
        //固定装备存在性与槽位支持
        var creatureInfo = npcInfo.creature_id != 0 ? CreatureInfoCfg.GetItemData(npcInfo.creature_id) : null;
        var listEquipType = creatureInfo != null ? creatureInfo.GetEquipItemsType() : null;
        foreach (long itemId in listCreatureEquipItemIds)
        {
            var itemInfo = ItemsInfoCfg.GetItemData(itemId);
            if (itemInfo == null)
            {
                errors.Add($"固定装备 [{itemId}] 在 ItemsInfo 表中不存在");
                continue;
            }
            if (listEquipType != null && !listEquipType.Contains(itemInfo.GetItemType()))
                errors.Add($"固定装备 [{itemId}] 的类型 {itemInfo.GetItemType()} 不被生物 [{npcInfo.creature_id}] 支持");
        }
        //固定皮肤存在性
        foreach (long skinId in listCreatureSkinData)
            if (CreatureModelInfoCfg.GetItemData(skinId) == null)
                errors.Add($"固定皮肤 [{skinId}] 在 CreatureModelInfo 表中不存在");
        //额外技能存在性
        if (!npcInfo.attack_mode_ext.IsNull())
        {
            foreach (long extId in npcInfo.attack_mode_ext.SplitForListLong(','))
                if (AttackModeExtInfoCfg.GetItemData(extId) == null)
                    errors.Add($"额外技能 [{extId}] 在 AttackModeExtInfo 表中不存在");
        }
        //体型解析校验
        if (!npcInfo.body_size.IsNull())
        {
            string sizeStr = npcInfo.body_size.Trim();
            bool isValidSize = sizeStr == "0";
            if (!isValidSize && sizeStr.Contains(","))
            {
                var segments = sizeStr.Split(',');
                isValidSize = segments.Length >= 2
                    && float.TryParse(segments[0].Trim(), out _)
                    && float.TryParse(segments[1].Trim(), out _);
            }
            if (!isValidSize)
                isValidSize = float.TryParse(sizeStr, out _);
            if (!isValidSize)
                errors.Add($"体型 body_size [{npcInfo.body_size}] 解析失败（空=1倍 / \"0.9,1.1\"区间 / \"1.1\"固定）");
        }
        //警告：中文名为空
        if (editingNameCn.IsNull() && npcInfo.GetNpcType() != NpcTypeEnum.CouncilorRandom)
            warnings.Add($"NPC [{npcId}] 中文名为空（随机议员用评级称谓名可无视）");
        //警告：议会NPC未配评级
        if ((npcInfo.GetNpcType() == NpcTypeEnum.Councilor || npcInfo.GetNpcType() == NpcTypeEnum.CouncilorRandom)
            && npcInfo.councilor_ratings == 0)
            warnings.Add($"NPC [{npcId}] 是议会类型但未配议会评级（默认按1）");
        //警告：战斗NPC未配属性
        if (npcInfo.npc_type == 1 && npcInfo.HP <= 0 && npcInfo.ATK <= 0)
            warnings.Add($"NPC [{npcId}] 是战斗类型但 HP/ATK 均未配置");
        //警告：无实体也未配头像
        if (npcInfo.creature_id == 0 && npcInfo.icon_res.IsNull())
            warnings.Add($"NPC [{npcId}] 无实体（creature_id=0）且未配头像 icon_res，对话界面将没有形象");
    }
    #endregion

    #region Excel 写回
    /// <summary>
    /// 写回 NPC 业务表：先删行（行号降序防漂移）再写当前编辑行（不存在则追加新行）
    /// </summary>
    private void WriteNpcExcel()
    {
        using (var pack = new ExcelPackage(new FileInfo(ExcelPathNpc)))
        {
            var sheet = pack.Workbook.Worksheets[SheetNpc];
            if (sheet == null)
                throw new Exception($"表 {Path.GetFileName(ExcelPathNpc)} 中找不到工作表 {SheetNpc}");
            var colMap = BuildColMap(sheet);
            DeleteRowsByIds(sheet, deletedNpcIds);
            if (editingNpcInfo != null && IsEditingDirty())
                WriteNpcRow(sheet, colMap, editingNpcInfo);
            pack.Save();
        }
    }

    /// <summary>
    /// 写入单个 NPC 行（全 25 列；name[language] 列恒写 id，遵循 textId==NPC id 约定）
    /// </summary>
    private void WriteNpcRow(ExcelWorksheet sheet, Dictionary<string, int> colMap, NpcInfoBean npcInfo)
    {
        //保存前把调色字典按固定皮肤过滤序列化（既定过滤规则）
        WriteBackSkinColorData();
        int row = FindRowById(sheet, npcInfo.id);
        if (row <= 0)
            row = sheet.Dimension.End.Row + 1;
        SetCellLong(sheet, row, colMap, "id", npcInfo.id);
        SetCellLong(sheet, row, colMap, "creature_id", npcInfo.creature_id);
        SetCellLong(sheet, row, colMap, "npc_type", npcInfo.npc_type);
        SetCellLong(sheet, row, colMap, "level", npcInfo.level);
        SetCellFloat(sheet, row, colMap, "HP", npcInfo.HP);
        SetCellFloat(sheet, row, colMap, "MP", npcInfo.MP);
        SetCellFloat(sheet, row, colMap, "DR", npcInfo.DR);
        SetCellFloat(sheet, row, colMap, "ATK", npcInfo.ATK);
        SetCellFloat(sheet, row, colMap, "ASPD", npcInfo.ASPD);
        SetCellFloat(sheet, row, colMap, "MSPD", npcInfo.MSPD);
        SetCellFloat(sheet, row, colMap, "attack_search_range", npcInfo.attack_search_range);
        SetCellText(sheet, row, colMap, "skin_data", npcInfo.skin_data);
        SetCellLong(sheet, row, colMap, "creature_random_id", npcInfo.creature_random_id);
        SetCellText(sheet, row, colMap, "equip_item_ids", npcInfo.equip_item_ids);
        SetCellText(sheet, row, colMap, "equip_random", npcInfo.equip_random);
        SetCellLong(sheet, row, colMap, "name[language]", npcInfo.id);
        SetCellLong(sheet, row, colMap, "councilor_ratings", npcInfo.councilor_ratings);
        SetCellText(sheet, row, colMap, "title_data", npcInfo.title_data);
        SetCellText(sheet, row, colMap, "body_size", npcInfo.body_size);
        SetCellText(sheet, row, colMap, "attack_mode_ext", npcInfo.attack_mode_ext);
        SetCellText(sheet, row, colMap, "skin_color_data", npcInfo.skin_color_data);
        SetCellText(sheet, row, colMap, "region", npcInfo.region);
        SetCellLong(sheet, row, colMap, "rarity", npcInfo.rarity);
        SetCellText(sheet, row, colMap, "icon_res", npcInfo.icon_res);
        SetCellText(sheet, row, colMap, "remark", npcInfo.remark);
    }

    /// <summary>
    /// 写回语言表：删除登记的名字行 + 当前编辑项的中文名（其他语种类留空待补录，Excel 里已有的其它语种内容保持不变）
    /// </summary>
    private void WriteLanguageExcel()
    {
        using (var pack = new ExcelPackage(new FileInfo(ExcelPathLanguage)))
        {
            var sheet = pack.Workbook.Worksheets[SheetNpc];
            if (sheet == null)
                throw new Exception($"语言表中找不到工作表 {SheetNpc}");
            var colMap = BuildColMap(sheet);
            DeleteRowsByIds(sheet, deletedNpcIds);
            if (editingNpcInfo != null && IsEditingDirty())
            {
                int row = FindRowById(sheet, editingNpcInfo.id);
                if (row <= 0)
                    row = sheet.Dimension.End.Row + 1;
                SetCellLong(sheet, row, colMap, "id", editingNpcInfo.id);
                SetCellText(sheet, row, colMap, "content_cn", editingNameCn);
            }
            pack.Save();
        }
    }
    #endregion

    #region Excel 单元格辅助
    /// <summary>
    /// 构建 表头名→列号 映射
    /// </summary>
    private Dictionary<string, int> BuildColMap(ExcelWorksheet sheet)
    {
        var colMap = new Dictionary<string, int>();
        for (int col = 1; col <= sheet.Dimension.End.Column; col++)
        {
            string header = sheet.Cells[1, col].Text;
            if (!string.IsNullOrEmpty(header) && !colMap.ContainsKey(header))
                colMap.Add(header, col);
        }
        return colMap;
    }

    /// <summary>
    /// 按 id 定位数据行（A列，第4行起；未找到返回-1）
    /// </summary>
    private int FindRowById(ExcelWorksheet sheet, long id)
    {
        for (int row = 4; row <= sheet.Dimension.End.Row; row++)
        {
            if (long.TryParse(sheet.Cells[row, 1].Text, out long cellId) && cellId == id)
                return row;
        }
        return -1;
    }

    /// <summary>
    /// 按 id 集合删除数据行（行号降序删防漂移）
    /// </summary>
    private void DeleteRowsByIds(ExcelWorksheet sheet, HashSet<long> ids)
    {
        if (ids.Count == 0)
            return;
        var rows = new List<int>();
        foreach (long id in ids)
        {
            int row = FindRowById(sheet, id);
            if (row > 0)
                rows.Add(row);
        }
        rows.Sort((a, b) => b.CompareTo(a));
        foreach (int row in rows)
            sheet.DeleteRow(row);
    }

    /// <summary>
    /// 按表头写单元格（long 写数值，保持列类型）
    /// </summary>
    private void SetCellLong(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, long value)
    {
        if (colMap.TryGetValue(header, out int col))
            sheet.Cells[row, col].Value = value;
    }

    /// <summary>
    /// 按表头写单元格（float 写数值，保持列类型）
    /// </summary>
    private void SetCellFloat(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, float value)
    {
        if (colMap.TryGetValue(header, out int col))
            sheet.Cells[row, col].Value = value;
    }

    /// <summary>
    /// 按表头写单元格文本（null 归一为空串）
    /// </summary>
    private void SetCellText(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, string value)
    {
        if (colMap.TryGetValue(header, out int col))
            sheet.Cells[row, col].Value = value ?? "";
    }
    #endregion
}
