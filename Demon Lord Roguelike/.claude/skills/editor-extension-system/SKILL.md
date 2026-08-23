---
name: editor-extension-system
description: Demon Lord Roguelike 游戏的编辑器扩展系统开发指南。使用此SKILL当需要创建或修改Unity编辑器工具、代码生成器、配置表导出工具、Inspector扩展等，包括ExcelEditorWindow、MVCEditorWindow、BaseUICreateWindow、GameTestEditor、SpineWindow、Inspector扩展、Hierarchy扩展、节点编辑器等。
watched_files:
  - Assets/FrameWork/Editor/
  - Assets/Editor/
  - Assets/FrameWork/Editor/Base/Window/
  - Assets/FrameWork/Editor/ScriptsTemplates/
  - Assets/Editor/GameTestEditor.cs
  - Assets/Editor/GameTestEditorPartial.cs
---

# 编辑器扩展系统开发指南

## 核心概念

项目提供丰富的编辑器扩展工具，覆盖代码生成、配置管理、资源管理、美术工具、测试工具等。

### 编辑器工具体系

```
EditorWindow (Unity)
├── ExcelEditorWindow              # Excel 配置导出
├── MVCEditorWindow                # MVC 代码生成
├── UIEditorWindow                 # UI 代码生成
├── BaseUICreateWindow             # UI 脚本创建向导
├── AddressableWindow              # Addressable 管理
├── SpineWindow(+SpineWindowPreview partial)  # Spine 工具（皮肤提取 + 动画预览页签，预览可绕过官方版本兼容检查并自由搭配皮肤）
├── NodeBaseEditorWindow           # 节点编辑器
├── SearchEditorWindow             # 搜索编辑器
├── AnimSearchWindow               # 动画搜索
├── ImageResWindow                 # 图片资源窗口
├── CubemapGeneratorWindow         # Cubemap 生成器
├── FBXEditorWindow                # FBX 编辑器
├── SkinMeshEditorWindow           # 皮肤网格编辑器
├── ProjectAssetCollectorWindow    # 项目资源收集器
├── PixelArtPreviewWindow          # 像素图预览工具（多文件夹拖拽→Grid预览→点击Ping定位；虚拟滚动+AssetPreview异步缩略图）
├── StyleBaseWindow                # 样式基础窗口
├── GameTestEditor                 # 游戏测试编辑器 (Inspector扩展)
├── GameBuildEditorWindow          # 打包游戏工具 (打包前 Spine 资源生成 + BuildPlayer)
├── SkinRandomEditorWindow         # 皮肤/装备/套装随机池配置 (CreatureRandomInfo 三模式: 皮肤池编辑skin_random_data/装备池·套装池编辑equip_random_data, 双列表点选增删, 写回Excel+同步JSON)
├── EquipSuitEditorWindow          # 装备套装配置 (EquipSuitInfo 套装表: 物种下拉+7槽位点选填入+新建/删除套装, 单EPPlus会话写回+同步JSON)
├── FightSceneEditorWindow         # 战斗场景配置 (excel_fight_scene: 预制/道路色/天空盒/雾/环境光/细节预制直观编辑, 保存写回Excel+再生JSON, Play时实时应用到当前战斗场景)
└── PixelDaEditorWindow            # PixelDa 像素美术生成 (AI 文生图/图编辑/图生视频/抽帧/音乐)
```

---

## Excel 配置导出 (ExcelEditorWindow)

**文件**: `Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs`

### 功能

- Excel 配置表 → JSON 文件导出
- 多语言文本导出
- 支持增量导出

### 打开方式

```
菜单: Custom/工具弹窗/Excel编辑器
```

### 使用流程

```
1. 编辑 Assets/Data/Excel/*.xlsx
2. 打开 ExcelEditorWindow
3. 选择需要导出的 Sheet
4. 点击导出，生成 JSON 到 Resources/JsonText/
```

### `valid` 有效性列约定（生成器内置过滤）

`CreateEntity` 生成 `*Bean.cs` 时会检测工作表是否存在列名为 **`valid`** 的字段（`cellName.Equals("valid")` → `hasValid`）。**只有含该列的表**才会在生成的 `Cfg` 里附带过滤逻辑，其它表生成结果完全不变（按需启用、对存量表零影响）。

约定语义：`valid` 为 `int`，**0=无效（不进入运行时列表），1=有效**。`hasValid` 为真时生成器会：

- `GetAllArrayData()`：`GetInitData` 后追加 `arrayData = System.Array.FindAll(arrayData, itemData => itemData.valid != 0);`，过滤后再缓存。
- `GetItemData(key)`：改走 `InitData(GetAllArrayData())`（而非直接 `GetInitData`），使字典/单点查询都复用同一份已过滤数据 —— 即无效行 `GetItemData(无效id)` 也返回 `null`，运行时彻底不存在。

注意点：

- **默认值坑**：JSON 反序列化 `int` 缺省为 `0`=无效。给某表新增 `valid` 列后，必须把现有每行填 `1` 并重新导出 JSON，否则该表全部数据消失。
- **链式 parent_id**：若某表用 `parent_id` 串等级链（如深渊馈赠），把链中间某级标 0 会令该级 `GetItemData` 返回 null、链从此断开 —— 通常标无效即意图弃用该族，符合预期。
- 首个启用该约定的表：`AbyssalBlessingInfo`（深渊馈赠，valid==0 不进入征服关卡间候选池）。

---

## MVC 代码生成 (MVCEditorWindow)

**文件**: `Assets/FrameWork/Editor/Base/Window/MVCEditorWindow.cs`

### 功能

- 根据配置表自动生成 Bean + Service 代码
- 支持字段类型映射
- 自动生成多语言 Property

### 使用流程

```
1. 配置 Excel 表结构（定义字段名和类型）
2. 打开 MVCEditorWindow
3. 选择目标 Excel
4. 配置命名空间和类名
5. 点击生成 → 生成 Bean 类、Service 类
```

### 生成的代码示例

```csharp
// 自动生成的 Bean
[Serializable]
public partial class MyFeatureInfoBean : BaseBean
{
    public long name;           // 名称文本ID
    public long value;          // 数值
    public string class_name;   // 类名

    [JsonIgnore]
    public string name_language
    {
        get { return TextHandler.Instance.GetTextById("MyFeatureInfo", name); }
    }
}

// 自动生成的 Cfg
public partial class MyFeatureInfoCfg : BaseCfg<long, MyFeatureInfoBean>
{
    public static string fileName = "MyFeatureInfo";
    protected static Dictionary<long, MyFeatureInfoBean> dicData = null;

    public static MyFeatureInfoBean GetItemData(long key)
    {
        if (dicData == null)
        {
            MyFeatureInfoBean[] arrayData = GetInitData(fileName);
            InitData(arrayData);
        }
        return GetItemData(key, dicData);
    }

    public static void InitData(MyFeatureInfoBean[] arrayData)
    {
        dicData = new Dictionary<long, MyFeatureInfoBean>();
        for (int i = 0; i < arrayData.Length; i++)
        {
            var itemData = arrayData[i];
            dicData.Add(itemData.id, itemData);
        }
    }
}
```

---

## UI 脚本创建工具 (BaseUICreateWindow)

**文件**: `Assets/FrameWork/Editor/Base/Window/BaseUICreateWindow.cs`

### 功能

- 根据 Prefab 自动生成 UI 脚本
- 支持多种 UI 类型（UI/Dialog/Popup/Toast/View/Common）
- 自动添加脚本组件到 Prefab

### 打开方式

```
菜单: Custom/工具弹窗/UI脚本创建
Toolbar: UI脚本创建 按钮
```

### 脚本模板

| 模板文件 | 生成类型 |
|---------|---------|
| `UI_BaseUI.txt` | 普通 UI（继承 BaseUIView） |
| `UI_BaseUIView.txt` | View 组件（继承 BaseUIComponent） |
| `UI_BaseUIDialog.txt` | 弹窗（继承 DialogView） |
| `UI_BaseUIPopup.txt` | 气泡（继承 PopupShowView） |
| `UI_BaseUIToast.txt` | 提示（继承 ToastView） |

### 使用流程

```
1. 创建 UI Prefab（GameObject 名 = 类名）
2. 在 Prefab 子物体中命名控件（ui_xxx 前缀）
3. 打开 BaseUICreateWindow
4. 拖入 Prefab，选择脚本类型和模块名
5. 点击生成 → 自动创建脚本 + 添加到 Prefab
```

---

## 游戏测试编辑器 (GameTestEditor)

**文件**: `Assets/Editor/GameTestEditor.cs` + `GameTestEditorPartial.cs`

### 功能

- Inspector 中配置测试参数
- 一键启动各种测试场景
- 参数持久化（EditorPrefs）

### Inspector 面板结构

```
LauncherTest (Inspector)
├── Test Scene Type 下拉选择
├── ──── 根据类型显示对应参数 ────
├── NormalGame: 正常游戏启动（走真实开始流程）
├── FightSceneTest: 战斗参数配置
├── CardTest: 卡片测试参数
├── Base: 基地测试参数
├── RewardSelect: 奖励选择参数
├── DoomCouncil: 终焉议会参数（两个启动按钮：开始终焉议会 StartForDoomCouncil / 查看所有固定议员 StartForDoomCouncilAllFixed）
├── NpcCreate: NPC创建参数（两个启动按钮：预制版 StartNpcCreate / 纯GUI代码版 StartNpcCreateGUI）
├── ResearchUI: 研究UI参数
├── AbyssalBlessing: 深渊馈赠UI参数
├── CreatureSacrifice: 献祭升级测试参数
├── CreatureVat: 魔物进阶测试参数（选存档 + 解锁VAT数量/加速等级）
├── CreatureJuicer: 魔汁机测试参数（选存档 + 投入魔物上限滑条 5~15）
├── EffectTest: 粒子特效测试（▶️ 开始粒子特效测试按钮，打开纯代码 IMGUI 面板 TestEffectGUI）
├── ConversationTest: 对话系统测试（说话NPC下拉/手动ID + 自由文本 TextArea + ▶️ 开始对话展示）
└── ▶️ 开始测试 按钮（仅运行时可用）
```

### 添加新测试类型

参见 [test-system skill](test-system/SKILL.md) 中的详细步骤。

---

## 打包游戏工具 (GameBuildEditorWindow)

**文件**: `Assets/Editor/GameBuildEditorWindow.cs`，**菜单**: `游戏/打包游戏`

### 功能

- 打包前 3 个可勾选步骤（默认全勾选）：生成所有 Spine 道具图标 / 生成所有 Spine 皮肤图标 / 刷新所有图集 —— 均直接复用 `GameDataEditor` 的 public static 方法（`SpineAllItemInit`/`SpineAllSkinInit`/`RefreshAllAtlases`）。
- 打包选项（均经 EditorPrefs 持久化）：开发包(Development)、允许脚本调试(AllowDebugging)、自动连接 Profiler(ConnectWithProfiler)、深度分析(EnableDeepProfilingSupport)、完成后自动运行(AutoRunPlayer)、完成后打开输出目录(ShowBuiltPlayer)。调试/Profiler/深度分析三个子选项依赖开发包，取消开发包时联动关闭并置灰。
- 打包路径选择：默认为 git 仓库根的上级目录下 `DLR/`（从 `Application.dataPath` 向上找 `.git` 动态推导，找不到则退化为项目根上级目录），支持浏览修改与「重置为默认路径」，选择经 EditorPrefs 持久化。
- 「开始打包」：先 `EnsureURPCompatibilityModeDefine` 确保当前平台带 `URP_COMPATIBILITY_MODE` 编译宏（Unity 6.3 起 URP 兼容模式被打包校验拦截，缺宏直接 BuildFailedException；缺宏时自动补宏并弹窗提示——补宏触发脚本重编译会中断本次打包，重编译完成后需重新点击「开始打包」）→ 自动切换到 `Assets/Scenes/GameScene.unity`（未保存修改弹保存提示、取消则中止；打包完成后自动切回原场景）→ 执行勾选步骤 → **固定只用 GameScene 打包**（不读 Build Settings 场景列表，避免日常挂的 TestScene 混进正式包）→ 按勾选项组装 `BuildOptions` → `BuildPipeline.BuildPlayer` 打到 `activeBuildTarget`（Windows 平台自动追加 `PlayerSettings.productName + ".exe"`），成功后打开产物目录（勾选 ShowBuiltPlayer 时由 Unity 打开，否则手动 `RevealInFinder`）。

---

## 皮肤/装备/套装随机池配置 (SkinRandomEditorWindow)

**文件**: `Assets/Editor/SkinRandomEditorWindow.cs`，**菜单**: `游戏/皮肤随机池配置`

### 功能

可视化编辑 `excel_creature_random_info[生物随机信息] .xlsx`（注意文件名含空格）的随机池（工作表 `CreatureRandomInfo`），按 `random_type` 列分三种模式（池下拉标签带 [皮肤]/[装备]/[套装] 前缀）：

- **皮肤池(random_type=0)**：编辑 `skin_random_data` 列（随机皮肤池，池内为 CreatureModelInfo id）。
- **装备池(random_type=1)**：编辑 `equip_random_data` 列（随机装备池，池内为 ItemsInfo 道具id）；右侧候选直读 `excel_items_info[道具信息].xlsx` 的装备类型道具（帽/衣/裤/鞋/鼻环/戒指/武器，`EquipItemTypes` 集合），按道具 `creature_model_id` 推导池物种（0=通用装备不参与推导、始终可见），筛选下拉为道具类型；装备图标按 `icon_res` 从 `AtlasForItems.spriteatlas` 取 sprite（`GUI.DrawTextureWithTexCoords` 按 textureRect 绘制）。
- **套装池(random_type=2)**：编辑 `equip_random_data` 列（套装池，池内为 EquipSuitInfo 套装id，多套等概率整套随机）；右侧候选直读 `excel_equip_suit_info[装备套装].xlsx`（`SuitInfoItem`：id/物种/件数/备注），按套装 `creature_model_id` 推导池物种（0=通用套装始终可见），左列表按物种分组；套装内容本身由 `EquipSuitEditorWindow` 编辑（本窗口只管池组合）。

### 皮肤池模式要点

- **随机池下拉**（`id | remark`）+ 当前池具体内容展示（压缩串只读 TextArea + 部件总数/覆盖部位/无效ID 统计）。
- **左列表 = 已加入随机的皮肤**：按部位(`CreatureSkinTypeEnum`)分组排序，逐行「移除」；池中悬空 ID（模型表不存在）红色标记排在最后。**右列表 = 未加入随机的皮肤**：**按池内已有部件的物种自动限定**（选了人类池只列人类皮肤，空池不限定，无物种下拉），支持部位/搜索(id/res_name/remark) 筛选，逐行「加入」；另有「全部移除」「加入全部(筛选结果)」批量按钮。
- **装备/武器类部位（统一维护在 `ExcludePartTypes` 列表：鼻环9/帽子50/衣服51/裤子52/鞋53/腰带54/手套55/武器线80/武器左右手90-91/双手武器92）双列表均不展示**——此类皮肤由装备道具驱动换皮（鼻环虽在枚举身体段，但道具表 item_type=5 经 `creature_model_info_id` 对接）；池内已有的装备部件数据保留不删，仅隐藏并在左列表头提示数量。新增装备类部位直接往 `ExcludePartTypes` 加，`IsEquipPart` 统一判定。
- **每行带皮肤图标**：命名约定 `{CreatureModel.mark_name}_Atlas_{CreatureModelInfo.res_name 的 / 换成 _}`（与 UITestNpcCreate 取图同约定），图标是 `GameDataEditor.SpineAllSkinInit` 抽取到 `Assets/LoadResources/Textures/Skins/` 的产物，懒加载+缓存，缺失时灰块占位（缺图标可跑「生成所有 Spine 皮肤图标」补齐）。
- 多池切换编辑不丢变更（每池独立 `skinSet`/`equipSet` + `originalData`/`originalEquipData` 对比出 dirty，`IsPoolDirty` 按池类型比对对应集合），刷新前有未保存变更确认。
- **保存**：把 ID 集合升序压缩为区间串（连续段 `a-b`，逗号连接，与表内原有书写格式一致）→ 按池类型写回 `skin_random_data`/`equip_random_data` 列（`ExcelUtil.SetExcelData`；套装池与装备池同写 `equip_random_data`）→ `ExcelUtil.ExcelToJsonItem` 整体再生 `CreatureRandomInfo.txt`（该 Excel 仅单表，再生安全）→ `AssetDatabase.Refresh`。解析与运行时 `SplitForListLong(',', '-')` 同规则。部件全集直读 `excel_creature_model_info[生物模型详情信息] .xlsx`（不经 JSON 保证最新），物种名/mark_name 取自 `excel_creature_model[生物模型信息].xlsx`。

---

## 装备套装配置 (EquipSuitEditorWindow)

**文件**: `Assets/Editor/EquipSuitEditorWindow.cs`，**菜单**: `游戏/装备套装配置`

### 功能

可视化编辑 `excel_equip_suit_info[装备套装].xlsx`（工作表 `EquipSuitInfo`）：一行=一套手动搭配的装备套装，供 `CreatureRandomInfo` 套装池（random_type=2）引用参与 NPC 整套随机（解决散件池衣裤不搭问题）。

- **表结构**：`id`、`creature_model_id`（种族模组，0=通用）、7 槽位列 `hat/clothes/pants/shoe/nose_ring/finger_ring/weapon`（道具ID，0=空槽）、`remark`（套装名）。
- **左侧套装编辑区**：套装下拉（`id | 物种 | 备注`）+ 物种下拉（`modelIdValues`：0=通用 + 官方物种，只列 remark 非空且 id<100000 的，Mod 物种不进下拉）+ 备注输入 + 7 槽位行（槽位名/图标/id/备注 + 「选择」「清空」）；当前挑选槽位 `currentPickSlot` 高亮（▶ 前缀）；**物种不匹配的件红色警告**（道具与套装物种均非 0 且不一致时——运行时 `EquipSuitInfoBean.CanEquipFor` 会把整套剔除候选）；悬空 ID（道具表不存在）红色标记。
- **右侧候选装备列表**：按当前槽位 `item_type` + 套装物种过滤（通用装备 0 始终可见、套装为通用 0 时不限物种）+ 搜索（id/icon_res/remark），点「填入」写入当前槽位；装备图标按 `icon_res` 从 `AtlasForItems.spriteatlas` 取 sprite（与 SkinRandomEditorWindow 同绘制法）。
- **新建/删除**：新建经工具栏「新id」输入框指定 id（默认建议当前 max+1、起始段 200001，可手改；校验正整数且未占用，默认通用物种，标 `[新]` 前缀）；删除即时从列表移除并记录 `deletedIds`（新建未保存的直接丢弃），保存时才物理删行。
- **保存**：删除（按 id 定位行、行号降序 `DeleteRow` 防漂移）+ 修改（按 id 定位行逐列覆写）+ 新增（末尾追加）统一在**一个 EPPlus 会话**完成（数字列写数值类型，**不走 `ExcelUtil.SetExcelData`**——它把值当 string 写入会把 long 列变文本）→ `ExcelUtil.ExcelToJsonItem` 再生 `EquipSuitInfo.txt`（**依赖 `EquipSuitInfoBean` 已由 ExcelEditorWindow 生成**，否则反射找不到实体类报错）→ 刷新各套装快照（`CommitSnapshot`）。
- **脏检查**：每套装 `Snapshot()`（物种|备注|各槽位道具拼串）对比 `originalSnapshot`，`isNew` 恒脏；刷新/关闭前有未保存变更确认。

---

## 战斗场景配置 (FightSceneEditorWindow)

**文件**: `Assets/Editor/FightSceneEditorWindow.cs`，**菜单**: `游戏/战斗场景配置`

### 功能

可视化编辑 `excel_fight_scene[战斗场景].xlsx`（工作表 `FightScene`）的每行场景参数，并支持 **Play 模式下实时应用到当前战斗场景**看效果：

- **场景下拉**（`id | remark`）+ 直读 Excel（`ExcelUtil.GetExcelPackage` EPPlus，不经 JSON 保证最新；前 3 行元数据、第 4 行起数据，按第 1 行字段名建列索引）。
- **直观编辑**：场景预制体（ObjectField↔`name_res` 纯文件名，目录 `PathInfo.FightScenePrefabPath`）、道路颜色 A/B（ColorField↔hex）、天空盒（Material ObjectField↔路径 + `skybox_rotate` Vector3）、雾（Toggle+颜色+Start/End，模式固定 Linear）、环境光（Toggle+颜色↔`ambient_light` hex）、细节预制（`details` 文本+Day/Night/清空快捷钮）、备注。
- **fog 字符串互转**：读取复用运行时 `FightSceneBean.GetFogParams` 解析（单一逻辑源），保存拼装 `Color:#xxx&Start:x&End:x&Mode:Linear`（浮点用 InvariantCulture 防文化差异）。
- **保存**：`ExcelUtil.SetExcelData`（按 id 定位行、字段名定位列，本表编辑列全为 string 正好适用）→ `ExcelUtil.ExcelToJsonItem` 再生 `FightScene.txt` → `AssetDatabase.Refresh`；每行 `Snapshot()` 快照做 dirty 检测，切换/刷新前有未保存确认。
- **运行时应用**（`ApplyToRuntimeScene`，仅 Play 且 `WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.Fight)` 非空时可用）：雾走 `VolumeHandler.SetFog/SetFogActive`；环境光直设 `RenderSettings.ambientLight`（未配置则不动）；天空盒设 `RenderSettings.skybox` + `_RotateX/Y/Z`；道路颜色找场景子物体里材质带 `_ColorA/_ColorB` 的 MeshRenderer 写 sharedMaterial；细节预制按 `details` 同名切换 Details 子物体（与 `WorldHandler.HandleFightSceneDetails` 同逻辑）。「实时应用」Toggle 开启时字段改动（EditorGUI change check）即自动应用；退出 Play 后 RenderSettings 随场景快照还原，无污染。

---

## 研究模块编辑 (ResearchEditorWindow)

**文件**: `Assets/Editor/ResearchEditorWindow.cs`，**菜单**: `游戏/研究模块编辑`

### 功能

可视化编辑研究节点树：顶部类型 Tab（设施/强化/魔物/世界）→ 中间可拖动画布（节点+前置连线+网格）→ 下方选中研究全字段编辑区（ResearchInfo 字段 + 多语言 cn/en + 对应 UnlockInfo）。数据源为 JSON（`ResearchInfo.txt`/`UnlockInfo.txt`/`Language_ResearchInfo_cn|en.txt`），保存时 diff 快照写回 Excel（`excel_research_info`/`excel_unlock_info`/`excel_language` 的 ResearchInfo 工作表）并再生 JSON。

- **画布坐标**：世界坐标 Y 向上、屏幕 Y 向下，`WorldToScreen`/`ScreenToWorld` 负责翻转；连线用 Cohen–Sutherland 裁剪到画布内（`GUI.BeginClip` 裁不住 Handles/GL）。
- **普通模式交互**：左键点节点=选中+拖动改 `position_x/y`（松手取整）；左键空白/右键/中键=平移；滚轮=以鼠标为中心缩放。
- **框选模式**（工具栏「框选」开关，开启时多一条框选工具条）：左键拖空白画框选矩形（节点中心落入即选中，按住 Shift 追加；<4px 视为单击单选）；点在已选中节点上=整组拖拽（`dragStartPositions` 记录各节点起始坐标，松手统一取整）；工具条支持清空选择、输入整体偏移 X/Y 点「应用偏移」批量移动（取整）。多选节点绿色高亮（单选橙色），切 Tab/关框选时清空选择。注意：工具条里窄宽度（<150px）的带 label 字段（`EditorGUILayout.FloatField("X", ...)`）必须临时收窄 `EditorGUIUtility.labelWidth`（如 12px，用后还原），否则 label 按默认 ~150px 吃掉全部输入区导致无法输入。
- **编辑区**：research_type/icon_res(带图集子 Sprite 预览)/level_max/position/unlock_id/pre_unlock_ids/pay_crystal/name/remark + 多语言 + UnlockInfo；「还原选中研究」按快照回滚单条。
- **保存**：`ChangeSet` 按 id 分组逐字段写 Excel（已有行更新、未命中末尾新建），多语言写 `excel_language` 的 ResearchInfo 工作表（id/content_cn/content_en 列），JSON 全量重写后 `AssetDatabase.Refresh` 并重建快照。

---

## Inspector 扩展

### InspectorBaseUIComponent

**文件**: `Assets/FrameWork/Editor/Base/Inspector/InspectorBaseUIComponent.cs`

自动显示和绑定 `ui_` 前缀的控件：
- 支持 Text, Button, Image, Slider 等常用组件
- 支持自定义组件类型
- 缺失控件高亮提示

### InspectorBaseUIView

**文件**: `Assets/FrameWork/Editor/Base/Inspector/InspectorBaseUIView.cs`

BaseUIView 的 Inspector 扩展，显示 UI 层级和动画信息。

### InspectorEffectBase / InspectorMaskUIView

特效和遮罩的 Inspector 扩展。

### InspectorFlowerSeaInstanceRenderer

**文件**: `Assets/FrameWork/Editor/Base/Inspector/InspectorFlowerSeaInstanceRenderer.cs`

花海渲染器（`FlowerSeaInstanceRenderer`，见 framework-core-system）的 Inspector 扩展：
- **全参数中文化标注**：`fieldContents` 字典（字段名→`GUIContent(中文标签, 中文悬停提示)`），未登记字段回退原名；枚举弹窗（textureMode/shape）用 `EditorGUILayout.Popup` 中文化，注意 **Popup 不画 [Header] 装饰**，需 `DrawHeaderFor` 反射补画
- 贴图区按 `textureMode` 条件显示（图集模式只画图集字段 / 单图模式只画单图字段）：主循环 `GetIterator` 跳过贴图字段，绘制到 `textureMode` 时插入条件块；地形区同理按 `heightMode` 条件显示（射线模式 / 高度图模式字段组）
- 图集模式关闭「使用图集全部格子」时展开 **行列 toggle 网格**（`DrawAtlasCellGrid`）：按均分列×行画 miniButton 阵列点选格子（顶行=贴图最上行，与看图习惯一致；>256 格退化为直接画列表防卡），含全选/清空按钮与空选警告；选中集合排序写回 `atlasSelectedCells` 保证序列化稳定
- 底部「重新生成花海 / 重置全部消散 / 测试踩踏(r=2)」按钮 + 状态行（花朵总数/已消散数）

---

## Hierarchy 扩展

### HierarchySelect

**文件**: `Assets/FrameWork/Editor/Base/Hierarchy/HierarchySelect.cs`

Hierarchy 窗口中显示额外信息（如组件图标、状态标识等）。

### HierarchySelectPopupSelect

Popup 类型的 Hierarchy 扩展。

---

## 节点编辑器 (NodeBaseEditorWindow)

**文件**: `Assets/FrameWork/Editor/Base/NodeEditor/NodeBaseEditorWindow.cs`

基于节点的可视化编辑器基类，可用于：
- 对话系统编辑
- AI 行为树编辑
- 技能序列编辑

---

## PixelDa 像素美术生成 (PixelDaEditorWindow)

**菜单**: `Custom/AI/像素图生成`

**目录**: `Assets/FrameWork/Editor/Base/Window/PixelDa/`（纯 C# 实现，复刻开源工具 dada-x/pixelda 的全部功能，无 Python 依赖）

| 文件 | 职责 |
|------|------|
| `PixelDaEditorWindow.cs` | 主窗口：文生图/图编辑/图生视频/视频抽帧/音乐/历史/设置 七个页签 |
| `PixelDaCore.cs` | `PixelDaConfig`(EditorPrefs 持久化：双提供商 Key/端点/模型) + `PixelDaDispatcher`(后台任务回主线程) |
| `PixelDaApi.cs` | HttpClient 直连豆包(Ark)/通义(DashScope) REST：文生图、图编辑、图生视频(异步轮询)、音乐 ABC |
| `PixelDaImageUtil.cs` | 纯色背景剔除(洪水填充)、精灵表横向合成、PNG 读写 |
| `PixelDaFrameUtil.cs` | 调系统 ffmpeg 按均匀时间戳抽帧、zip 打包 |
| `PixelDaMusicUtil.cs` | ABC 记谱解析 → 方波(chiptune)合成 WAV/AudioClip |

### 功能与提供商

- 支持**豆包**(`doubao-seedream-4-0`/`seedance`/`seed-1-6`) 与**通义**(`wan2.5-t2i`/`wanx2.1-imageedit`/`wan2.5-i2v`/`qwen-plus`)，设置页填各自 API Key 并可切换。
- 端点 URL 与模型名在「设置→高级」可改（防官方接口变动写死失效）。
- 输出统一存 `Assets/Out/PixelDa/<images|videos|frames|sprites|music|zips>/`，自动 `AssetDatabase.Refresh` 导入工程。

### 与原工具的纯 C# 实现差异

- **去背景**为「纯色背景剔除」(采样四角主色 + 阈值 + 边缘洪水填充)，适配纯色背景像素图，非原工具的 rembg/u2net AI 抠图。
- **抽帧**依赖系统 ffmpeg（设置页可配路径），非原工具的 OpenCV。
- **音乐**用方波合成 ABC 记谱模拟 8-bit chiptune（实现常见 ABC 子集），非原工具的 music21+8bit 音色库渲染。

> 该工具有专属开发文档：[pixelda](../pixelda/SKILL.md) skill 与 [pixelda](../../agents/pixelda.md) agent，详细功能/接口/线程模型见其中。

---

## 创建新的编辑器窗口

### 继承 EditorWindow 创建编辑器工具

```csharp
// Assets/FrameWork/Editor/Base/Window/MyEditorWindow.cs
public class MyEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/我的工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<MyEditorWindow>("我的工具");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private string inputData = "";
    private Vector2 scrollPos;

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("配置区域", EditorStyles.boldLabel);

        inputData = EditorGUILayout.TextField("输入数据", inputData);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("执行操作", GUILayout.Height(30)))
        {
            ExecuteOperation();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void ExecuteOperation()
    {
        // 编辑器逻辑
        EditorUtility.DisplayDialog("结果", $"处理完成: {inputData}", "确定");
    }
}
```

### 继承 Editor 创建 Inspector 扩展

```csharp
// Assets/FrameWork/Editor/Base/Inspector/InspectorMyComponent.cs
[CustomEditor(typeof(MyComponent))]
public class InspectorMyComponent : Editor
{
    private SerializedProperty propSpeed;
    private SerializedProperty propName;

    private void OnEnable()
    {
        propSpeed = serializedObject.FindProperty("speed");
        propName = serializedObject.FindProperty("displayName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(propSpeed);
        EditorGUILayout.PropertyField(propName);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("快速配置"))
        {
            propSpeed.floatValue = 10f;
            propName.stringValue = "Default";
        }

        serializedObject.ApplyModifiedProperties();
    }
}
```

---

## 脚本模板系统

**路径**: `Assets/FrameWork/Editor/ScriptsTemplates/`

| 模板文件 | 用途 |
|---------|------|
| `Excel_Bean.txt` | Excel 配置 Bean 模板 |
| `Excel_Cfg.txt` | Excel 配置管理类模板 |
| `Excel_LanguageEntity.txt` | 多语言实体 Bean 模板 |
| `UI_BaseUI.txt` | 普通 UI 脚本模板 |
| `UI_BaseUIView.txt` | View 组件脚本模板 |
| `UI_BaseUIDialog.txt` | 弹窗脚本模板 |
| `UI_BaseUIPopup.txt` | 气泡脚本模板 |
| `UI_BaseUIToast.txt` | 提示脚本模板 |
| `Service_Base.txt` | 数据服务模板 |

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 框架编辑器根目录 | `Assets/FrameWork/Editor/` |
| 编辑器窗口 | `Assets/FrameWork/Editor/Base/Window/` |
| Inspector 扩展 | `Assets/FrameWork/Editor/Base/Inspector/` |
| Hierarchy 扩展 | `Assets/FrameWork/Editor/Base/Hierarchy/` |
| 节点编辑器 | `Assets/FrameWork/Editor/Base/NodeEditor/` |
| 脚本模板 | `Assets/FrameWork/Editor/ScriptsTemplates/` |
| 编辑器工具类 | `Assets/FrameWork/Editor/Base/Utils/` |
| AssetBundle 工具 | `Assets/FrameWork/Editor/AssetBundles/` |
| Steam 编辑器 | `Assets/FrameWork/Editor/Steamworks.NET/` |
| 项目编辑器 | `Assets/Editor/` |
| PixelDa 像素生成工具 | `Assets/FrameWork/Editor/Base/Window/PixelDa/` |
| 游戏测试编辑器 | `Assets/Editor/GameTestEditor.cs` + `GameTestEditorPartial.cs` |
| 打包游戏工具 | `Assets/Editor/GameBuildEditorWindow.cs` |
| 皮肤/装备/套装随机池配置 | `Assets/Editor/SkinRandomEditorWindow.cs` |
| 装备套装配置 | `Assets/Editor/EquipSuitEditorWindow.cs` |
| 战斗场景配置 | `Assets/Editor/FightSceneEditorWindow.cs` |
| 研究模块编辑 | `Assets/Editor/ResearchEditorWindow.cs` |
| Excel 配置目录 | `Assets/Data/Excel/` |

---

## 注意事项

1. **Editor 代码隔离**: 编辑器代码必须放在 `Editor/` 目录下，打包时不会包含。
2. **UNITY_EDITOR 宏**: 运行时引用的编辑器代码需要 `#if UNITY_EDITOR` 包裹。
3. **EditorPrefs 持久化**: 编辑器中的参数使用 EditorPrefs 保存，跨会话有效。
4. **Application.isPlaying 检查**: 编辑器按钮的操作如果需要在运行时执行，需检查 `Application.isPlaying`。
5. **MenuItem 路径**: 菜单项路径格式为 `Custom/分类/功能名`。
6. **Prefab 修改**: 编辑器代码修改 Prefab 后需要调用 `EditorUtility.SetDirty()` 或 `PrefabUtility.SavePrefabAsset()`。
