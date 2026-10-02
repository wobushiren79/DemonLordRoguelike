---
name: reference_mod_excel_source
description: Mod 道具配置（幻化药）的唯一真实源是 Mod 项目的 excel_mod_items_info_*，改数据必须 生成脚本常量/Mod Excel/主项目 JsonText 三处同步，只改产物会被 export 回退
metadata:
  type: reference
---

Mod 幻化药（及一切 Mod 道具）配置的数据流是**两段式流水线，Excel 为唯一真实源**（与主项目配置表惯例一致）：

```
gen_<mod>_spine_mod.py scan   扫 Mod 项目 spine 资源 → 重建 Mod 项目道具 Excel（ICON_RES 等默认值由脚本常量写入）
gen_<mod>_spine_mod.py export 读 Excel → 导出主项目 Mods/<Mod>/JsonText/（ItemsInfo.txt + Language_ItemsInfo_*.txt ×12）
```

**真实源位置**：`<MOD项目根>/Assets/Data/Excel/excel_mod_items_info_<mod名小写>[Mod道具信息-<Mod名>].xlsx`
- `<MOD项目根>` 是主项目仓库**之外**的独立 Unity 工程，不写死路径：脚本经 `--mod-project` 参数传入（与主项目平级的 DemonLordRoguelikeMod 目录；文件名约定见 `TestTransformPotionGUI.GetModItemsExcelRelPath`）
- **AeonsEchoSpine 例外**：用无后缀的基础文件名 `excel_mod_items_info[Mod道具信息].xlsx`，其余 11 个 Mod 各带小写 mod 名后缀
- 主项目 `Mods/` 目录下**只有部署产物（JsonText），没有任何 xlsx**——在主项目仓库里搜 Mod 的 Excel 是搜不到的

**改 Mod 道具数据必须三处同步**，缺一不可：
1. 生成脚本常量（主项目 `.claude/scripts/gen_*_spine_mod.py`，如 ICON_RES）——管下次 scan 重建
2. Mod 项目 Excel（真实源）——管下次 export
3. 主项目 `Mods/<Mod>/JsonText/` 产物——管立即生效

**Why**：2026-10-02 改幻化药图标 `Item_TransformPotion_1`→`Item_Potion_1` 时只改了脚本常量 + 主项目 JsonText，遗漏 Mod 项目 12 个 Excel（6867 行），被用户指正——只改产物，下次任何人跑 export 即从 Excel 重新导出旧值，改动静默回退。

**How to apply**：涉及 Mod 道具字段改动时，先确认数据流位置；Excel 修改按 CLAUDE.md Excel 规则备份（脚本自带 backup 到 `<MOD项目根>/ExcelBackup/`，Assets 之外不被导出工具扫描）+ 改后读回验证 + 删临时备份；多语言在 `excel_mod_language_*` 同理。相关：[[reference_language_excel_source]]（主项目多语言真实源同理）。
