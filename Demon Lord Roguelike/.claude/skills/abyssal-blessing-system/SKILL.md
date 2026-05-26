---
name: abyssal-blessing-system
description: Demon Lord Roguelike 游戏的深渊馈赠(AbyssalBlessing)系统开发指南。使用此SKILL当需要创建或修改深渊馈赠配置、深渊馈赠 BUFF 加成、深渊馈赠等级替换、深渊馈赠 UI（选择界面/详情气泡/列表展示）、关卡间馈赠流程、深渊馈赠数据管理等，包括 AbyssalBlessingInfoBean、AbyssalBlessingEntityBean、UIFightAbyssalBlessing、UIViewAbyssalBlessingInfoContent、UIPopupAbyssalBlessingInfo、BuffHandler.AddAbyssalBlessing、buff_parent_id/buff_level 等级替换机制、Buff_AbyssalBlessingChange 事件等。
watched_files:
  - Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/
  - Assets/Scripts/Component/UI/Common/AbyssalBlessing/
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfoComponent.cs
  - Assets/Scripts/Component/Handler/BuffHandler.cs
  - Assets/Scripts/Component/Manager/BuffManager.cs
  - Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx
  - Assets/Resources/JsonText/AbyssalBlessingInfo.txt
  - Assets/Resources/JsonText/Language_AbyssalBlessingInfo_cn.txt
  - Assets/Resources/JsonText/Language_AbyssalBlessingInfo_en.txt
---

# 深渊馈赠系统开发指南

## 核心概念

深渊馈赠（Abyssal Blessing）是**征服模式**关卡间获得的全局增益系统：每通关一关后，玩家从随机生成的 3 个馈赠中选择 1 个（也可跳过），馈赠通过添加 BUFF 到防守核心（FightDefenseCore）作用于整局战斗，并在最终领奖后清空。

### 数据三件套

```
AbyssalBlessingInfoBean     - 馈赠配置数据（id / icon_res / buff_ids / name / details）
AbyssalBlessingEntityBean   - 馈赠运行时实例数据（abyssalBlessingUUID + AbyssalBlessingInfoBean）
BuffInfoBean                - 馈赠绑定的 BUFF 配置（通过 buff_ids 关联，可含等级链）
```

### 关键流程

```
[征服模式战斗结算]
     │
     ▼
GameFightLogicConquer.ActionForUIFightSettlementNext()
     │  非最后一关 → 打开 UIFightAbyssalBlessing
     │
     ▼
UIFightAbyssalBlessing.SetData()
     │  随机抽取 3 个 AbyssalBlessingInfoBean
     │  对每个馈赠：若 buff 有 buff_level，解析"当前等级+1"用于展示
     │
     ▼
玩家选择 / 跳过
     ├── 选择 → ActionForUIFightAbyssalBlessingSelect(info)
     │           ↓
     │       FightBeanForConquer.AddAbyssalBlessing(info)
     │           ↓
     │       new AbyssalBlessingEntityBean(info) → BuffHandler.AddAbyssalBlessing(entity)
     │           ↓
     │       事件 Buff_AbyssalBlessingChange → UIViewAbyssalBlessingInfoContent 刷新
     │
     └── 跳过 → ActionForUIFightAbyssalBlessingSkip → 直接 StartNextGame
     │
     ▼
[StartNextGame] → 进入下一关
     │
     ▼ （所有关卡通关后）
[ActionForUIRewardSelectEnd]
     │
     ▼
BuffHandler.Instance.manager.ClearAbyssalBlessing()  // 清理所有馈赠
```

## 关键架构（必读）

### 1. 与 BUFF 系统的关系

深渊馈赠**本身不实现 BUFF 逻辑**，所有效果通过 `buff_ids` 字段引用 BuffInfo 配置实现。具体 BUFF 实体类型 / 触发逻辑 / 属性管线 / 堆叠策略请参考 `buff-system` SKILL。

馈赠 BUFF 在 BuffManager 中走**独立容器**，不与战斗生物 BUFF 混用：

| 容器 | 类型 | 用途 |
|------|------|------|
| `manager.dicFightCreatureBuffsActivie` | DictionaryList<string, List<BuffBaseEntity>> | 战斗生物 BUFF（key=creatureUUID） |
| `manager.dicAbyssalBlessingBuffsActivie` | DictionaryList<AbyssalBlessingEntityBean, List<BuffBaseEntity>> | 深渊馈赠 BUFF（key=馈赠实例） |

馈赠 BUFF 作用目标统一为**防守核心**（CreatureFightTypeEnum.FightDefenseCore），施加者与目标 UUID 都填核心 UUID。

### 2. 等级 BUFF 替换机制（重点）

馈赠中的 BUFF 支持等级链，由 `BuffInfoBean.buff_parent_id` + `buff_level` 定义：

```
buff_parent_id = 30001                 // 共享同一族
buff_level     = 1, 2, 3, ...           // 等级递增
```

`BuffHandler.AddAbyssalBlessing` 的核心替换逻辑：

```csharp
// 拿到馈赠配置中的 buff_ids 第 i 个
BuffInfoBean buffInfo = BuffInfoCfg.GetItemData(buffId);
if (buffInfo != null && buffInfo.buff_level > 0)
{
    long parentId = buffInfo.buff_parent_id;
    // 1. 查询已有最高等级
    int currentLevel = GetAbyssalBlessingCurrentLevel(parentId);
    // 2. 移除整个旧条目（含该 parent 下的所有 BUFF）
    RemoveAbyssalBlessingByParentId(parentId);
    // 3. 解析下一级 BUFF
    BuffInfoBean nextLevelBuffInfo = BuffInfoCfg.GetBuffByParentAndLevel(parentId, currentLevel + 1);
    if (nextLevelBuffInfo != null) buffId = nextLevelBuffInfo.id;
}
BuffBean buffData = new BuffBean(buffId);
var buffEntity = manager.GetBuffEntity(buffData, defenseCoreUUID, defenseCoreUUID);
```

**关键点**：
- UI 展示侧（`UIFightAbyssalBlessing.SetData`）通过 `BuffHandler.Instance.GetAbyssalBlessingCurrentLevel(parentId)` 主动解析"下一级"的 BUFF，让玩家看到的就是"将要获得"的等级。
- 没有等级的 BUFF（`buff_level == 0`）走原 buffId，不做替换。
- `GetBuffByParentAndLevel` 在 `BuffInfoBeanPartial.cs` 中实现，按 `(parentId, level)` 缓存。

### 3. 事件通信

```
EventsInfo.Buff_AbyssalBlessingChange   // BUFF系统-深渊馈赠变化
```

- **触发处**：`BuffHandler.AddAbyssalBlessing` 末尾 → `EventHandler.TriggerEvent(Buff_AbyssalBlessingChange, abyssalBlessingEntityData)`
- **监听处**：`UIViewAbyssalBlessingInfoContent.Awake` 注册 → 在面板可见时刷新列表

### 4. 数据生命周期

| 时机 | 操作 |
|------|------|
| 关卡间选择馈赠 | `FightBeanForConquer.AddAbyssalBlessing()` → 添加 BUFF |
| 切换关卡（进入战斗） | 馈赠 BUFF 留存（防守核心一直存在） |
| 通关全部关卡（领奖结束） | `BuffHandler.Instance.manager.ClearAbyssalBlessing()` 清空所有馈赠 |
| 战斗过程 | `BuffHandler.UpdateData` 同时驱动馈赠 BUFF 的 `UpdateBuffTime` |

⚠️ 馈赠 BUFF 不会随单关结束自动销毁，**ClearAbyssalBlessing 只能在最终领奖完成后调用**。

### 5. UI 组件层级

```
UIFightAbyssalBlessing                          # 关卡间选择界面（征服模式触发）
├── ui_AbyssalBlessingList                      # 滚动列表（3 个候选）
│   └── UIViewFightAbyssalBlessingItem         # 单个馈赠候选项
└── ui_SkipBtn                                  # 跳过按钮

UIViewAbyssalBlessingInfoContent                # 战斗界面常驻列表（已选馈赠展示）
└── UIViewAbyssalBlessingInfoContentItem       # 单项（含 popup 触发）
    └── ui_Icon_PopupButtonCommonView          # 点击 → PopupEnum.AbyssalBlessingInfo

UIPopupAbyssalBlessingInfo : PopupShowCommonView # 馈赠详情气泡
```

## 创建新馈赠

### 1. Excel 配置（唯一真实源）

修改 `Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx`：

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 馈赠唯一 ID |
| `icon_res` | string | 图标资源名 |
| `buff_ids` | string | 关联 BUFF ID 列表，多个用 `,` 分隔 |
| `name` | long | 名字文本 ID（多语言） |
| `details` | long | 描述文本 ID（多语言） |
| `remark` | string | 备注（仅文档用途） |

修改完 Excel 后必须在 Unity 编辑器中运行 `ExcelEditorWindow` 导出工具重新生成 `Assets/Resources/JsonText/AbyssalBlessingInfo.txt`，否则配置不会生效。

### 2. 配套 BUFF（如需新效果）

若馈赠引用的 BUFF 不存在或需要新效果：
- 添加/修改 `excel_buff_info` 配置，参考 `buff-system` SKILL
- 选择合适的 `class_entity`（属性 / 条件 / 周期 / 即时）
- 等级 BUFF 必须配 `buff_parent_id` + `buff_level`，连续递增

### 3. 多语言文本

在 `Language_AbyssalBlessingInfo_cn.txt` / `_en.txt` 添加对应 `name` / `details` 文本 ID 的翻译。

### 4. 图标资源

深渊馈赠图标统一从 **专用图集** `Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas` 加载：

- 图集 tag 由 `SpriteAtlasTypeEnum.AbyssalBlessing` 映射为 `AtlasForAbyssalBlessing`（约定：`AtlasFor{枚举名}`）。
- `icon_res` 字段填写 Sprite 在该图集中的名称（不带后缀）。
- 加载入口统一走 `IconHandler.Instance.SetAbyssalBlessingIcon(iconName, image)`，**禁止再走 `SetUIIcon`**（那会去 `AtlasForUI` 找图集，找不到时 fallback 到 `icon_unknow`）。
- 新增馈赠图标后必须将 Sprite 拖入 `AtlasForAbyssalBlessing.spriteatlas` 的 Packables 列表。

## 使用深渊馈赠系统

### 触发选择界面（已由 GameFightLogicConquer 处理）

```csharp
var uiFightAbyssalBlessing = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
uiFightAbyssalBlessing.SetData(
    actionForSelect: ActionForUIFightAbyssalBlessingSelect,  // 选择回调
    actionForSkip:   ActionForUIFightAbyssalBlessingSkip);   // 跳过回调
```

### 测试模式（指定 ID 直接展示 UI）

`UIFightAbyssalBlessing.SetDataForTest(long[] ids, ...)` 不走随机，直接按传入的 ID 列表展示（最多 SHOW_NUM=3 个，无效 ID 自动跳过）。配套入口：

- **Editor 入口**：`LauncherTest` 上选 `TestSceneTypeEnum.AbyssalBlessing` → Inspector 用"➕ 添加馈赠"按钮逐行填入 ID（带每行删除 / 移除最后一个 / 打开配置表按钮）→ 点"打开深渊馈赠 UI"。EditorPrefs 持久化（key 前缀 `GameTestEditor_abyssalBlessingTestIds`）。
- **代码入口**：`LauncherTest.StartForAbyssalBlessingUI(List<long> ids)`。

```csharp
// 直接调用示例
var ui = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
ui.SetDataForTest(new long[] { 1000001001, 1000001002 },
    info => LogUtil.Log($"选 {info.id}"),
    () => LogUtil.Log("跳过"));
```

> SetData 和 SetDataForTest 共用同一个私有 `SetDataInternal` 渲染管线，等级 BUFF 解析（含 fallback 到 level 1）、出现动画、点击动画、ScreenLock 遮罩等行为完全一致。

### 程序化添加馈赠（测试 / 特殊流程）

```csharp
AbyssalBlessingInfoBean info = AbyssalBlessingInfoCfg.GetItemData(1000001001);
AbyssalBlessingEntityBean entity = new AbyssalBlessingEntityBean(info);
BuffHandler.Instance.AddAbyssalBlessing(entity);
// 内部会：
// 1. 找到防守核心 UUID
// 2. 解析 buff_ids（带等级替换）
// 3. 添加到 manager.dicAbyssalBlessingBuffsActivie
// 4. 触发 Buff_AbyssalBlessingChange 事件
```

### 查询已有馈赠

```csharp
// 全部馈赠
var allAbyssalBlessing = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
foreach (var key in allAbyssalBlessing.ListKey)
{
    AbyssalBlessingEntityBean entity = key;
    // ...
}

// 查询某父级 BUFF 的当前等级（0=未拥有）
int currentLevel = BuffHandler.Instance.GetAbyssalBlessingCurrentLevel(parentId);

// 获取某馈赠实例对应的所有 BUFF
var buffs = BuffHandler.Instance.manager.GetAbyssalBlessingBuffsActivie(entity);
```

### 清空馈赠

```csharp
// 只在征服全通关 + 领奖结束后调用
BuffHandler.Instance.manager.ClearAbyssalBlessing();
```

### 监听馈赠变化

```csharp
this.RegisterEvent<AbyssalBlessingEntityBean>(
    EventsInfo.Buff_AbyssalBlessingChange,
    EventForAbyssalBlessingChange);

public void EventForAbyssalBlessingChange(AbyssalBlessingEntityBean entity)
{
    // 刷新 UI
}
```

## UI 开发模板

### 馈赠候选项（UIFightAbyssalBlessing 子项）

```csharp
public partial class UIViewFightAbyssalBlessingItem : BaseUIView
{
    public AbyssalBlessingInfoBean abyssalBlessingInfo;

    public void SetData(AbyssalBlessingInfoBean info, BuffInfoBean resolvedBuffInfo = null)
    {
        this.abyssalBlessingInfo = info;
        // 带等级的 BUFF 优先展示已解析的下一级文本
        if (resolvedBuffInfo != null)
        {
            SetName(resolvedBuffInfo.name_language);
            SetDetails(resolvedBuffInfo.content_language);
        }
        else
        {
            SetName(info.name_language);
            SetDetails(info.details_language);
        }
    }

    public void OnClickForSelect()
    {
        var ui = UIHandler.Instance.GetUI<UIFightAbyssalBlessing>();
        ui.OnClickForSelect(abyssalBlessingInfo);
    }
}
```

### 战斗内常驻列表（UIViewAbyssalBlessingInfoContent）

`OnEnable` 时拉取 `dicAbyssalBlessingBuffsActivie.ListKey`，按 key 数量复用/实例化 `UIViewAbyssalBlessingInfoContentItem`，并注册 `Buff_AbyssalBlessingChange` 事件按需刷新。

### 详情气泡（UIPopupAbyssalBlessingInfo）

继承 `PopupShowCommonView`，`SetData` 接收 `AbyssalBlessingEntityBean`，从其 `abyssalBlessingInfo` 显示图标 / 名字 / 详情。`PopupButtonCommonView.SetData(entity, PopupEnum.AbyssalBlessingInfo)` 触发。

### 图标加载统一入口

所有展示馈赠图标的 UI（候选项 / 常驻项 / 详情气泡）一律调用：

```csharp
IconHandler.Instance.SetAbyssalBlessingIcon(info.icon_res, ui_Icon);
```

该方法内部使用 `SpriteAtlasTypeEnum.AbyssalBlessing` → `AtlasForAbyssalBlessing` 图集；找不到图标时自动 fallback 到 `icon_unknow`（公共未知图标）。

## 关键文件速查

| 功能 | 文件路径 |
|------|----------|
| 馈赠配置 Bean（自动生成，禁改） | `Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs` |
| 馈赠配置扩展 | `Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs` |
| 馈赠运行时实例 | `Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs` |
| Excel 源表 | `Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx` |
| 导出 JSON | `Assets/Resources/JsonText/AbyssalBlessingInfo.txt` |
| 多语言 | `Assets/Resources/JsonText/Language_AbyssalBlessingInfo_{cn,en}.txt` |
| BUFF 添加入口 | `Assets/Scripts/Component/Handler/BuffHandler.cs`（AddAbyssalBlessing 区域） |
| BUFF 容器 | `Assets/Scripts/Component/Manager/BuffManager.cs`（dicAbyssalBlessingBuffsActivie） |
| 征服模式流程 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs` |
| 数据持有 | `Assets/Scripts/Bean/Game/FightBeanForConquer.cs`（AddAbyssalBlessing） |
| 选择界面 | `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs` |
| 选择项 | `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIViewFightAbyssalBlessingItem.cs` |
| 常驻列表 | `Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContent.cs` |
| 常驻项 | `Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContentItem.cs` |
| 详情气泡 | `Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs` |
| 图集 | `Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas` |
| 图集枚举 | `Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs`（SpriteAtlasTypeEnum.AbyssalBlessing） |
| 图标加载入口 | `Assets/Scripts/Component/Handler/IconHandler.cs`（SetAbyssalBlessingIcon） |
| Prefab | `Assets/Resources/UI/UIFightAbyssalBlessing.prefab` 等 |

## 约束

- **Excel 是唯一真实源**：任何馈赠配置变更（新增/修改/删除）必须改 Excel，再用 Unity 编辑器导出 JSON。仅改 JSON 会在下次导出被覆盖丢失。
- **`AbyssalBlessingInfoBean.cs` 是自动生成的**，禁止直接修改；扩展逻辑写到 `AbyssalBlessingInfoBeanPartial.cs`。
- **不要直接操作 `manager.dicAbyssalBlessingBuffsActivie`** 写入数据，必须经过 `BuffHandler.AddAbyssalBlessing`，否则会跳过等级替换逻辑与事件通知。
- **等级 BUFF 的 `buff_parent_id` + `buff_level` 必须连续递增**（1, 2, 3, ...），否则升级链会断裂；`GetBuffByParentAndLevel` 找不到下一级时不会创建任何 BUFF。
- **UI 选择界面随机展示 3 个**（`showNum = 3`），多于/少于 3 个的展示需要改 `UIFightAbyssalBlessing.SetData`；候选生成无去重机制（可能重复抽到同一 ID）。
- **馈赠目标固定为防守核心**，施加者也是核心 UUID。如需作用其他生物，需要修改 `BuffHandler.AddAbyssalBlessing`。
- **`ClearAbyssalBlessing` 只能在征服全通关 + 领奖结束后调用**（已在 `ActionForUIRewardSelectEnd` 中处理），中途调用会导致玩家选择的馈赠丢失。
- **馈赠 BUFF 与战斗生物 BUFF 走独立 Update**，添加新 BUFF 类型时确保其 `UpdateBuffTime` / 事件订阅 / 池化路径都支持馈赠场景下的多帧生存。
- **馈赠图标必须放入 `AtlasForAbyssalBlessing.spriteatlas`**，加载只能走 `IconHandler.Instance.SetAbyssalBlessingIcon`，不要再用 `SetUIIcon`（会去 UI 图集找，丢图后只能显示未知图标）。

## 常见坑

1. **新增馈赠后游戏中查不到** → 多半是只改了 JSON 未改 Excel，下次导出被覆盖。统一从 Excel 改。
2. **等级 BUFF 升级到某级不显示了** → 检查 `GetBuffByParentAndLevel(parentId, currentLevel+1)` 是否能找到记录，等级链是否断裂。
3. **UI 显示成"1 级"但实际加了 2 级** → 选择界面解析的是"下一级"展示，确认 `UIFightAbyssalBlessing.SetData` 内调用的是 `GetAbyssalBlessingCurrentLevel + 1`。
4. **`UIViewAbyssalBlessingInfoContent` 不更新** → 检查 `OnEnable` 是否调用了 `RefreshUIData`，事件回调是否做了 `activeInHierarchy` 守卫导致首次开启错过事件。
5. **跨关切换馈赠被清空** → `ClearAbyssalBlessing` 提前调用（仅最终领奖才能调）。
6. **多个馈赠引用同一 `buff_parent_id`** → 新的会替换旧的整条 entry（含其他无关 BUFF），需要避免一个馈赠的 buff_ids 同时含多个不同 parent 的等级 BUFF。
7. **馈赠图标显示"未知"** → 多半是 Sprite 未加入 `AtlasForAbyssalBlessing.spriteatlas`，或者代码里调用了 `SetUIIcon` 走错图集。统一走 `SetAbyssalBlessingIcon`。
