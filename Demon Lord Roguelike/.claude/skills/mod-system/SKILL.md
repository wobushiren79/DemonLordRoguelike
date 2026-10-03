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
Mods/                          - Mod根目录（与Assets同级；已加入仓库根 .gitignore，本地目录不上传 git，分发靠拷贝）
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

> **统一初始化点**：`BaseLauncher.Launch()` 首行调用 `InitializeAllModsSync()`（LauncherGame/LauncherTest 均经 `base.Launch()` 覆盖，正式包与测试场景同路径）——必须在 `TextHandler.InitData()` 及任何 Cfg 首次访问之前，否则 Mod 的 JsonText（含 `Language_*` 多语言）合并不生效。已加载的 Mod 重复初始化会被 `IsModLoaded` 早退，重复调用安全。

> **Mod 开关（启动过滤）**：三个 `InitializeAllMods*` 变体内部统一经 `ModManager.FilterEnabledMods` 过滤——以 `GameConfigBean.listModEnable`（List<string>，存开启的 Mod 名）为准，**未记录的 Mod（含新出现的 Mod）默认关闭不加载**；`GetAvailableModNames()` 语义不变仍返回目录下全部可用 Mod（供 UI 列表展示）。开关 UI 在设置界面 Mods 页签（`UIGameSettingForMods`，见 ui-game agent），切换即 `SetModEnable`+`SaveGameConfig` 落盘；因 Catalog/JsonText 合并发生在启动阶段且不可逆，**改动重启游戏后生效**（Toast 43003 提示），运行中不做热切换。
>
> **测试模式强制全开**：`ModManager.isForceAllModsEnabled=true` 时 `FilterEnabledMods` 跳过过滤全量加载（仅内存标记不持久化，GameConfig 设置项与正式游戏不受影响）。目前由 `LauncherTest.Launch()` 在 `base.Launch()` 前置位——测试场景（卡片/幻化药等全部测试入口）默认所有 Mod 可用，免去逐一手动开启+重启；`LauncherGame` 不置位。

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

> **空 Catalog 判失败（2026-10-02 起）**：三个 `LoadModCatalog*` 变体在 Catalog 加载成功后检查资源条目数，**0 条目直接判失败**并 LogError「Catalog为空（0个资源条目），疑似空构建产物（Mod 分组未参与构建）」——否则空 catalog 会让下游 `DownloadDependenciesAsync` 报难懂的 `InvalidKeyException: No Union of Assets between Keys=`。遇到该报错去查 `Mods/<Mod名>/` 是否缺 bundle（MOD 项目构建器空构建所致，典型根因：上次构建中断残留分组 `IncludeInBuild=false`，各 Mod 构建器已带自愈）。

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

## InternalId 转换钩子（相对路径还原 + monoscripts Bundle 去重）

`ModManager.cs`「InternalId 转换钩子」region 在每次加载 Mod Catalog 前经 `EnsureInternalIdTransformInstalled()` 幂等安装 `Addressables.InternalIdTransformFunc`，钩子链依次执行两步：

### ① 相对路径还原（ResolveRelativeBundlePath）

**问题**：Mod Catalog（catalog.bin）里 Bundle 的 InternalId 由 Mod 工程的 Addressables LoadPath 配置决定，若配置为相对路径（当前全部 Mod 均为 `Mods\<ModName>\xxx.bundle` 形态），运行时 Addressables 直接用它做 `File.Exists`/`AssetBundle.LoadFromFile`，相对路径按**进程当前工作目录(CWD)**解析——编辑器下 CWD=项目根恰好命中所以测不出；**打包后 CWD 取决于启动方式**（快捷方式起始位置/启动器/命令行所在目录），不保证是 exe 目录，导致报 `Unable to open archive file: Mods/...` + `RemoteProviderException : Invalid path in AssetBundleProvider: 'Mods\...'`，Mod 资源加载失败。

**机制**：相对路径 InternalId 转为以游戏根目录（`Application.dataPath/..`）为基准的绝对路径（`Path.GetFullPath` 归一化），与 CWD 彻底无关。**仅处理 `.bundle` 结尾的 Bundle 文件路径**，运行时变量（`{` 开头）/网络地址（含 `://`）/已带根路径/非 bundle 路径一律原样放行，主工程自身内容（`{UnityEngine.Application.streamingAssetsPath}/aa/...`）不受影响。该修复对已发布的旧 Mod 直接生效，**无需重新构建 Mod**。

> **⚠ 必须限定 `.bundle` 的原因**：`InternalIdTransformFunc` 对**所有** location 生效，`BundledAssetProvider` 会把转换结果当作 **Bundle 内资源名**去 `LoadAssetAsync`（包源码 `BundledAssetProvider.cs` InternalOp 中 `TransformInternalId(location)` → `LoadAssetAsync(assetPath, type)`）。若不加 `.bundle` 限制，主工程资源的 bundle 内相对路径（如 `Assets/LoadResources/Common/ControlData.prefab`）也会被拼成绝对路径，导致 bundle 内按名取资源失败，报 `Unable to load asset of type ... from location ...`（2026-10-01 实际踩过的回归）。

### ② monoscripts Bundle 冲突去重（DedupMonoScriptBundleId）

**问题**：多个 Mod 用同一构建环境（同一 Mod 工程/同一 Spine 版本）构建时，会各自产出**内容完全相同**的 monoscripts Bundle（文件名形如 `<工程名哈希>_monoscripts_<内容哈希>.bundle`，同名即同内容）。Unity 禁止两个不同路径的 Bundle 包含相同资产文件（MonoScript 的 GUID 相同），后加载的 Mod 报 `The AssetBundle '...' can't be loaded because another AssetBundle with the same files is already loaded`，导致该 Mod 的 SkeletonDataAsset 等资源整条依赖链加载失败。

**机制**：把同名 monoscripts Bundle 的 InternalId **重定向到首个已加载实例的 InternalId**。`AssetBundleProvider` 按**转换后的 ID** 作缓存键（见其 `CreateCacheKeyForLocation` 注释："so we don't try and load the same bundle twice"），后加载 Mod 的同名 Bundle 直接命中缓存复用，不再触发二次加载。

- 登记表 `s_MonoScriptBundleCanonicalIds`：文件名 → 首个加载的 InternalId（懒登记，首次出现即为规范来源，与 Mod 加载顺序无关）。
- 只按**文件名完全相等**（含内容哈希）去重：哈希不同的 monoscripts Bundle 意味着 Mod 与宿主脚本版本不一致，此时**保留报错**（响亮的失败优于静默的版本错配）。

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
// 注意：ModManager 没有任何静态成员/Instance，须走 ModHandler.Instance.manager
int modId = ModHandler.Instance.manager.GetModId("Spine");
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

> 道具型 Mod 的完整生产范式（资源约定/ID规则/生成脚本/构建部署）见 **aeonsecho-spine-mod** / **arkre-spine-mod** / **nikke-spine-mod** / **browndust-spine-mod** / **girlwars-spine-mod** / **other-spine-mod** / **putgirl-spine-mod** / **cherrytale-spine-mod** / **crosscore-spine-mod** / **echocalypse-spine-mod** / **snowbreak-spine-mod** / **starlusts-spine-mod** Skill（各 Mod 资源约定与道具规则不同，流程不互相套用）。

### 幻化药型 Mod 通用规则：idle 动画检测（2026-09-28 起，新 Mod 必须继承）

Mod spine 资源的待机动画名未必命中主项目标准待机候选（`excel_spine_animation_state` id=10001 的 res 字段，当前 `idle,wait,idle1,wait1,stand`；运行时 `SpineAnimationStateCfg.CheckSpineAnim` 按候选顺序大小写不敏感匹配骨架实际动画名，全不命中则详情UI静态+报错）。**所有幻化药型 Mod 的 gen 生成脚本在 scan 阶段必须执行 idle 动画检测**：

1. **读候选**：按脚本位置推导主项目根（`.claude/scripts/` 向上两级）读 `Assets/Resources/JsonText/SpineAnimationState.txt` 的 id=10001 候选列表（`load_std_idle_candidates`，配置表为唯一真实源，读失败回退硬编码同值）
2. **检测**：对每个出药 SkeletonData 的同名 json 读 `animations` 键（`get_spine_anims`）→ `pick_idle_anim`：命中候选→不生成键；否则取首个小写含 `idle` 的动画原始名（保留大小写，Spine SetAnimation 需精确名；多个取 animations 顺序第一个）；完全没有含 idle 动画→不生成键+警告统计
3. **写键**：show 段骨架的替代动画写 `idle_anim` 键、ui_show 段骨架写 `ui_show_idle_anim` 键（与 show_res/ui_show_res 键对体系对齐，段间隔离互不误用），拼 other_data 串尾
4. **消费**：show 段经游戏层 `SpineHandler.GetAnimNameAppoint`（Idle 分支幻化时优先 `CreatureBeanPartial.GetTransformIdleAnim()`）；详情UI经 `GameUIUtil.SetCreatureUIForDetails` 三级分支（`GetTransformUIShowIdleAnim()` 非空→框架层按名直播；ui_show_res 非空→框架候选；否则原链路）
5. **写回保留**：`TestTransformPotionGUI` 保存循环「`ParseTransformOtherData` 读出 → 改写三尺寸字段 → `BuildOtherData` 整体拼回」的结构体往返天然保留全部其余键（`ParseTransformOtherData` 返回 `TransformOtherData` 结构体——2026-09-28 由多 out 参数重构为结构体，新增键=结构体加字段+解析加 case，调用点零改动）

### 幻化药型 Mod 通用规则：walk/attack/dead 动画映射（机制 2026-10-02 起，首例=AeonsEchoSpine attack）

战斗用 show 段幻化的 Mod（ Chess 系骨架上战场）除 Idle 外还会播 Walk/Attack/Dead，Mod 骨架同样未必命中主项目标准候选（Walk=`walk,walk1,move`、Attack=`attack,attack1`、Dead=`dead,dead1,die`）。**C# 机制已按 idle_anim 同款模式备齐三键**：`TransformOtherData` 结构体含 `walkAnim`/`attackAnim`/`deadAnim` 字段（`walk_anim`/`attack_anim`/`dead_anim` 键），消费=游戏层 `SpineHandler.GetAnimNameAppoint` 幻化守卫分支（2026-10-02 起由仅 Idle 例外扩展为 Idle/Walk/Attack/Dead 四状态各查映射键，非空优先按名直播，缺省交框架候选），`TestTransformPotionGUI.BuildOtherData` 同步拼接防写回丢键。**是否生成键/映射到什么动画由各 Mod 用户拍板**（两种兜底路线：①映射键=把替代动画名写进各药 other_data；②主项目配置表扩容=把 Mod 动画名加进 `excel_spine_animation_state` 对应状态 res 候选，一处生效全 Mod 共享但污染主项目标准）。已拍板实例：AeonsEchoSpine（2026-10-02）——**attack 走映射键**（scan 检测无标准攻击候选时按 `skill1`→`skill` 顺序取全等命中动画名写 `attack_anim` 键，56 段次替代/1 段次无 skill 保持待机）；**walk 走配置表扩容**（id=20001 候选加 `move1,move2`，覆盖 B/D 型 43 骨架，A 型 213 个无移动动画骨架仍不命中用户已知悉）；**dead 走映射键·方案B**（scan 检测无标准死亡候选时有 `attacked` 写 `dead_anim:attacked`=受击抖一下再消失、否则回退该骨架实际待机动画名，65 段次=attacked×10/idle×55；`walk_anim` 键机制已备未启用）。其余仅 ui_show 幻化的 Mod（详情UI只播 Idle）不需要本规则。

三个 gen 脚本（`gen_aeonsecho_spine_mod.py`/`gen_arkre_spine_mod.py`/`gen_nikke_spine_mod.py`）均已实现该规则，新 Mod 复制脚本时连同 `load_std_idle_candidates`/`get_spine_anims`/`pick_idle_anim` 三函数与 compute_items 检测接入一并继承。**例外一**：BrownDustSpine（`gen_browndust_spine_mod.py`）出药规则为「按动画名拆药」（idle/all/loop/cut 每个匹配动画一个药、四类独立判定不去重），其 ui_show_idle_anim 键=各药对应动画名，不走 pick_idle_anim 单选逻辑（详见 browndust-spine-mod SKILL）。**例外二**：GirlWarsSpine（`gen_girlwars_spine_mod.py`）待机动画**固定用 `A`**（全部骨架动画均为 A/A1/A2/in，用户指定 2026-09-29），ui_show_idle_anim 键恒 =A、每药必带，骨架无 A 动画时省略+警告（详见 girlwars-spine-mod SKILL）。**例外三**：CherryTaleSpine（`gen_cherrytale_spine_mod.py`）同为「按动画拆药」但**全部动画各出 1 药**（排除 Talk 与 _mark 差分），ui_show_idle_anim 键=各药对应动画名（详见 cherrytale-spine-mod SKILL）。**例外四**：SnowbreakSpine（`gen_snowbreak_spine_mod.py`）动画均带 `sp_` 前缀不命中标准候选，改走 **stand 特殊处理链**（`pick_stand_anim`：① 以 stand 结尾→② 含 stand→③ 含 idle→④ 首个动画兜底），ui_show_idle_anim 键=链检出动画名、**每药必带**（详见 snowbreak-spine-mod SKILL）。标准规则的后续继承者：PutGirlSpine/OtherSpine/CrossCoreSpine（`gen_crosscore_spine_mod.py`，2026-09-29 新增：其资源待机动画名就是候选 #1 的 idle，405/423 药命中候选不写键，详见 crosscore-spine-mod SKILL）/EchocalypseSpine（`gen_echocalypse_spine_mod.py`，2026-09-30 新增：314/317 药命中候选不写键，3 个 idle_A 写替代键，详见 echocalypse-spine-mod SKILL）/StarLustsSpine（`gen_starlusts_spine_mod.py`，2026-09-30 新增：88/144 骨架精确 Idle 命中候选不写键，56 个 CG 写 InteractiveMode/P1_Idle 等替代键，无 idle 骨架跳过不出药，详见 starlusts-spine-mod SKILL）。

### 幻化药型 Mod 通用规则：ui_show_skin 组合皮肤语法（2026-09-29 起）

`other_data` 的 `ui_show_skin` 键支持 **「|」分隔多皮肤叠加**（首例=CherryTaleSpine D 类组合皮药 `ui_show_skin:Eye_01|Mouth_01`）：

1. **生成侧**：组合皮肤只用于「部件皮肤叠加」场景（Eye/Mouth 等覆盖在 default 基底上的部件皮）；整皮替换场景（ArkRe LV1 等）仍写单皮肤名
2. **消费侧**：`CreatureHandler.SetCreatureData` 把 `GetTransformUIShowSkin()` 返回串按 `|` 拆分——**多个→`SpineHandler.ChangeSkeletonSkin(Skeleton, params string[])` 叠加换肤**（new Skin + AddSkin×N + SetSkin + SetupPoseSlots，未在组合内的部件自动回落骨架默认皮肤；单皮肤缺失报错跳过、不阻断其余叠加）；**单个→原 `ChangeSkeletonSkin(Skeleton, string)` 整皮替换**（既有单皮肤药行为不变）
3. **禁止**对组合皮肤逐个调 string 重载——整皮替换语义下后调会覆盖先调，只剩最后一个皮肤
4. `TestTransformPotionGUI` 保存的结构体往返天然保留 `|` 皮肤串（无需特判）

### 查询JsonText文件

```csharp
// 检查是否有Mod包含指定名称的JsonText文件（ModManager 无静态 Instance，须走 ModHandler.Instance.manager）
bool hasFile = ModHandler.Instance.manager.HasModJsonTextFile("CreatureInfo");

// 获取包含指定fileName的所有Mod信息
var fileInfos = ModHandler.Instance.manager.GetModJsonTextFileInfos("CreatureInfo");
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
