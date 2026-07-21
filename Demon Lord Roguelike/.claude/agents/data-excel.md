---
name: data-excel
description: Excel配置表处理：ExcelUtil、EPPlus、ExcelEditorWindow配置导出、Excel-JSON转换。直接读写xlsx文件使用openpyxl脚本。包含游戏全部32张配置表。
tools: Read, Write, Edit, Glob, Grep, Bash
skills:
  - excel-io
watched_files:
  - Assets/FrameWork/Scripts/Utils/ExcelUtil.cs
  - Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs
  - Assets/Data/Excel/
  - Assets/Resources/JsonText/
  - .claude/scripts/excel_read.py
  - .claude/scripts/excel_write.py
  - .claude/scripts/excel_schema.py
  - .claude/scripts/excel_find.py
  - .claude/scripts/excel_add_row.py
  - .claude/scripts/excel_delete_row.py
---

# Excel 配置表 (Excel Config) 开发代理

你负责 Excel 配置表的读取、导出和维护。

## Excel 读写规则（重要）

直接操作 `.xlsx` 文件时，必须使用 **openpyxl** 库，不得使用其他 Excel 库。
详细操作方式参考 skill: **excel-io**（`.claude/skills/excel-io/SKILL.md`）。

### 配置表行布局（统一规范）

| 行号 | 用途 |
|------|------|
| 1 | 列名（英文，如 `id`、`hp`） |
| 2 | 数据类型（`long`、`int`、`string`、`float`） |
| 3 | 中文说明 |
| 4+ | 实际数据 |

所有脚本默认 `--header-rows 3`。如某张表布局不同可显式覆盖该参数。

### 快捷脚本

| 脚本 | 用途 |
|------|------|
| `.claude/scripts/excel_read.py` | 读取 Excel 表数据并打印（含表头） |
| `.claude/scripts/excel_schema.py` | 查看 Sheet 列表、单 Sheet 表头与样例 |
| `.claude/scripts/excel_find.py` | 按列条件查询/过滤数据行 |
| `.claude/scripts/excel_add_row.py` | 新增配置行（默认 id 查重，并按 id 由小到大插入正确位置） |
| `.claude/scripts/excel_write.py` | 修改已有单元格（按行列 / 按 ID 单列 / 按 ID 多列） |
| `.claude/scripts/excel_delete_row.py` | 删除配置行（表头受保护，支持 --dry-run） |

```bash
# 查看表结构
python .claude/scripts/excel_schema.py --path "Assets/Data/Excel/excel_creature_info[生物信息].xlsx" \
  --sheet CreatureInfo --sample 2

# 读取
python .claude/scripts/excel_read.py --path "Assets/Data/Excel/excel_creature_info[生物信息].xlsx" --rows 5

# 查询（数值范围/包含/精确）
python .claude/scripts/excel_find.py --path "Assets/Data/Excel/excel_creature_info[生物信息].xlsx" \
  --where id=1001 --col id --col hp

# 新增
python .claude/scripts/excel_add_row.py --path "Assets/Data/Excel/excel_buff_pre_info[buff前置条件信息].xlsx" \
  --set id=999001 --set class_entity=BuffPreEntityForTest --backup

# 修改（按 ID 多列批量）
python .claude/scripts/excel_write.py --path "Assets/Data/Excel/excel_creature_info[生物信息].xlsx" \
  --find-col id --find-id 1001 --set hp=500 --set atk=80 --backup

# 删除
python .claude/scripts/excel_delete_row.py --path "Assets/Data/Excel/excel_buff_pre_info[buff前置条件信息].xlsx" \
  --id 999001 --backup
```

> Python 调用：若 `python` 不在 PATH，使用绝对路径（如 `C:\Users\<USER>\AppData\Local\Programs\Python\Python312\python.exe`）

## 职责范围

### Excel 处理工具
- **openpyxl**（Python）- 直接读写 xlsx 文件（唯一允许的库）
- **ExcelUtil** - Unity 内 Excel 读取与转换工具（C#，Editor 环境）
- **EPPlus** - Unity Excel 处理库（Assets/FrameWork/Plugins/EPPlus/）
- **ExcelEditorWindow** - Excel 编辑器窗口（导出 JSON）

### 配置表位置
- **原始 Excel**: `Assets/Data/Excel/`
- **导出 JSON**: `Assets/Resources/JsonText/`

### 关键文件

| 文件 | 路径 |
|------|------|
| Excel 读取脚本 | `.claude/scripts/excel_read.py` |
| Excel 写入脚本 | `.claude/scripts/excel_write.py` |
| ExcelUtil (C#) | `Assets/FrameWork/Scripts/Utils/ExcelUtil.cs` |
| ExcelEditorWindow | `Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs` |
| 配置目录 | `Assets/Data/Excel/` |
| 导出目录 | `Assets/Resources/JsonText/` |

---

## 配置表总览

### 框架系统（FrameWork）

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_audio_info[音频信息_FrameWork].xlsx` | AudioInfo | 182 | id, name_res, remark, audio_type, volume_scale |
| `excel_base_info[基础信息_FrameWork].xlsx` | BaseInfo | 3 | id, content |
| `excel_language[多语言_FrameWork].xlsx` | UIText + 17个子表 | 20(UIText) | id, content_cn, content_en |
| `excel_spine_animation_state[骨骼动画枚举_FrameWork].xlsx` | SpineAnimationState | 33 | id, res |
| `excel_ui_text[UI文本_FrameWork].xlsx` | UIText | 152 | id, content[language] |

> `excel_language` 包含多 Sheet：UIText / AbyssalBlessingInfo / ItemsType / ItemsInfo / BuffInfo / DoomCouncilInfo / StoreGashaponmachineInfo / GameWorldInfo / CreatureInfo / NpcInfo / CreatureModel / CreatureAttributeTypeInfo / ConversationCouncilorInfo / TitleInfo / NpcRelationshipInfo / DoomCouncilRatingsInfo / RarityInfo / ResearchInfo

---

### 生物系统（Creature）

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_creature_info[生物信息].xlsx` | CreatureInfo | 110 | id, creature_type, creature_layer, creature_buff, spine_base, attack_mode, attack_search_time, attack_search_back(防守生物转身攻击身后 0否1是: 正面无目标时转身攻击身后,范围同正面; 首用者骷髅战士id=2001; 手写辅助 CreatureInfoBeanPartial.IsAttackSearchBack), HP/MP/DR/ATK/ASPD/MSPD/MPR/MPF, model_id, unlock_id, name[language], body_size(体型倍率: 空/0=1倍、"0.9,1.1"=区间随机、"1.1"=固定；扭蛋/建号按creatureId创建时随机一次), remark （42列） |
| `excel_creature_attribute_type_info[生物属性信息].xlsx` | CreatureAttributeTypeInfo | 12 | id, mark_name, res_name, color_text, name[language] |
| `excel_creature_model[生物模型信息].xlsx` | CreatureModel | 66 | id, res_name, unlock_id, size_spine, ui_show_spine, name[language] |
| `excel_creature_model_info[生物模型详情信息] .xlsx` | CreatureModelInfo | 438 | id, model_id, show_type, part_type, res_name, color_state |
| `excel_creature_random_info[生物随机信息] .xlsx` | CreatureRandomInfo | 16 | id, skin_random_data |

---

### 战斗系统（Fight / Buff）

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_attackmode_info[攻击方式].xlsx` | AttackModeInfo | 32 | id, class_name, prefab_name, visual_name(DSP批量渲染分桶key,空=不走DSP), buff, attack_search_type, collider_size/area, effect_hit/damage, speed_move, sound_start/miss/hit, start_pos_offset, trail_data(拖尾/残影:count残影数&interval采样间隔秒&startAlpha最新档透明度&endAlpha最老档透明度&color染色rgb,需配visual_name,空=无拖尾), child_attack_mode_id, damage_add_rate(伤害加成比例,float,最终伤害=攻击者ATK×该值,0/空=按1倍;自爆史莱姆爆炸300001配50), remark （20列）。102001=BOSS技能"前方3格"(AttackModeMeleeArea, type24, size"1.5,1,0.25") |
| `excel_attackmode_ext_info[攻击模块扩展信息].xlsx` | AttackModeExtInfo | 1 | id, attack_mode_id(对应AttackModeInfo), ext_type(额外攻击类型,1=BOSS技能), trigger_interval(释放间隔秒), remark （5列）。配 NpcInfo.attack_mode_ext 实现"额外攻击"按间隔自动释放(不限于BOSS)，运行时由 AIIntentCreatureAttack 的额外攻击机制消费 |
| `excel_buff_info[buff信息].xlsx` | BuffInfo | 70 | id, buff_type, rarity, class_entity/events/data, trigger_value/chance/num/time/effect, name[language] （24列） |
| `excel_buff_pre_info[buff前置条件信息].xlsx` | BuffPreInfo | 6 | id, class_entity |
| `excel_fight_scene[战斗场景].xlsx` | FightScene | 4 | id, name_res, road_color_a/b, skybox_mat |
| `excel_fight_type_conquer_info[战斗-征服模式].xlsx` | FightTypeConquerInfo | 12 | id, world_id, fight_scene_ids, enemy_ids, enemy_num, attack_wave, fight/road/level 参数, drop/reward_crystal, reward_reputation(通关声望奖励) （24列） |

---

### 道具系统（Item）

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_items_info[道具信息].xlsx` | ItemsInfo | 228 | id, item_type, item_weapon_type, num_max, creature_model_id, icon_res, attack_mode_data, name[language] （12列） |
| `excel_items_type[道具类型].xlsx` | ItemsType | 10 | id, icon_res, name[language] |

---

### NPC 与议会系统（NPC / DoomCouncil）

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_npc_info[NPC信息].xlsx` | NpcInfo | 37 | id, creature_id, npc_type, level, HP/MP/DR/ATK/ASPD/MSPD, skin_data, equip_item_ids, councilor_ratings, title_data, name[language], body_size(体型倍率: 空/0=1倍、"0.9,1.1"=区间随机、"1.1"=固定), attack_mode_ext(Boss额外技能:逗号分隔的AttackModeExtInfo id,非空即启用), remark （20列） |
| `excel_npc_relationship_info[NPC关系信息].xlsx` | NpcRelationshipInfo | 7 | id, icon_res, name[language], relationship_min/max, relationship_type |
| `excel_doom_council_info[终焉议会信息].xlsx` | DoomCouncilInfo | 13 | id, success_rate, cost_reputation/crystal, class_entity_name/data, unlock_id, name[language] （11列） |
| `excel_doom_council_ratings_info[终焉议会议员等级信息].xlsx` | DoomCouncilRatingsInfo | 12 | id, icon_res, vote, name[language] |
| `excel_conversation_councilor_info[对话-议员].xlsx` | ConversationCouncilorInfo | 33 | id, relationship, content[language] |

---

### 游戏世界与进程系统

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_abyssal_blessing_info[深渊馈赠信息].xlsx` | AbyssalBlessingInfo | 8 | id, icon_res, parent_id, level, buff_ids, name[language], details[language], remark, valid(0无效1有效,生成器据此过滤), max_count(一局最多获得次数,0=不限,仅 level<=0 生效) |
| `excel_effect_info[粒子信息].xlsx` | EffectInfo | 18 | id, res_name, show_type, show_time, float/int/long/vector3/vector4_data |
| `excel_game_world_info[游戏世界信息].xlsx` | GameWorldInfo | 4 | id, icon_res, unlock_id, unlock_id_infinite/conquer_difficulty_level/quick_attack/speed2, map_pos, name[language] |
| `excel_level_info[等级信息].xlsx` | LevelInfo | 12 | id, level_exp, sacrifice_num, attribute_point(升级获得加点数,当前全等级配置5), CMP_rate(魔力召唤增加倍率,按等级递增) |
| `excel_rarity_info[稀有度].xlsx` | RarityInfo | 8 | id, ui_board_color, buff_color, item_add_relationship, name[language], CMP_rate(魔力召唤增加倍率,N=0依次+0.5) |
| `excel_research_info[研究信息].xlsx` | ResearchInfo | 83 | id, research_type, icon_res, level_max, position_x/y, unlock_id, pre_unlock_ids, pay_crystal, name[language] |
| `excel_title_info[称号信息].xlsx` | TitleInfo | 15 | id, name[language] |
| `excel_unlock_info[解锁信息].xlsx` | UnlockInfo | 108 | id, unlock_type |

---

### 商店系统（Store）

| 文件名 | Sheet | 数据行 | 主要列 |
|--------|-------|--------|--------|
| `excel_store_gashaponmachine_info[商店-扭蛋机].xlsx` | StoreGashaponMachineInfo | 23 | id, creature_ids, buy_num, pay_crystal, icon_res, pre_unlock_ids, name[language] |

---

## 约束

- 读写 xlsx 必须使用 openpyxl，编码 UTF-8
- **新增数据必须按 id 由小到大排序插入**：新增配置行时不要直接追加到表格末尾，而要根据 id 大小插入到中间对应位置，保证整张表的 id 始终保持升序。`excel_add_row.py` 已默认按 id 排序插入（新 id 比所有现有 id 都大时才落到末尾）；除非特殊需求，禁止使用 `--append` 强制追加打乱排序。
- 写入前使用 `--backup` 参数备份原文件
- 配置表为统一 3 行表头规范（列名/类型/中文说明），数据从第 4 行开始；脚本默认 `--header-rows 3`
- 配置表修改后必须通过 ExcelEditorWindow 导出 JSON
- 导出的 JSON 文件编码为 UTF-8
- 新增配置表需在编辑器工具中注册
- 多语言文本通过 `excel_language[多语言_FrameWork].xlsx` 统一管理，各表的 `name[language]`、`content[language]` 列均引用该表
- `*InfoBean.cs` 和 `*Bean.cs` 是自动生成的，禁止直接修改
- **`valid` 有效性列约定（生成器内置过滤）**：任意配置表只要含名为 `valid` 的列（int，`0`=无效/`1`=有效），`ExcelEditorWindow.CreateEntity` 生成的 `Cfg` 会自动加 `valid!=0` 过滤——`GetAllArrayData` 过滤数组、`GetItemData` 改走 `GetAllArrayData`，valid==0 的行运行时彻底不存在。⚠️ 给某表新增 `valid` 列后必须把现有每行填 `1` 并重新导出，否则该表全部数据被当无效丢弃（JSON int 缺省 0）。当前已启用：`AbyssalBlessingInfo`。详见 editor-extension-system SKILL。
- PowerShell 操作含中文/方括号的文件路径时使用 `Copy-Item -LiteralPath` / `Remove-Item -LiteralPath` 避免方括号被解析为通配符
