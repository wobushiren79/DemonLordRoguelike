---
name: spine-skin-import
description: 优先用 Spine MCP 将 Aseprite 分件导入角色现有骨架并绑定父骨骼与独立皮肤。交给用户在 Spine 中手动调整绘制顺序，保存后由 AI 验收并导出 Spine 4.3 JSON、图集和 PNG。
---

# 在 Spine 中导入角色部件、创建皮肤并导出

## 定位项目和本批素材

- 在角色 Aseprite 母版同级目录定位正式 `.spine`、[上一步生成的分件 JSON](../aseprite-spine-export/SKILL.md) 和 `images/`，从导出清单确定本批附件。以当前 `.spine` 为准，检查同类旧槽、父骨骼、绘制顺序和皮肤全名。哥布林见[哥布林参考](references/goblin.md)。
- 保持副本能解析同级 `images/`。已完成的皮肤跳过。已有同名槽或附件时，核对它的父骨骼、占位符和所属皮肤，再补缺少的步骤。分件导出可能含旧款式，按清单筛出本批，避免给旧槽额外导入默认皮肤附件。

## 优先通过 Spine MCP 执行

使用 [UmutFarukDilli/spine-mcp](https://github.com/UmutFarukDilli/spine-mcp)。连接、数据准备、现有骨架合并和工具限制见 [MCP 操作参考](references/mcp.md)。

1. 用 `spine_cli_info` 确认目标骨架，用 `spine_export` 从正式工程读取本轮基准 JSON。按物种参考确定新部件的父骨骼、皮肤文件夹和要并入的插槽类别。
2. 在工作 JSON 上用 MCP 创建插槽、独立皮肤和绑定附件。以父骨骼设置姿态的逆变换换算附件位置与方向，附件尺寸取实际 PNG。核对同名设置姿态附件、皮肤占位符和贴图路径。
3. 用 Spine 4.3 的增量合并把本批数据加进原骨架副本，比例 `1`，保留已有附件。回导 JSON 比对新旧槽、皮肤和动画，再交接绘制顺序。

## 在 Spine 编辑器中导入和绑定

需要用 Spine 编辑器完成导入、皮肤绑定或父骨骼设置时，按当前副本状态从尚未完成的步骤继续。使用 Windows Computer Use 时先加载 `computer-use:computer-use` 指南。

1. **打开工程。** 在 Spine 打开物种的 `.spine`，进入设置模式（Setup），确认目标骨架和图片目录。
2. **导入数据。** 主菜单选「导入数据（Import Data）」。导入选项选「JSON 或二进制文件」，文件选母版同级刚导出的 `<母版名>.json`，缩放 `1`，关闭「新项目」，选「导入到现有骨架」并指定目标骨架。现有附件选「忽略（Ignore）」或取消「替换现有附件」，按界面显示核对后导入。核对新增插槽和附件与清单相符，旧槽和旧皮肤仍可用。 [Spine 导入指南](https://esotericsoftware.com/spine-import#Import-into-an-existing-skeleton)
3. **逐件创建皮肤并绑定。** 在同类皮肤文件夹创建与新部件同名的皮肤并激活。选中新槽下本件导入附件，执行「新建 → 皮肤占位符（New → Skin Placeholder）」，将该附件移入当前激活的皮肤。保留同类既有的插槽、占位符和附件命名约定。创建对话框出现时关闭「为每个皮肤复制附件」及相关复制选项，保持原贴图路径。一次只绑定当前皮肤对应的部件，检查其设置姿态使用对应占位符。 [Spine 皮肤占位符](https://esotericsoftware.com/spine-skins#Selected-attachments) · [皮肤文件夹](https://esotericsoftware.com/spine-skins#Folders)
4. **设置父骨骼。** 选中 `root` 下的新插槽（Slot），用「设置父级（Set Parent）」或 `P`，选择同类旧插槽所挂的已有骨骼。核对换父级后的设置姿态位置、方向和尺寸，必要时调整新附件的局部变换，使其与导入图和同类旧部件的对齐方式一致。旧附件的局部变换仅作参考。 [Spine 设置父级](https://esotericsoftware.com/spine-first-steps#Creating-bones)

## 交给用户手动调整绘制顺序

1. AI 先完成导入、皮肤绑定和父骨骼设置，并回读确认这些改动和原有数据。交给用户可直接打开的 `.spine` 副本，列出各组新增槽和它们要并入的已有类别。
2. 用户在 Spine 的「设置（Setup）→ 绘制顺序（Draw Order）」中将新增槽放入同类旧槽的连续区段，保持类别之间的前后关系。每次只显示一个的同类变体，组内顺序任意。例如 `Mouth_11` 可以放在原有嘴巴组的前面、中间或后面。请用户保存同一个副本并告知完成。 [Spine 插槽与绘制顺序](https://esotericsoftware.com/spine-slots#Draw-order)

## 验收与交付

1. 从用户保存的副本继续。AI 回读检查同类槽是否连续、类别之间及其余槽的相对顺序是否正确。对每次只显示一个的同类变体，按类别核对绘制顺序。确认旧骨架、皮肤、附件和动画仍可用，并复核新部件的父骨骼、占位符、皮肤和贴图。若有槽跨到其他类别，指出具体槽位并交给用户修正。检查待机及相关动作中的位置、跟随、遮挡和皮肤切换。无法预览时注明待验收项。
2. 验收通过后，AI 将保存好的副本写回正式工程。用 Spine 4.3 的默认 JSON 设置和图集打包，从同一个工程导出运行时 JSON、图集描述文件和全部 PNG 页到 Aseprite 母版同级目录。沿用已有图集的扩展名和透明度设置，具体命令见 [Spine MCP 操作参考](references/mcp.md#导出运行时资源)。根据骨架名识别运行时 JSON，并与 Aseprite 分件 JSON 区分。
3. 发布前核对实际导出版本、绘制顺序、皮肤绑定、图集文件名及其 PNG 页。确认 JSON 引用的每个贴图路径都在图集中，再交付正式 `.spine`、运行时 JSON、图集描述文件和所有 PNG 页。试跑交付副本，并标明实际采用的导入方式。
4. 正式工程和整套导出文件回读通过后，删除为本次操作创建的临时目录及其中的工程副本、贴图副本、中间 JSON、备份、脚本和缓存。等待用户手动排序或仍需重试时保留所需临时文件。清理后确认临时目录已删除，再结束任务。

需要更新 Unity 内的角色资源时，使用 [Spine 资源导入 Unity](../spine-unity-import/SKILL.md)。
