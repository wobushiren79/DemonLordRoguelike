---
name: spine-unity-import
description: 将已验收的 Spine JSON、.atlas.txt 和全部 PNG 页原位导入 Unity，同步武器、装备、道具及可自定义身体部件的配置和适用随机池，再导出运行时数据并在编辑模式验收。
---

# 导入 Unity Spine 资源并同步配置

## 核对源和导入位置

1. 从 [Spine 导出](../spine-skin-import/SKILL.md#验收与交付) 的交付结果确认同一次导出的运行时 `<骨架名>.json`、`<骨架名>.atlas.txt` 和图集列出的全部 PNG 页。核对 JSON 的 `skeleton.spine` 与 Unity 项目安装的 spine-unity 主次版本一致，确认每个纹理附件路径都有对应的同名图集区域，图集页的尺寸与 PNG 一致。
2. 在 `Assets/LoadResources/Spine/Creature/` 中定位角色现有的 `*_SkeletonData.asset`，按它关联的 JSON、`*_Atlas.asset`、材质和贴图确定准确的替换路径。记录源文件哈希、将覆盖的文件哈希及原有资源 GUID。哥布林的源目录为 `../资源/生物/哥布林/`，目标目录为 `Assets/LoadResources/Spine/Creature/Goblin/`，文件名为 `Goblin.json`、`Goblin.atlas.txt` 和 `Goblin.png`。
3. 核对图集的 Alpha 类型与目标材质和贴图导入设置相容。哥布林图集声明 `pma:true`，对应 URP Spine Sprite 材质的 `_ALPHAPREMULTIPLY_ON`。
4. 从用户选定的导入批次、母版同级的 `<生物>_素材名字.xlsx` 和实际 Spine 皮肤核对新增或更新的款式，准备配置清单。武器、装备和其他道具需要 `ItemsInfo` 与对应的多语言名称；使用骨架皮肤的武器或装备还需要 `CreatureModelInfo` 外观记录。可自定义身体部件写入 `CreatureModelInfo`。按[配置表与皮肤绑定](references/configuration.md)查重和确定字段。纯贴图刷新继续使用原配置；资源已导入但本批配置缺失时，直接从补表继续。

## 原位导入

1. 按 [Unity MCP 连接](../unity-mcp-connection/SKILL.md) 检查服务并选择与当前工程路径相符的 Unity 实例。按项目 `CLAUDE.md` 的 MCP 连接规则处理未连接的情况。
2. 将要覆盖的旧文件备份到 `Assets/` 之外的临时目录。对同名导出，通过 Unity MCP 复用 [GameResourceEditor.ImportSpineResources](../../../Assets/Editor/GameResourceEditor.Spine.cs)。调用时给源目录传入仅包含本次配套文件的临时目录，给目标目录传入现有 Spine 资源目录。只有所有源文件和同名目标均已核对齐全时才开始覆盖。原位写入 `.json`、`.atlas.txt` 和图集页 PNG，保留现有 `.meta`、`*_SkeletonData.asset`、`*_Atlas.asset` 和材质引用。
3. 图集页名或页数变化时，按新图集清单核对新增页和已有页，在 Unity 中一起导入并验证图集材质与各页的绑定。现有导入方法只覆盖已存在的同名文件，新增页需通过 Unity 导入。导入过程中发生错误时，用备份恢复已覆盖文件并刷新。

用户选择在编辑器中手动导入同名文件时，使用「游戏 → 游戏资源处理 → Spine → Spine 资源导入」。界面中的「目标目录」填外部导出目录，「导入目录」填 Unity 内的资源目录，确认配套文件齐全后执行单条导入。

## 补齐配置并导出

1. 资源刷新并可加载后，按[配置参考](references/configuration.md)更新正式 Excel 源表，先登记部件，再写引用它的道具和多语言名称。继续使用已有 ID，新增行按 ID 升序插入。配置值来自已确认的素材和设计参数；遇到缺少必要参数或绑定有歧义时，列明相应款式与字段并继续处理已确定的项。
2. 按[随机池规则](references/configuration.md#同步随机池)将本批可随机的身体部件、武器和装备同步到该物种适用的现有随机池。已登记的款式可单独补同步。
3. 核对新记录需要的图标，按当前配置引用补齐缺失资源。定向重新导出受影响的运行时配置。需要补表的款式须验证配置引用及适用随机池的成员。

## 核验与清理

1. 全套文件到位后刷新 Unity 资源数据库。在编辑模式加载原有的 `SkeletonDataAsset` 和图集，确认依赖可解析，新增皮肤和区域可找到，骨架、动画及项目原有 GUID 正常。检查本次导入相关的 Console 错误，并对照源和目标文件哈希。需要 Play 模式验收时由用户手动运行。
2. 对本批配置按表名和 ID 回读，确认武器或装备经道具的部件 ID 找到正确皮肤，图标和多语言键可解析，身体部件在目标模型和部位下生效。发现配置写入或导出失败时，恢复该次未完成的关联表和派生文件；若资源已成功导入，说明剩余待办。
3. 核验通过后清理本次创建的临时导入目录、备份、脚本和缓存。交付 Unity 资源路径、受影响表及记录 ID、运行时导出验证结果和未完成项。
