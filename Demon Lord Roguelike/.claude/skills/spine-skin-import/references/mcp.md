# Spine MCP 与 4.3 CLI

## 连接

- 使用 [UmutFarukDilli/spine-mcp](https://github.com/UmutFarukDilli/spine-mcp#connecting-to-claude-and-other-clients) 的 stdio 服务。本项目验证版本为 `0.2.0`，提交 `c5689892a990d252a7d51ae3bb1427374fb093f8`。支持 stdio MCP 的客户端可以使用 `uvx`，参数为 `--from`、`git+https://github.com/UmutFarukDilli/spine-mcp@c5689892a990d252a7d51ae3bb1427374fb093f8`、`spine-mcp`。
- 服务环境中把 `SPINE_CLI_PATH` 设为本机已安装的 Spine CLI，Windows 用 `Spine.com`。用 `SPINE_ARGS=-u <已安装的 4.3 补丁版本>` 固定版本，哥布林实测为 4.3.26。用 `spine_cli_info` 核对工程、骨架名和实际启动的编辑器版本。

## 导出重建基准

1. 在原工程副本运行 `spine_cli_info`，记录版本与骨架清单。调用 `spine_export(project=<副本>, output=<基准目录>, settings=<完整导出预设>)`，使用当前版本 JSON（`class: export-json`），设置 `all=true`、`nonessential=true`、`cleanUp=false`、`packAtlas=null`。每个骨架保留一份原始导出用于比对，在工作副本中编辑。核对导出覆盖整个原工程，包括平时禁用导出的内容。
2. 从原副本读取已保存的上次导出设置：`Spine.com -u <版本> -i <原工程副本> -o <暂存目录> --last-export-settings`。历史设置可能指定旧版本，另核对实际发布 JSON 版本及图集参数。没有历史设置时，从已有发布资源或项目预设确认参数。保存可复用的外置发布预设，将输入和输出指向本次工程与暂存目录。
3. 如需加件，用 `spine_add_slot` 指定槽名和父骨骼，用 `spine_add_skin` 创建带文件夹的完整皮肤名，再用 `spine_add_attachment` 指定 `slot`、同名 `name`、`skin`、`image_path`、`assign=true`、局部变换及实际 PNG 尺寸。修改已有槽的父骨骼时，用 `spine_update_slot` 修改 `bone` 并补偿附件局部变换。

## 续用增量合并副本

- 任务从增量数据续做且要先并入已有骨架时，在工程副本使用支持合并的 Spine 4.3 CLI，并以 `--to` 指定目标骨架。增量 JSON 只包含本批新增槽、皮肤及附件，以及它们所需的父骨骼和祖先。普通 `spine_import_data` 导入已有工程会添加骨架。
- 仅对导入启动独立 MCP 进程，在其环境中设 `SPINE_ARGS=-u <版本> --to <目标骨架名> --merge`。调用 `spine_import_data(project=<工程副本>, data_file=<增量 JSON>, scale=1)`，省略 `skeleton_name`。日志应出现 `Skeleton merge`。其他 MCP 调用使用仅含 `-u <版本>` 的服务环境。
- 客户端无法给导入单独配置 MCP 进程时，可直接调用官方 CLI：

  ```powershell
  & $spineCli -u $version -i $deltaJson -s 1 -o $projectCopy --to $skeleton --merge -r
  ```

- 增量合并省略 `--replace` 以保留旧附件。4.3.26 实测即使输入携带按目标顺序排列的旧槽，新槽仍追加到末尾。用完整导出预设回读合并后的副本，核对本批与原有数据，再执行下一节。 [官方 CLI 合并说明](https://esotericsoftware.com/forum/d/30191-%E5%B0%86a-skeletons%E7%9A%84%E4%B8%80%E5%88%87%E5%86%85%E5%AE%B9%E5%90%88%E5%B9%B6%E5%88%B0b-skeletons%E7%9A%84%E9%97%AE%E9%A2%98/3)

## 设置姿态排序与新建工程

1. 以完整 `slots` 的槽名列表确定目标序列，`index=0` 在最背面。新槽可用 `spine_add_slot(index=…)` 指定位置。移动已有槽用 `spine_update_slot(project=<工作 JSON>, name=<槽名>, index=<目标下标>)`。每次移动会改变其他槽的下标，按当前列表定位，最后核对整个序列。见 [MCP 工具说明](https://github.com/UmutFarukDilli/spine-mcp#fixing)。
2. 检查动画的 `drawOrder` 和 `drawOrderFolder` 时间轴。若移动 Setup 槽会改变关键帧结果，按旧帧中各槽的实际前后关系转换到新槽序，并将新增槽放入应随行的区段。保留关键帧时间与原有动作遮挡。用 `spine_validate` 校验工作 JSON，并将其与基准逐字段比较，只接受本批计划的差异。
3. 用只指定 4.3 补丁版本的 MCP 进程执行 `spine_import_data(project=<尚不存在的候选 .spine 路径>, data_file=<排序后的完整骨架 JSON>, skeleton_name=<原骨架名>, scale=1)`。工作 JSON 与候选工程均须能按原相对路径解析贴图。多骨架工程按原清单逐个导入同一个新工程，各骨架各导入一次。重试时从新的候选路径开始，避免重复添加骨架。
4. 对候选执行 `spine_cli_info` 核对骨架名称、数量和工程帧率，再用与基准相同的完整预设回导，和预期工作 JSON 比较。验证旧造型时显式使用相同皮肤组合，遇到绘制顺序关键帧时比对其切换前后的渲染结果。记录重建导致的预览和历史设置变化。见 [Spine 开发者对工程信息往返的说明](https://en.esotericsoftware.com/forum/d/18145-project-versioning)。

## 导出运行时资源

- 调用 `spine_export(project=<候选或指定工程>, output=<暂存目录>, settings=<已核对的发布预设>)`，从同一工程输出 JSON、图集描述和全部 PNG 页。核对 4.3 版本、动画清理选项、图集扩展名、透明度和过滤设置。纯排序时沿用相同打包参数，比对原有图集区域与像素。新增部件允许重新排图，核对所有新旧附件引用的区域、页名与 PNG 均匹配。[Spine CLI 导出](https://esotericsoftware.com/spine-command-line-interface#Export)
- 原工程有禁用导出的内容时，用同一发布预设分别导出基准副本和候选，确认原先排除的内容在发布产物中仍被排除。
