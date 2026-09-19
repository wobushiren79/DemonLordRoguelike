---
name: mod-system
description: Demon Lord Roguelike 游戏的Mod系统开发指南。使用此SKILL当需要创建或修改Mod加载、Mod资源管理、ModID映射、Mod配置覆盖等，包括Mod目录结构、Catalog加载、资源异步/同步加载、JsonText扩展等。
watched_files:
  - Assets/FrameWork/Scripts/Component/Manager/ModManager.cs
  - Assets/FrameWork/Scripts/Component/Handler/ModHandler.cs
  - Assets/FrameWork/Scripts/Bean/ModIdMapBean.cs
  - Assets/FrameWork/Scripts/Bean/BaseBean.cs
  - Assets/FrameWork/Scripts/Component/Manager/GameDataManager.cs
---

# Mod系统开发指南

## 核心概念

### Mod数据结构

```
ModManager          - Mod管理器（资源加载、缓存、卸载）
ModHandler          - Mod处理器（逻辑接口层）
ModIdMapBean        - ModID映射数据（modName -> modId）
```

### Mod系统架构

```
Mods/                          - Mod根目录（与Assets同级）
├── Spine/                     - 示例Mod目录
│   ├── catalog.bin            - Addressables Content Catalog
│   ├── catalog.hash           - Catalog哈希
│   ├── *.bundle               - 资源Bundle文件
│   ├── settings.json          - Mod设置
│   └── JsonText/              - 可选：配置覆盖文件夹
│       └── *.txt              - 覆盖游戏的Json配置
```

### ModID映射体系

```
ModIdMapBean                      - Mod名称到modId的映射
├── modIdMap (Dictionary<string, int>)
ModIdMapService                   - 数据持久化服务 (BaseDataService<ModIdMapBean>)
```

每个已加载的Mod会被分配一个唯一的 `modId`（1~`ModManager.MaxModId`=92232），用于：
- 区分不同Mod的资源，避免ID冲突（配置行 id 改写为 `modId*10^14 + 原id`，见 BaseBean.CombineModId）
- Mod新增时不会导致旧Mod的ID变化（持久化存储在 `persistentDataPath/ModIdMap`，**全局一份，不按存档槽位区分**）
- 超过 92232 上限时新 Mod **拒绝分配 ID**：打错误日志、不进入映射、其 JsonText 配置不合并（资源仍可按 modName 加载）

## Mod目录结构

### 创建新Mod

```
Mods/YourModName/
├── catalog.bin            - 必需：Addressables生成的Catalog文件
├── *.bundle               - 必需：资源Bundle文件
└── JsonText/              - 可选：配置覆盖目录
    └── CreatureInfo.txt   - 示例：覆盖生物配置
```

### Mod根目录路径

```csharp
// 编辑器与打包后路径一致：与 Assets / GameName_Data 同级的 Mods 目录
string modsRoot = Path.Combine(Application.dataPath, "..", "Mods");
```

## 初始化与加载Mod

### 初始化所有Mod

```csharp
// 方式1：异步回调
ModHandler.Instance.InitializeAllMods((success) =>
{
    LogUtil.Log($"Mod初始化结果: {success}");
});

// 方式2：异步await
bool success = await ModHandler.Instance.InitializeAllModsAsync();

// 方式3：同步（仅在必要时使用）
bool success = ModHandler.Instance.InitializeAllModsSync();
```

### 加载单个Mod Catalog

```csharp
// 异步回调
ModHandler.Instance.LoadModCatalog("Spine", (success) =>
{
    if (success)
        LogUtil.Log("Mod加载成功");
});

// 异步await
bool success = await ModHandler.Instance.LoadModCatalogAsync("Spine");

// 同步
bool success = ModHandler.Instance.LoadModCatalogSync("Spine");
```

## 加载Mod资源

### 同步加载单个资源

```csharp
// 加载Spine动画数据
SkeletonDataAsset skeletonData = ModHandler.Instance.LoadAssetSync<SkeletonDataAsset>(
    "Spine", 
    "amelia_skeletondata"
);
```

### 异步加载单个资源

```csharp
// 方式1：回调
ModHandler.Instance.LoadAsset<Sprite>("Spine", "icon_amelia", (sprite) =>
{
    if (sprite != null)
        image.sprite = sprite;
});

// 方式2：await
Sprite sprite = await ModHandler.Instance.LoadAssetAsync<Sprite>("Spine", "icon_amelia");
```

### 批量加载资源（通过Label）

```csharp
ModHandler.Instance.LoadAssets<GameObject>("Spine", "characters", (assets) =>
{
    foreach (var asset in assets)
    {
        LogUtil.Log($"加载资源: {asset.name}");
    }
});
```

## 卸载与释放

### 卸载单个Mod

```csharp
// 卸载Mod的所有资源和Catalog
ModHandler.Instance.UnloadMod("Spine");
```

### 卸载所有Mod

```csharp
ModHandler.Instance.UnloadAllMods();
```

### 释放单个资源

```csharp
// 释放指定Mod的单个资源（从缓存中移除并释放句柄）
ModHandler.Instance.ReleaseAsset("Spine", "icon_amelia");
```

### 释放批量资源

```csharp
// 释放通过Label加载的批量资源
ModHandler.Instance.ReleaseAssets("Spine", "characters");
```

## 查询Mod信息

### 检查Mod状态

```csharp
// 检查Mod是否已加载
bool loaded = ModHandler.Instance.IsModLoaded("Spine");

// 获取所有已加载的Mod名称
List<string> loadedMods = ModHandler.Instance.GetLoadedModNames();

// 获取所有可用的Mod名称（存在catalog.bin的目录）
List<string> availableMods = ModHandler.Instance.GetAvailableModNames();
```

### 资源归属查询

```csharp
// 判断指定assetKey是否属于某个已加载的Mod
bool isModAsset = ModHandler.Instance.IsModAsset("amelia_skeletondata");

// 获取包含指定assetKey的Mod名称
string modName = ModHandler.Instance.GetModNameForAsset("amelia_skeletondata");
```

### 获取ModID

```csharp
// 获取指定Mod的modId（1~ModManager.MaxModId=92232），未分配（或超限被拒）则返回1
int modId = ModManager.Instance.GetModId("Spine");
```

### 获取Mod目录路径

```csharp
string modPath = ModHandler.Instance.GetModPath("Spine");
// 返回: .../Mods/Spine
```

## JsonText配置覆盖

Mod可以通过 `JsonText` 目录覆盖游戏的配置数据。

### 文件结构

```
Mods/YourModName/JsonText/
├── CreatureInfo.txt       - 覆盖生物基础配置
├── CreatureModelInfo.txt  - 覆盖生物模型配置
├── ItemsInfo.txt          - 覆盖道具配置
└── ...
```

### 合并机制（BaseCfg.GetInitDataForMods）

任意走 `BaseCfg.GetInitData(fileName)` 加载的配置表都会被 Mod 同名 JsonText 扩展：主游戏 `Resources/JsonText/{fileName}.txt` 先加载，再追加各 Mod `JsonText/{fileName}.txt` 的行。**每行只做两件事**：

1. `bean.id = CombineModId(modId, bean.id)`（`modId*10^14 + 自ID`，见 ModID 分配规则）
2. `bean.CombineModReferenceIds(modId)`（`BaseBean` virtual 钩子，默认无操作）——把「指向 Mod 自带配置的引用字段」按同一 modId 拼接。**重写不由手写**：`ExcelEditorWindow.CreateEntity` 生成 `*Bean.cs` 时，凡 Excel 列头带 `[language]`/`[language_1]`/`[language_2]`/`[mode_id]` 标记的字段（long/int 类型）自动生成本重写（`if (field > 0) field = XxxCfg.CombineModId(modId, field);`；0=无引用约定不拼接）。例如 ItemsInfo 的 `name[language]` 列 → Mod 道具名自动指向 Mod 自带语言表 `Language_ItemsInfo_{lang}.txt` 的同自ID 行（约定：name 自ID = 道具自ID；name=0 不拼接显示空文本；**Mod 道具不能复用主游戏 textId**——拼接后必指向 Mod 语言表）。**新增需要拼接的引用列时，只需在 Excel 列头加 `[mode_id]`/`[language]` 标记并重新生成该表 Entity**，无需写任何代码。注意：带标记的列在 Mod 行里一律视为 Mod 本地引用；想引用主游戏配置的行就不要给该列加标记。

多语言表同样可扩展（`LanguageCfg` 也走 `GetInitData`，文件名形如 `Language_ItemsInfo_cn`），Mod 需提供全部 12 语言文件（cn/en/jp/kr/tw/de/fr/ru/es/br/pl/tr），缺失语言的玩家会看到 `Error:{id}`。

> 道具型 Mod 的完整生产范式（资源约定/ID规则/生成脚本/构建部署）见 **aeonsecho-spine-mod** Skill（AeonsEchoSpine=首个道具型 Mod 实例，其流程不自动适用于其他 Mod）。

### 查询JsonText文件

```csharp
// 检查是否有Mod包含指定名称的JsonText文件
bool hasFile = ModManager.Instance.HasModJsonTextFile("CreatureInfo");

// 获取包含指定fileName的所有Mod信息
var fileInfos = ModManager.Instance.GetModJsonTextFileInfos("CreatureInfo");
foreach (var (modId, modName, filePath) in fileInfos)
{
    LogUtil.Log($"Mod[{modName}] ID={modId} 路径={filePath}");
}
```

## ModID映射持久化

### 获取/保存ModID映射

```csharp
// 通过GameDataManager获取ModID映射
ModIdMapBean modIdMap = GameDataHandler.Instance.manager.GetModIdMap();

// 遍历所有已记录的ModID
foreach (var kvp in modIdMap.modIdMap)
{
    LogUtil.Log($"Mod: {kvp.Key} -> ID: {kvp.Value}");
}

// 保存ModID映射（通常由系统自动管理）
GameDataHandler.Instance.manager.SaveModIdMap();
```

### ModID分配规则

1. ModID按Mod名称字母顺序分配
2. 已分配ID的Mod持久化存储（`persistentDataPath/ModIdMap`，全局一份、不按存档槽位区分），跨会话保持不变
3. 新增Mod分配第一个空闲ID（从1开始递增）
4. 卸载Mod不会删除其ID映射，保证重新加载时ID不变
5. 上限为 `ModManager.MaxModId = 92232`（由 BaseBean.CombineModId 的 `modId*10^14 + selfId` 不溢出 long 推出）；超限的新 Mod 拒绝分配 ID、JsonText 配置不合并（`GetModJsonTextFileInfos` 跳过未分配映射的 Mod），资源仍可按 modName 加载

## 常用代码模板

### 在UI中显示Mod资源图片

```csharp
ModHandler.Instance.LoadAsset<Sprite>(modName, assetKey, (sprite) =>
{
    if (sprite != null)
    {
        imgIcon.sprite = sprite;
        imgIcon.SetNativeSize();
    }
});
```

### 加载Mod Spine动画

```csharp
SkeletonDataAsset skeletonData = ModHandler.Instance.LoadAssetSync<SkeletonDataAsset>(
    modName, 
    $"{characterName}_skeletondata"
);

if (skeletonData != null)
{
    skeletonGraphic.skeletonDataAsset = skeletonData;
    skeletonGraphic.Initialize(true);
}
```

### 安全加载Mod资源（带fallback）

```csharp
public Sprite GetIcon(string modName, string assetKey)
{
    if (ModHandler.Instance.IsModLoaded(modName))
    {
        var sprite = ModHandler.Instance.LoadAssetSync<Sprite>(modName, assetKey);
        if (sprite != null)
            return sprite;
    }
    // Fallback到游戏内置资源
    return LoadInternalIcon(assetKey);
}
```

### 初始化时加载所有Mod并执行后续操作

```csharp
public void StartGameWithMods()
{
    ModHandler.Instance.InitializeAllMods((success) =>
    {
        if (success)
        {
            var loadedMods = ModHandler.Instance.GetLoadedModNames();
            LogUtil.Log($"已加载 {loadedMods.Count} 个Mod");
            
            // 继续初始化游戏...
            InitGameData();
        }
    });
}
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| Mod管理器 | `Assets/FrameWork/Scripts/Component/Manager/ModManager.cs` |
| Mod处理器 | `Assets/FrameWork/Scripts/Component/Handler/ModHandler.cs` |
| ModID映射Bean | `Assets/FrameWork/Scripts/Bean/ModIdMapBean.cs` |
| Mod配置合并（id/引用拼接） | `Assets/FrameWork/Scripts/Bean/BaseBean.cs`（`BaseCfg.GetInitDataForMods`/`CombineModId`/`BaseBean.CombineModReferenceIds`） |
| ModID映射服务 | `Assets/FrameWork/Scripts/Component/Manager/GameDataManager.cs` (内联使用 BaseDataService<ModIdMapBean>) |
| 游戏数据管理器 | `Assets/FrameWork/Scripts/Component/Manager/GameDataManager.cs` |
| Mod根目录 | `项目根目录/Mods/` |
