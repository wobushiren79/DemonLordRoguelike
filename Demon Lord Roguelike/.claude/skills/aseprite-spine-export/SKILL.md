---
name: aseprite-spine-export
description: 在角色 Aseprite 母版中持久标记暂缓导出的装备、武器和身体部件，使用 Prepare-For-Spine_AI.lua 导出 Spine 导入 JSON 和 images/ 分件 PNG。
metadata:
  watched_files:
    - .claude/scripts/run-aseprite.ps1
    - 资源/生物/Prepare-For-Spine_AI.lua
    - 资源/生物/Spine-Export-Marking.lua
    - 资源/生物/Spine-Export-Exclude.lua
    - 资源/生物/Spine-Export-Restore.lua
    - Assets/FrameWork/Addons/Spine/Prepare-For-Spine_AI.lua
---

# 从 Aseprite 母版导出 Spine 素材

## 确定输入和输出

- 使用角色正式 `.aseprite` 母版。从 Unity 项目目录定位脚本 `../资源/生物/Prepare-For-Spine_AI.lua`。脚本按当前已保存的 Aseprite 文件名和目录输出 `<母版名>.json` 与 `images/<附件名>.png`。正式交付放在母版同级，例如 `资源/生物/哥布林/哥布林.json` 与 `资源/生物/哥布林/images/`。
- 先核对同级已有的导入 JSON 和 `images/`，确认本次是首次导出、更新指定素材，还是刷新整份导出。旧导入 JSON 的 `skeleton.images` 指向 `images/`，只有 `root` 骨骼与 `default` 皮肤。哥布林的 `哥布林.json` 是此脚本的导入 JSON。同目录的 `Goblin.spine`、`Goblin.json`、`Goblin.atlas.txt` 和 `Goblin.png` 属于后续 Spine 工程与运行时导出，原样保留。如果 `<母版名>.json` 已被运行时骨架占用，先定位正确的导入文件再写回。
- 更新指定素材时，从回填交付清单或 `<生物>_素材名字.xlsx` 确认母版帧号和完整图层路径。Excel 名称用于核对素材身份，PNG 和 JSON 附件名沿用脚本按图层和帧生成的名称。

## 在母版中标记暂缓导出的素材

1. 在 Aseprite 打开正式母版，在时间轴点选目标帧与目标图层交叉的 Cel。单件武器、眼睛等单图层款式选一格即可。整套装备按素材名字表选中该套实际使用的多个格子，可分几次标记。前后发、左右翼等配对资源也按款式选择各层。同帧的眼嘴、独立武器或鼻环可分别保留。
2. 快速操作时运行「文件 → 脚本 → Spine-Export-Exclude」立即排除所选 Cel，或运行「Spine-Export-Restore」立即恢复；这两个入口不弹窗，仍执行链接 Cel 检查。需要查看选区排除数量、明确选择操作或刷新整份母版颜色时，运行「Spine-Export-Marking」使用原窗口。所选 Cel 排除后的时间轴底色为亮红色（`#FF4848`），同一图层的各帧可单独识别。按 Ctrl+S 保存母版，重开后仍可看到标记。恢复会还原这些格子排除前的颜色；排除期间手动改过的其他颜色会保留。链接 Cel 共用标记和底色，只需处理其中几帧时，先将那些格子取消链接。
3. 两个快捷入口可在「编辑 → 键盘快捷键」（Ctrl+Alt+Shift+K）中搜索脚本名并分别绑定按键。绑定后在时间轴选中 Cel，按对应键即可直接排除或恢复，无需再点窗口按钮。Aseprite 普通 Lua 脚本不能向时间轴原生右键菜单添加自定义项。
4. 照常运行「文件 → 脚本 → Prepare-For-Spine_AI」。脚本在写 PNG、槽位和附件之前跳过标记的 Cel。多个 Cel 导出同一名称时，采用仍可导出的最上层 Cel。写入旧目录会保留已存在的未引用 PNG，其他 Spine 工程可能仍在使用它们。

导出器、标记窗口和两个快捷入口的仓库原件都在 `资源/生物/`。换机器或更新菜单脚本时，通过「文件 → 脚本 → 打开脚本文件夹」复制这四个 Lua 文件，再选择「重新扫描脚本文件夹」，然后按上一步设置快捷键。框架子模块中的同名 AI 导出脚本也遵循此标记约定。

旧版已排除的格子可在同一脚本里点「刷新整份母版的标记颜色」，一次补齐整份母版的底色，再按 Ctrl+S 保存。时间轴缩略图挡住底色时，在时间轴齿轮菜单关闭「缩略图」（默认快捷键 F6）。

导出器读取 Cel 用户数据（`cel.data`）中独占一行的 `[spine:skip]`。时间轴底色用于显示状态，自行给 Cel 上色不会改变导出结果。自动化增删标记时保留原备注，随后刷新标记颜色并保存。脚本在 Cel 的 `dlr/spine-export` 扩展属性中保存原时间轴色，恢复后清除备份。

## 在同名副本运行

在独立临时目录复制母版，副本保留原文件名，提前创建同级 `images/`。记录正式母版哈希。脚本会写当前文件旁的同名产物，因此在临时目录执行，校验后再发布到正式目录。

从 Unity 项目目录运行以下命令，其中 `$master` 是已选母版的路径。

```powershell
$spineScript = (Resolve-Path -LiteralPath '../资源/生物/Prepare-For-Spine_AI.lua').Path
$exe = powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-aseprite.ps1' -WhichOnly
$stage = Join-Path $env:TEMP ('aseprite-spine-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $stage 'images') -Force | Out-Null
$copy = Join-Path $stage (Split-Path -Leaf $master)
Copy-Item -LiteralPath $master -Destination $copy
& $exe -b $copy --script $spineScript | Out-Host
```

- 脚本按叶图层自身的可见性筛选。若目标素材在隐藏层，只在副本开启该叶图层。用官方 Lua 设置 `layer.isVisible`，在同一批处理中执行 `& $exe -b $copy --script $visibilityScript --script $spineScript`，其中 `$visibilityScript` 是用于调整副本图层可见性的脚本。需要排除某层时检查叶图层自身，单独隐藏父组可能仍会出图。
- 图层名含数字时，附件名的第一段数字取「原数字 + 母版帧号 − 1」，保留数字前后的文字。图层名无数字时只读第 1 帧。缺失或全透明的 Cel 跳过。同名附件由后处理的上层覆盖，遇到重名时按实际帧和图层核对最终采用的 Cel。
- 哥布林的 `Weapon_R_1` 默认隐藏，若导出其第 21–24 帧，在副本开启该层并核对 `Weapon_R_21` 至 `Weapon_R_24`。`Eye_1`、`Mouth_1` 第 11–14 帧分别生成 `Eye_11` 至 `Eye_14`、`Mouth_11` 至 `Mouth_14`。回填时仅用于展示的固定身体 Cel 不作为新款式发布。

## 校验与写回

1. 回读候选 JSON，核对 `skeleton.images` 为 `images/`、每个目标槽在 `skins.default` 下有附件，且对应 PNG 可读。按源 Cel 核对附件名、像素尺寸和位置。该脚本把画布底边中点作为原点，附件坐标取 Cel 中心并翻转 Y 轴。PNG 尺寸以实际 Cel 为准。脚本退出码为零时仍须检查文件和目标附件是否齐全。
2. 首次导出使用候选 JSON 和 PNG。已有导出时，以旧 JSON 为底按附件名更新本次范围内的 `slots`、`skins.default` 和图片。批处理日志中的 `SPINE_SKIP <附件名>` 要从最终 JSON 的这两处移除；`SPINE_REPLACE <附件名>` 要使用候选中保留下来的同名 Cel 更新 PNG 和槽位、附件数据。即使原定只更新其他素材，这两类也须处理，避免旧资源重新进入交付。其余历史附件及旧槽相对顺序沿用。新变体优先接在同类已有槽之后，例如 `Weapon_R_21` 接在 `Weapon_R_20` 后。脚本会因图层可见性漏掉旧附件，`images/` 也可能存有其他 Spine 工程使用的图，发布时逐个更新文件。
3. 发布前确认最终 JSON 引用的每张图能在目标 `images/` 读取。备份将覆盖的文件，发布后回读 JSON 和本次更新的 PNG，核对母版哈希。部分写入失败时恢复已替换文件并移除本次新建的文件。核验通过后清理备份和临时产物，交付 JSON、`images/` 位置与本次附件名。

用户要求把交付的附件加入现有 Spine 骨架时，使用 [Spine 部件导入与皮肤绑定](../spine-skin-import/SKILL.md)。
