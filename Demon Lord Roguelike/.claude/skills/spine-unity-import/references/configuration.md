# Unity 配置表与 Spine 皮肤绑定

下列工作簿均位于 Unity 项目的 `Assets/Data/Excel/`。处理配置时使用实际工作表列名和项目现有数据。

| 工作簿 | 工作表 | 用途 |
|---|---|---|
| `excel_creature_model[生物模型信息].xlsx` | `CreatureModel` | 通过骨架资源路径查目标模型 ID。通常只读。 |
| `excel_creature_model_info[生物模型详情信息] .xlsx` | `CreatureModelInfo` | 可自定义身体部件和装备外观。文件名在 `]` 与 `.xlsx` 之间有空格。 |
| `excel_creature_random_info[生物随机信息] .xlsx` | `CreatureRandomInfo` | 皮肤池和装备池。文件名在 `]` 与 `.xlsx` 之间有空格。 |
| `excel_items_info[道具信息].xlsx` | `ItemsInfo` | 武器、装备和其他道具的游戏配置。 |
| `excel_language[多语言_FrameWork].xlsx` | `ItemsInfo` | 道具显示名等多语言文本。 |

## 从确认的素材定位配置

1. 读取母版同级名字表的 `素材名字` 工作表，以「母版帧号 + 完整图层路径」定位记录，读取素材类别、类型和确认名称。按[分件导出命名规则](../../aseprite-spine-export/SKILL.md#在同名副本运行)从图层及母版帧找到实际附件，再从运行时 JSON 确认皮肤全名。帧号不一定等于子 Aseprite 帧号。各部位可在同一帧登记；只处理本次指定的款式，跳过复制来做预览的固定身体 Cel。
2. `CreatureModelInfo` 填 `id`、`model_id`、`show_type`、`part_type`、`res_name`、`color_state` 及备注。`res_name` 填完整且区分大小写的 Spine 皮肤路径。按模型、显示类型、部位及皮肤全名查是否已有记录，沿用已有 ID。`part_type` 对照 [CreatureSkinTypeEnum](../../../../Assets/Scripts/Enums/CreatureEnum.cs)，显示类型和染色开关用相同用途的现有部件或已确认参数。部分身体素材登记的是款式编号，可在备注中沿用编号。
3. 对使用该皮肤的道具，用 `ItemsInfo.creature_model_info_id` 引用对应部件 ID，按关联部件和已有道具 ID 查重。无骨架外观的道具按其类型配置，不新增虚构部件。`creature_model_id` 按可装备范围设置，`0` 表示通用；`item_type`、`item_weapon_type` 对照 [道具枚举](../../../../Assets/Scripts/Enums/ItemsEnum.cs)。核对堆叠、图标、攻击参数、获取来源及奖励稀有度。按本批已确认的设计或明确适用的同类模板填值，特别核对空的 `reward_rarity` 表示奖励稀有度不限，空的 `source` 表示默认来源。
4. `ItemsInfo.name[language]` 引用多语言表 `ItemsInfo.id`，保留既有文本 ID。中文 `content_cn` 沿用确认的素材名称，其他语种按项目多语言约定填写。该工作表若有重复语言列，同一语言的两列填相同值。身体部件只登记部件配置，除非用户同时明确要做成可获取的道具。

## 同步随机池

1. 查物种和使用者配置引用的池 ID，结合池类型及既有同类成员确定要更新的行。同物种可能同时有通用外观池、角色创建池和用途更窄的池。将本批可随机的款式纳入适用池，已有专用池继续按其用途选款。
2. `random_type=0` 的外观池在 `skin_random_data` 填 `CreatureModelInfo.id`。由装备驱动的外观（如鼻环）随装备走散件池。 `random_type=1` 的散件池在 `equip_random_data` 填 `ItemsInfo.id`。`random_type=2` 的套装池引用 `EquipSuitInfo.id`，等有对应套装变更时同步。检查新增武器的物种、装备槽和职业武器子类型能否通过现有筛选，报告因职业限制无法抽取的款式。
3. 池成员以逗号分隔，连续区间的两端均包含。核对原成员和本批 ID，确认只增加预期成员、没有由于扩大区间引入未选 ID，重复导入时不重复添加。导出并回读 `CreatureRandomInfo.txt`，确认池中的新 ID 可在对应的正式配置中解析，报告目标池和实际生效范围。

## 写入、导出与回读

- 按 [Excel 读写](../../excel-io/SKILL.md)和[项目规则](../../../../CLAUDE.md#excel-读写规则)用 `run-python.ps1` 和 `openpyxl` 处理。写入前在 `Assets/` 外备份本次要改的 Excel 和派生产物，沿用当前表的 ID 编段并查重，按 ID 升序新增。分别回读每张表，关闭工作簿和文件流，确认 Git 可读。
- 若道具需要从 Spine 出图标，按同类现有命名为本批皮肤生成图标，确认 `icon_res` 能在对应 SpriteAtlas 中查到。眼嘴等部件在使用选件缩略图时核对 `Textures/Skins` 和 `AtlasForSkins`。单页图集可通过 Unity MCP 复用 [SpineWindow.ExtractAndSaveTextures](../../../../Assets/FrameWork/Editor/Base/Window/SpineWindow.cs) 逐皮肤处理，PPU 对齐项目当前设置；多页图集按实际区域所属纹理页提取。只更新本批图标并重打涉及的图集，避免用全角色提取覆盖旧图标。
- 用 Unity 编辑器的 `ExcelUtil.ExcelToJsonItem(FileInfo, string)` 导出到临时目录，核对新增或修改的 ID、引用和字段后，将受影响的 `ItemsInfo.txt`、`CreatureModelInfo.txt`、`CreatureRandomInfo.txt`、`Language_ItemsInfo_*.txt` 发布到 `Assets/Resources/JsonText/`。按实际改表选择文件。多语言工作簿会导出所有工作表，只取本批受影响的派生文件。只有编辑器手动路径可用时，可用「Custom → 工具弹窗 → 处理 Excel → 生成 Json」处理对应工作簿，并核对其他派生文件未被无关改动。导出工具可能只记日志而不抛异常，需读回文件确认。
- 在编辑模式核对 `道具 ID → 部件 ID → 模型/完整皮肤名`、`道具名称 ID → 多语言文本` 或 `模型/部位 → 身体部件皮肤`。旧资源引用和非本批配置应保持不变。若只能写 Excel，交付须明确运行时配置尚待导出。

## 哥布林示例

哥布林 `model_id=6`。其同类记录的 `show_type=0`，眼睛、嘴巴、右手武器的 `part_type` 分别为 `5`、`6`、`91`。例如母版第 11 帧的 `Eye_1` 对应 `Eye/Eye_11`，在 `CreatureModelInfo` 登记外观。母版第 21 帧的 `Weapon_R_1` 对应 `Weapon/Weapon_R_21`，需登记外观后用 `ItemsInfo.creature_model_info_id` 绑定，图标沿用 `Goblin_Atlas_Weapon_Weapon_R_21` 的命名。三种武器可共用右手皮肤分类，道具子类型仍按登记的武器类型选取：单手剑 `2`、弓 `7`、爆炸物 `9`。

哥布林眼睛和嘴巴同步到角色创建池 `6` 和通用外观池 `6000001`。武器道具同步到散件池 `60000001`。套装池 `60000002` 另有独立配置。
