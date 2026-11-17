---
name: aseprite-orc-backfill
description: 将已确认的兽人眼睛和嘴巴从身体 Aseprite 子文件回填到母版，优先补目标层空位，保留同组搭配，并按实际母版帧号同步素材名字表。
---

# 兽人眼睛和嘴巴回填

按[通用母版回填规则](../aseprite-master-backfill/SKILL.md)核验文件、登记素材和成对写回。以实际母版目录 `../资源/生物/兽人/` 为基准，`..` 相对于 Unity 项目目录。

| 用途 | 路径（相对母版目录） |
|---|---|
| 母版 | `兽人.aseprite` |
| 已确认眼睛和嘴巴 | `身体/兽人_身体.aseprite` |
| 素材名字表 | `兽人_素材名字.xlsx` |

## 核对源帧和位置

- 用 [Aseprite 基础工具](../aseprite-pixel-editor/SKILL.md)检查母版和子文件的画布、颜色模式和实际 Cel。当前母版为 128×128 RGB，眼睛和嘴巴分别使用顶层 `Eye_1`、`Mouth_1`，两层都在 `Head` 上方。
- 母版第 1 帧的 `Head` 在 `(54,39)`，尺寸 21×20。原始眼部包络为 `(62,44)`、10×7，嘴部包络为 `(63,51)`、9×6。按已确认的源 Cel 保留实际像素、原生尺寸、画布坐标、不透明度和 Z 顺序，包络用于检查贴合。
- 按已确认清单定位每款的源帧和图层。预览编号、身体子文件帧号和母版帧号分别记录。子文件筛选重排后，回读 Cel 核对实际源帧。

## 补位与验证

1. 按确认顺序处理，未指定顺序时按身体子文件源帧升序。沿用同源帧的已选眼嘴搭配，按[目标层补位规则](../aseprite-master-backfill/SKILL.md#身体部件与武器优先补位)找两个目标层都空闲的最早帧；只选其中一类时只查该层。没有合适空位才追加。重复回填前按名字表、完整图层路径及实际 Cel 核实已登记的款式和原位。
2. 用[独立 Cel 与 manifest 工具](../aseprite-equipment-from-preview/references/tools.md)提取所选部件。追加时 `copy_cels` 从母版第 1 帧复制下列 15 个固定身体 Cel，补位时只补齐其中缺失的 Cel 并保留同帧其他内容。`cels` 写入本帧选中的 `Eye_1` 或 `Mouth_1`。发型、帽子、衣裤、武器和未选部件在追加帧留空，保留原标签范围。

   ```text
   ArmUp_R
   ArmDown_R
   Plam_R
   Foot_R
   Calf_R
   Thigh_R
   Foot_L
   Calf_L
   Thigh_L
   Pelvis
   Body
   Head
   ArmUp_L
   ArmDown_L
   Plam_L
   ```

3. 保存后重新打开候选文件。仅追加时用 `verify through=N` 核对原有的 N 帧，有补位时逐帧检查其他既有 Cel。核对图层顺序、可见性、标签范围、所选部件及固定身体 Cel。按原生尺寸和最近邻整数倍检查眼睛、嘴巴与头部的贴合和遮挡。

## 名字登记与交付

沿用[通用素材名字表结构](../aseprite-master-backfill/SKILL.md#同步素材名字表)。每款登记一行，类别为 `身体部件`，类型为 `眼睛` 或 `嘴巴`，帧号为从 1 开始的实际母版帧号，图层路径为 `Eye_1` 或 `Mouth_1`。同帧眼嘴分别登记，固定身体 Cel 无需登记。素材名称使用用户确认的名称，只有编号时可暂记已核对的编号，并列出待命名项。

按[通用写回规则](../aseprite-master-backfill/SKILL.md#预览写回与核对)成对写回母版和名字表，交付文件路径、款式与母版帧号对应关系及合成预览。收到 Spine 导出请求时使用 [Aseprite 分件导出](../aseprite-spine-export/SKILL.md)。
