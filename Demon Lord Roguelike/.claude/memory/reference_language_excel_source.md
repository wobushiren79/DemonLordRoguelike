---
name: reference_language_excel_source
description: 多语言 Language_*.txt 的真实源是 excel_language 工作簿的同名工作表，改文本必须改 Excel 否则导出被覆盖
metadata:
  type: reference
---

`Assets/Resources/JsonText/Language_{CfgName}_{cn,en}.txt` 是**派生产物**，真实源是 `Assets/Data/Excel/excel_language[多语言_FrameWork].xlsx` 里**与 `{CfgName}` 同名的工作表**（如成就 = `AchievementInfo` 工作表）。

- 工作表列：`id / content_cn / content_en / content_1_cn / content_1_en / remark`。
- `content`(index0)=名字/主文本，`content_1`(index1)=详情/描述；一个 id 一行同时给两种语言的两条文本。
- `Language_UIText_*` 例外：来自 `excel_ui_text[UI文本_FrameWork].xlsx`。
- 每个配置(BuffInfo/ItemsInfo/AbyssalBlessingInfo/...)的多语言都在 `excel_language` 的同名工作表里，不在各自的 `excel_xxx_info` 表内。

**坑**：只改 `.txt` 会在下次 ExcelEditorWindow 导出时被覆盖丢失。修改文本**必须改 `excel_language` 对应工作表**（用 openpyxl，见 [[reference_unityskills_shadergraph_limits]] 同类 Excel 读写规范），再导出 `.txt`；可同时手动写 `.txt` 让运行期即时生效。与 CLAUDE.md「配置表数据修改：Excel 是唯一真实源」一致。相关：成就描述用 `name`/`details` 双列同指一个文本id + `{Name}` 占位符模板（见 achievement-system / localization-system skill 的 GetTextReplace）。
