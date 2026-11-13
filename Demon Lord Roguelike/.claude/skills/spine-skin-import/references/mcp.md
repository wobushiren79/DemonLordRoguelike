# Spine MCP 与 4.3 CLI

## 连接

- 使用 [UmutFarukDilli/spine-mcp](https://github.com/UmutFarukDilli/spine-mcp#connecting-to-claude-and-other-clients) 的 stdio 服务。本项目验证版本为 `0.2.0`，提交 `c5689892a990d252a7d51ae3bb1427374fb093f8`。支持 stdio MCP 的客户端可以使用 `uvx`，参数为 `--from`、`git+https://github.com/UmutFarukDilli/spine-mcp@c5689892a990d252a7d51ae3bb1427374fb093f8`、`spine-mcp`。
- 服务环境中把 `SPINE_CLI_PATH` 设为本机已安装的 Spine CLI，Windows 用 `Spine.com`。用 `SPINE_ARGS=-u <已安装的 4.3 补丁版本>` 固定版本，哥布林实测为 4.3.26。用 `spine_cli_info` 核对工程、骨架名和实际启动的编辑器版本。

## 构建增量数据

1. 对当前工程用 `spine_export` 读出基准 JSON，复制为工作 JSON。
2. 每件先用 `spine_add_slot` 指定 `name` 和父骨骼 `bone`，用 `spine_add_skin` 创建带文件夹的完整皮肤名，再用 `spine_add_attachment` 指定 `slot`、同名 `name`、`skin`、`image_path`、`assign=true` 及附件局部变换和实际 PNG 尺寸。调整已有槽的父骨骼时，用 `spine_update_slot` 修改 `bone`，并按目标父骨骼补偿附件局部变换。
3. 用 `spine_validate` 校验工作 JSON。为增量导入取出新槽、新皮肤、绑定附件、所需父骨骼及祖先，保留与基准一致的骨骼设置姿态和图片路径。现有皮肤和动画留在 `.spine` 中。保留原骨架的数据用于导入后比对。

## 合并到现有骨架

- Umut 的 `spine_import_data` 默认执行骨架导入，实测在工程内新增了第二个骨架。需用支持合并的 Spine 4.3 CLI，并以 `--to` 指定现有骨架。
- 仅对导入启动独立 MCP 进程，在其环境中设 `SPINE_ARGS=-u <版本> --to <目标骨架名> --merge`。调用 `spine_import_data(project=<工程副本>, data_file=<增量 JSON>, scale=1)`，省略 `skeleton_name`。日志应出现 `Skeleton merge`。其他 MCP 调用使用仅含 `-u <版本>` 的服务环境。
- 客户端无法给导入单独配置 MCP 进程时，可直接调用官方 CLI：

  ```powershell
  & $spineCli -u $version -i $deltaJson -s 1 -o $projectCopy --to $skeleton --merge -r
  ```

- 增量合并省略 `--replace`，保留旧附件。哥布林 4.3.26 实测会把新槽追加到 Setup 绘制顺序末尾。完成结构校验后，把合并副本和各组新增槽要并入的类别交给用户。 [官方 CLI 合并说明](https://esotericsoftware.com/forum/d/30191-%E5%B0%86a-skeletons%E7%9A%84%E4%B8%80%E5%88%87%E5%86%85%E5%AE%B9%E5%90%88%E5%B9%B6%E5%88%B0b-skeletons%E7%9A%84%E9%97%AE%E9%A2%98/3)

## 导出运行时资源

- 用仅配置 4.3 版本的 MCP 服务调用 `spine_export(project=<验收后的工程>, output=<临时导出目录>, format="json", pack=true)`。JSON 采用默认设置。已有图集需要沿用专用参数时，读取工程保存的图集参数，并通过 Spine 4.3 CLI `-e json+pack` 和 `--set packAtlas.<字段>=<值>` 导出配套文件。 [Spine CLI 导出](https://esotericsoftware.com/spine-command-line-interface#Export)
- 临时导出目录中确认 `skeleton.spine` 为 4.3、皮肤绑定及插槽顺序正确。逐一核对渲染附件的贴图路径与图集区域、图集页名与实际 PNG 文件。验证通过后，将同一次导出的整套文件写到 Aseprite 母版同级目录。如果目标已有运行时资源，备份后成套写回。回读通过后清理临时文件。
