---
name: aseprite-spine-export
description: 将已确认的角色 Aseprite 母版用资源/生物/Prepare-For-Spine_AI.lua 导出为母版同级的 Spine 导入 JSON 和 images/ 分件 PNG。用于回填之后导出新素材或刷新既有导入资源。
metadata:
  watched_files:
    - .claude/scripts/run-aseprite.ps1
    - 资源/生物/Prepare-For-Spine_AI.lua
---

# 从 Aseprite 母版导出 Spine 素材

## 确定输入和输出

- 使用角色正式 `.aseprite` 母版。从 Unity 项目目录定位脚本 `../资源/生物/Prepare-For-Spine_AI.lua`。脚本按当前已保存的 Aseprite 文件名和目录输出 `<母版名>.json` 与 `images/<附件名>.png`。正式交付放在母版同级，例如 `资源/生物/哥布林/哥布林.json` 与 `资源/生物/哥布林/images/`。
- 先核对同级已有的导入 JSON 和 `images/`，确认本次是首次导出、更新指定素材，还是刷新整份导出。旧导入 JSON 的 `skeleton.images` 指向 `images/`，只有 `root` 骨骼与 `default` 皮肤。哥布林的 `哥布林.json` 是此脚本的导入 JSON。同目录的 `Goblin.spine`、`Goblin.json`、`Goblin.atlas.txt` 和 `Goblin.png` 属于后续 Spine 工程与运行时导出，原样保留。如果 `<母版名>.json` 已被运行时骨架占用，先定位正确的导入文件再写回。
- 更新指定素材时，从回填交付清单或 `<生物>_素材名字.xlsx` 确认母版帧号和完整图层路径。Excel 名称用于核对素材身份，PNG 和 JSON 附件名沿用脚本按图层和帧生成的名称。

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
2. 首次导出使用候选 JSON 和 PNG。已有导出时，以旧 JSON 为底按附件名更新本次范围内的 `slots`、`skins.default` 和图片，保留其他附件及旧槽相对顺序。新变体优先接在同类已有槽之后，例如 `Weapon_R_21` 接在 `Weapon_R_20` 后。刷新整份导出时也要核对历史附件和顺序。脚本会因图层可见性漏掉旧附件，`images/` 也可能存有其他 Spine 工程使用的图，发布时逐个更新文件，保留目录中的其他图片。
3. 发布前确认最终 JSON 引用的每张图能在目标 `images/` 读取。备份将覆盖的文件，发布后回读 JSON 和本次更新的 PNG，核对母版哈希。部分写入失败时恢复已替换文件并移除本次新建的文件。核验通过后清理备份和临时产物，交付 JSON、`images/` 位置与本次附件名。

用户要求把交付的附件加入现有 Spine 骨架时，使用 [Spine 部件导入与皮肤绑定](../spine-skin-import/SKILL.md)。
