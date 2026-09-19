---
name: aeonsecho-spine-mod
description: AeonsEchoSpine Mod（回响幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描AeonsEcho spine资源套装、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定、道具ID规则、other_data组合格式（Chess+Avator/ui_show_spine）、生成脚本与一键构建的完整步骤。目前仅适用于AeonsEchoSpine这一个Mod，其他Mod流程可能不同。
watched_files:
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Utils/GameUIUtil.cs
  - Assets/FrameWork/Scripts/Bean/BaseBean.cs
  - Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs
  - Assets/FrameWork/Scripts/Utils/ExcelUtil.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - .claude/scripts/gen_aeonsecho_spine_mod.py
---

# AeonsEchoSpine Mod（回响幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`AeonsEchoSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：341 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ AeonsEcho 系列 spine 资源
- **MOD 项目**：独立于主游戏的 Unity 工程（示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**），负责存放 spine 源资源与构建 Addressables 产物
- **主项目**：Demon Lord Roguelike（本仓库），负责消费 Mod（幻化解析、道具合并、UI 展示）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/AeonsEcho/
├── 1101/                          - 资源套装（数字文件夹名=套装号）
│   ├── Chess/                     - 基础 spine（世界/战斗/普通卡片形象），每个文件夹一个变体
│   ├── Chess_s01/                 - Chess 变体（s01/s02...）
│   ├── Avator_lv1/                - ui_show_spine 高清图（详情UI形象），每个文件夹一个变体
│   ├── Avator_lv1a/               - Avator 变体（lv1/lv1a/lv2/lv2a/lv3...）
│   └── ...
├── 851101/  (仅 Chess → 每 Chess 出 1 个道具)
└── 4001/    (仅 Avator 无 Chess → 跳过，幻化药必须以 Chess 为基础形象)
```

- 每个变体文件夹内含完整 spine 导出物：`{套装号}_{变体名}_SkeletonData.asset` + `_Atlas.asset` + `.json/.atlas.txt/.png/.mat`
- **资源名 = SkeletonData 资产文件名（不含扩展名）**，如 `1101_Chess_s01_SkeletonData`；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）
- 非 Chess/Avator 命名的文件夹（如 Elf、Secretary）不参与幻化药生成

## 道具生成规则

- **组合**：套装内 Chess × Avator 笛卡尔积。如套装有 Chess/Chess_s01 + Avator_lv1/Avator_lv2 → 4 个道具（Chess×Avator_lv1、Chess×Avator_lv2、Chess_s01×Avator_lv1、Chess_s01×Avator_lv2）；无 Avator 时每个 Chess 单独出 1 个
- **道具自ID**（Mod JsonText 内的原始 id，运行时由 BaseCfg.CombineModId 拼 modId 前缀）：`18`(道具类型) + `套装号` + `2位序号`(01 起，Chess 优先外层排序)。例：套装 1101 → 18110101~18110120；套装 851101 → 1885110101
- **other_data 组合格式**：`{chessRes}` 或 `{chessRes},{avatorRes}|{uiScale};{uiX},{uiY}`
  - chess 段：基础形象（必填）
  - avator 段：ui_show_spine 高清展示（详情UI，isUIShow=true 时使用；可空）
  - `|` 后第三段：详情UI尺寸，格式同 `CreatureModelBean.ui_data_b`（`scale;x,y`）；生成器按 `430/Avator骨架高` 校准 scale（与主游戏 Other 角色 ui_data_b 同量级），默认位移 `0,-215`
- **name 自ID = 道具自ID**（约定）：指向 Mod 自带语言表同 id 行；运行时拼接由 **Excel 列头 `name[language]` 标记驱动**——`ExcelEditorWindow.CreateEntity` 生成 `ItemsInfoBean.cs` 时自动产出 `CombineModReferenceIds` 重写（无标记/0 值不拼接）。**Mod 道具不支持复用主游戏 textId**（会被拼接后查不到），空文本用 name=0
- **固定字段**：item_type=18、num_max=1（不堆叠）、icon_res=`Item_TransformPotion_1`（复用主游戏内置图标）、creature_model_id=0、reward_rarity=""（消耗品不进装备奖励池——奖励生成按 creature_model_id 过滤，0 型道具天然不进池）
- **道具名**：cn「幻化药·回响{套装}-{序号}」/ tw「幻化藥·迴響…」/ 其他语言「Echo Potion …」（12 语言全生成）

## 主项目配套代码（消费侧）

| 机制 | 位置 |
|------|------|
| other_data 三段解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`） |
| 基础形象（chess 段） | `CreatureBeanPartial.GetTransformSpineRes` |
| 高清展示（avator 段） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支） |
| 详情UI尺寸（第三段） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`（替代原生物 ui_data_b） |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` 钩子（由生成器按列头 `name[language]` 标记自动重写进 `ItemsInfoBean.cs`） |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod`（assetName 命中已加载 Mod 的 catalog key 时走 Mod 路径） |

## 生成流程（完整步骤）

### 0. 前置（仅机制变更时需要）：重新生成 ItemsInfo Entity

Mod 道具 `name` 的 modId 拼接代码在 `ItemsInfoBean.cs` 里**由生成器产出**（列头 `name[language]` 标记驱动）。若 `ItemsInfoBean.cs` 还没有 `CombineModReferenceIds` 重写（如本机制刚上线/刚加标记列），先在主项目 Unity 里对 `excel_items_info[道具信息].xlsx` 执行「生成 Entity」（ExcelEditorWindow），确认生成结果包含：

```csharp
public override void CombineModReferenceIds(int modId)
{
    if (name > 0) name = ItemsInfoCfg.CombineModId(modId, name);
}
```

### 1. 生成 JsonText 配置（在主项目侧执行）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_aeonsecho_spine_mod.py" --mod-project "<MOD项目根目录>"
```

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/AeonsEchoSpine/JsonText/
├── ItemsInfo.txt                    - 341 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数：`--ui-scale-k 430`（详情UI缩放校准常数）、`--ui-pos-y -215`（详情UI默认Y偏移）。资源套装新增/变更后**重跑本脚本即可**（幂等全量重生成）。

### 2. 构建 Addressables 产物（在 MOD 项目的 Unity 编辑器里执行）

菜单：**工具/Mod/AeonsEchoSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/AeonsEchoSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/AeonsEcho` 全部 `SkeletonDataAsset` → 同步进 `Mod_AeonsEchoSpine` 分组（先清空再全量加，Address=资产名；旧空分组 `Mod_AeonsEcho` 自动改名复用）
2. 临时把其他分组 `IncludeInBuild=false`（隔离构建，只出本 Mod 的 bundle），构建后自动恢复
3. 新增/复用 Profile `AeonsEchoSpine`，构建/加载路径指向 `Mods/AeonsEchoSpine` 并设为激活
4. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
5. 产物：`Mods/AeonsEchoSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`

另有「仅同步分组条目」菜单用于先检查条目列表再构建。

### 3. 部署到主项目（在主项目侧执行）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_aeonsecho_spine_mod.py" --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

校验 `Mods/AeonsEchoSpine/catalog.bin` 存在后，整体覆盖拷贝到 `主项目/Mods/AeonsEchoSpine/`（先删旧目录）。

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动（含 `ModHandler.Instance.InitializeAllModsSync()`）
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 18110101))`（modId 用 `ModManager.Instance.GetModId("AeonsEchoSpine")` 取，通常首个新 Mod=1 或 2）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·回响1101-01」
4. 确认后：卡片详情 UI 显示 **Avator 高清图**（比例正常不裁切）；基地/战斗场景显示 **Chess 形象**
5. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Echo Potion 1101-01」
6. 移除 `Mods/AeonsEchoSpine` 目录重启 → 已幻化生物自动回落原形象（每 id 一次 LogError 属预期），幻原药仍可清残留

## 注意与边界

- **本流程仅适用于 AeonsEchoSpine**：其他 Mod（Nikke/BrownDust 等分组）资源约定与道具规则可能不同，不套用本文档
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- 套装内组合数 >99 时 2 位序号溢出，生成器会告警截断（当前最多 20 组合，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存的是拼接后的完整 id（含 modId 前缀）：Mod 移除→id 查不到配置→自动回落原形象；Mod 重装且 modId 不变（持久化映射）→自动恢复
