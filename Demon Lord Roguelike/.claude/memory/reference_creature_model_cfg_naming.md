---
name: reference_creature_model_cfg_naming
description: CreatureModelCfg(尺寸表)与CreatureModelInfoCfg(部件资源表)名字相近极易混，清缓存/读配置时用错类型会静默失效
metadata:
  type: reference
---

# CreatureModelCfg vs CreatureModelInfoCfg 命名陷阱

项目里有两张名字几乎一样的「生物模型」配置表，Bean/Cfg 完全不同：

| 用途 | Bean | Cfg | fileName(JsonText) | 关键字段 |
|---|---|---|---|---|
| **模型尺寸/展示表**（每个生物模型一行的主表） | `CreatureModelBean` | `CreatureModelCfg` | `CreatureModel` | `size_spine`、`ui_data_s`（小卡图标缩放;坐标）、`ui_data_b`（大卡）、`res_name`、`unlock_id`、`ui_show_spine` |
| **模型部件资源表**（捏人部件：头/发型/眼睛等） | `CreatureModelInfoBean` | `CreatureModelInfoCfg` | `CreatureModelInfo` | `model_id`、`part_type`、`color_state` |

- `creatureData.creatureModel` 是 `CreatureModelBean`，读 `CreatureModelCfg`。
- 典型事故（2026-10-03 卡片编辑器尺寸校准保存后显示"还原"）：`SetExcelData`+`ExcelToJsonItem` 写盘成功，但 `ClearCfgCache(typeof(CreatureModelInfoCfg))` 清错缓存，`CreatureModelCfg` 旧缓存继续供数 → 刷新读回旧值。修复=清 `CreatureModelCfg`。
- **排查「配置改了但不生效」时**：先确认操作/清缓存的 Cfg 类型与 fileName 对应的是不是目标表，再怀疑写盘链路（可参考 [[reference_epplus_stream_write_noop]]、[[reference_unity_mcp_execute_code_encoding]] 的 GetExcelPackage 静默吞异常）。
