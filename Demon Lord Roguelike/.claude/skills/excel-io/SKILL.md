---
name: excel-io
description: 使用 openpyxl 读取和写入 Excel (.xlsx) 配置表。当需要直接读取或修改 Assets/Data/Excel/ 下的配置表数据时使用此 SKILL。
tools: Read, Write, Edit, Glob, Grep, Bash
---

# Excel 读写操作指南 (openpyxl)

## 核心规则

- **唯一允许的库**：`openpyxl`，禁止使用 `xlrd`、`xlwt`、`xlwings`、`pandas.read_excel`
- **编码**：UTF-8（openpyxl 默认行为，无需额外指定）
- **脚本存放位置**：`.claude/scripts/`
- **写入前必须确认**：修改配置表前先读取确认内容，写入后告知用户已变更的行列

## 配置表目录

```
Assets/Data/Excel/                   # 原始 Excel 配置表（31张）
Assets/Resources/JsonText/           # 导出的 JSON 文本（由编辑器工具生成）
```

## 配置表速查（文件名 → Sheet名）

| 中文名 | 文件名（精简） | Sheet名 | 数据行 |
|--------|---------------|---------|--------|
| 深渊馈赠 | excel_abyssal_blessing_info | AbyssalBlessingInfo | 6 |
| 攻击方式 | excel_attackmode_info | AttackModeInfo | 31 |
| 音频信息 | excel_audio_info | AudioInfo | 39 |
| 基础信息 | excel_base_info | BaseInfo | 3 |
| Buff信息 | excel_buff_info | BuffInfo | 49 |
| Buff前置 | excel_buff_pre_info | BuffPreInfo | 6 |
| 议员对话 | excel_conversation_councilor_info | ConversationCouncilorInfo | 33 |
| 生物属性类型 | excel_creature_attribute_type_info | CreatureAttributeTypeInfo | 12 |
| 生物信息 | excel_creature_info | CreatureInfo | 110 |
| 生物模型 | excel_creature_model | CreatureModel | 66 |
| 生物模型详情 | excel_creature_model_info | CreatureModelInfo | 438 |
| 生物随机 | excel_creature_random_info | CreatureRandomInfo | 16 |
| 终焉议会 | excel_doom_council_info | DoomCouncilInfo | 13 |
| 议会议员等级 | excel_doom_council_ratings_info | DoomCouncilRatingsInfo | 12 |
| 粒子效果 | excel_effect_info | EffectInfo | 14 |
| 战斗场景 | excel_fight_scene | FightScene | 4 |
| 战斗-征服 | excel_fight_type_conquer_info | FightTypeConquerInfo | 12 |
| 游戏世界 | excel_game_world_info | GameWorldInfo | 6 |
| 道具信息 | excel_items_info | ItemsInfo | 228 |
| 道具类型 | excel_items_type | ItemsType | 10 |
| 多语言 | excel_language | UIText(+17子表) | 152+ |
| 等级信息 | excel_level_info | LevelInfo | 12 |
| NPC信息 | excel_npc_info | NpcInfo | 37 |
| NPC关系 | excel_npc_relationship_info | NpcRelationshipInfo | 7 |
| 稀有度 | excel_rarity_info | RarityInfo | 8 |
| 研究信息 | excel_research_info | ResearchInfo | 83 |
| 骨骼动画枚举 | excel_spine_animation_state | SpineAnimationState | 33 |
| 扭蛋机 | excel_store_gashaponmachine_info | StoreGashaponMachineInfo | 23 |
| 称号 | excel_title_info | TitleInfo | 15 |
| UI文本 | excel_ui_text | UIText | 152 |
| 解锁信息 | excel_unlock_info | UnlockInfo | 108 |

## 通用工具脚本

项目提供了两个通用脚本：

- `.claude/scripts/excel_read.py`  — 读取 Excel 指定 Sheet 的数据
- `.claude/scripts/excel_write.py` — 写入/修改 Excel 单元格数据

## 读取 Excel

### 方式一：调用通用脚本

```bash
python .claude/scripts/excel_read.py \
  --path "Assets/Data/Excel/excel_creature_info[生物信息].xlsx" \
  --sheet "Sheet1" \
  --rows 10
```

参数说明：
- `--path`  Excel 文件路径（相对于项目根目录）
- `--sheet` Sheet 名称（默认读取第一个 Sheet）
- `--rows`  读取前 N 行（默认全部）
- `--col`   只输出指定列（可多次使用，如 `--col id --col name`）

### 方式二：内联 Python

```python
import openpyxl

wb = openpyxl.load_workbook("Assets/Data/Excel/excel_creature_info[生物信息].xlsx", read_only=True)
ws = wb.active  # 或 wb["Sheet1"]

# 读取表头（第1行）
headers = [cell.value for cell in next(ws.iter_rows(min_row=1, max_row=1))]
print("表头:", headers)

# 遍历数据行（从第2行开始，跳过表头）
for row in ws.iter_rows(min_row=2, values_only=True):
    if row[0] is None:
        continue  # 跳过空行
    print(row)

wb.close()
```

## 写入 Excel

### 方式一：调用通用脚本

```bash
python .claude/scripts/excel_write.py \
  --path "Assets/Data/Excel/excel_creature_info[生物信息].xlsx" \
  --sheet "Sheet1" \
  --row 5 \
  --col 3 \
  --value "新值"
```

参数说明：
- `--row` / `--col` 使用 1-based 行列索引
- `--value` 写入的值（字符串/数字自动转换）
- `--backup` 写入前自动备份（推荐）

### 方式二：内联 Python

```python
import openpyxl
import shutil

path = "Assets/Data/Excel/excel_creature_info[生物信息].xlsx"

# 写入前备份
shutil.copy2(path, path + ".bak")

wb = openpyxl.load_workbook(path)
ws = wb.active

# 修改单元格（1-based 索引）
ws.cell(row=5, column=3).value = "新值"

wb.save(path)
wb.close()
print(f"已保存: {path}")
```

## 批量修改示例

```python
import openpyxl

path = "Assets/Data/Excel/excel_creature_info[生物信息].xlsx"
wb = openpyxl.load_workbook(path)
ws = wb.active

# 读取表头，建立列名->索引映射
headers = {cell.value: cell.column for cell in ws[1]}

# 按列名修改
for row in ws.iter_rows(min_row=2):
    id_val = row[headers["id"] - 1].value
    if id_val == 1001:
        row[headers["hp"] - 1].value = 500
        break

wb.save(path)
wb.close()
```

## 注意事项

1. **配置表修改后需重新导出 JSON**：通过 Unity 编辑器菜单 `Custom/工具弹窗/Excel编辑器` 重新导出
2. **read_only=True**：只读场景下使用，性能更好，但不能修改
3. **空行处理**：Excel 表中可能存在空行，遍历时检查 `row[0] is None`
4. **文件名含中文**：Windows 路径直接传入即可，openpyxl 处理 UTF-8 路径无问题
5. **不要修改 Bean 文件**：`*InfoBean.cs` 和 `*Bean.cs` 是自动生成的，修改 Excel 后由编辑器工具重新生成
