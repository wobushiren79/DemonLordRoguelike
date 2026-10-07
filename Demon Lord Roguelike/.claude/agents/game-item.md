---
name: game-item
description: 道具系统开发：道具创建/装备/使用、背包系统、道具商店、道具信息弹窗。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Bean/Game/ItemBean.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - Assets/Scripts/Utils/ItemsUtil.cs
  - Assets/Scripts/Component/UI/Common/Item/
  - Assets/Scripts/Component/UI/Common/ItemSelect/
  - Assets/Scripts/Component/UI/Common/Backpack/
  - Assets/Scripts/Component/UI/Popup/ItemInfo/
---

# 道具系统 (Item System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与道具相关的代码开发。

## 职责范围

### 道具数据
- **ItemBean / ItemBeanPartial** - 道具基础数据（运行时实例含 `rarity` 品质；`juicerExp` 魔汁经验值，仅 Juice 类型有效，旧存档无此字段默认 0 兼容）
- **ItemsEnum** - 道具枚举定义（`ItemTypeEnum`：装备部位 Hat=1~Weapon=10 + **消耗品 Juice=11（魔汁，限非魔王）/ TransformPotion=18（幻化药）/ RestorePotion=19（幻原药），均非装备、两药含魔王可用** + Portrait=101 头像；`ItemIdEnum.Juice = 200001` / `RestorePotion = 200003`——内置幻化药 200002 已删除、枚举同步移除，幻化药全部由 Mod 提供；`ItemSourceEnum.ConquerReward = 1` 征服模式奖励来源）
- **ItemsInfoBean** - 道具配置信息（来自 Excel）
  - `reward_rarity`（string，逗号分隔稀有度ID，空=全稀有度）：**奖励可出稀有度白名单**。空表示该道具在任意稀有度奖励中都可能产出；配了(如 `5,6`)则仅在 UR/L 稀有度的奖励里出现。辅助方法在 `ItemsInfoBeanPartial`：`GetRewardRarityList()`（解析缓存）、`IsMatchRewardRarity(int rarity)`（空白名单→true）。注意与 `ItemBean.rarity`（运行时实例品质）语义不同。
  - 消费点：`RewardSelectBean.CreateItemEquip`（征服/传送门装备奖励池）先定目标稀有度→按 `IsMatchRewardRarity` 过滤道具池→随机取一件；过滤后为空回退发魔晶。**仅**作用于装备奖励生成，扭蛋/其它路径不受影响。
  - `source`（string，逗号分隔 `ItemSourceEnum` 枚举值，空=默认来源）：**道具来源白名单**。辅助方法 `GetSourceList()`（解析缓存）、`HasSource(ItemSourceEnum)`、`IsEquipType()`。消费点：`RewardSelectBean.ReplaceCrystalSlotBySourceItem`——source 含 ConquerReward 且非装备的道具进候选池，征服奖励随机一个魔晶位替换为池内随机道具（数量1；无候选/全装备/测试模式不替换）。首个实例=AeonsEchoSpine 341 个幻化药全配 `source="1"`。
  - 编辑工具：菜单「游戏/道具稀有度配置」（`Assets/Editor/ItemRarityConfigEditorWindow.cs`）——虚拟化列表(图标懒加载)列出所有道具、同名相邻，右侧稀有度枚举勾选，保存写 Excel + 定向补丁 `ItemsInfo.txt` 的 `reward_rarity`。顶部支持名字搜索 + `item_type` 类型筛选 + **物种(creature_model_id→CreatureModel remark，0=通用)筛选**。新增该列后需在 Unity 对 ItemsInfo「生成 Entity」使 Bean 字段生效。

### 道具管理
- **ItemsUtil** - 道具工具类
- **GameDataHandler** / **GameDataManager** - 游戏数据处理（含道具持久化）

### 道具 UI（`Common/Item/`）
- **UIViewItem** - 道具项**基类**（公共字段 itemData + SetData/SetIcon/SetNum/SetItemBG/SetItemPopup/OnClickForButton；SetItemBG 按 itemData.rarity 用 RarityInfo.ui_board_color_item 给 ui_ItemBG 上色，空槽位/缺配置回退白色）
- **UIViewItemBackpack** - 背包道具项（`: UIViewItem`，加 creatureData + SetData(item,creature)；右键经 `ui_UIViewItem` 同物体 Button 旁的 PopupButtonCommonView 转发（Awake `AddListenerForRightClick` → `EventForRightClick`，itemData 非空时触发 `EventsInfo.UIViewItemBackpack_OnRightClickSelect` 并 `ClearData` 隐藏悬浮详情；左键仍走 Button.onClick → `OnClickForSelect` → `UIViewItemBackpack_OnClickSelect`）
- **UIViewItemEquip** - 装备项（`: UIViewItem`，加 itemTypeEnum + 空槽位占位图标/部位名）
- **UIViewItemBackpackList** - 背包列表（在 `Common/Backpack/`）
- **UIViewStoreItem** - 商店道具项
- **UIPopupItemInfo** - 道具信息气泡（幻化药额外展示 show/ui_show 形象预览区，见下方「幻化药 / 幻原药」的「详情气泡预览」）

### 道具选项控件（`Common/ItemSelect/`）
- **UIViewItemSelect** - 道具选项通用控件（prefab `Resources/UI/Common/UIViewItemSelect.prefab`，内嵌送礼/丢弃/装备三个 `UIViewItemSelectChild` 按钮）：`SetData(actionForGift/Delete/Equip)` **传入回调即显示对应按钮、为空隐藏**，业务全由使用方回调处理；`ShowSelect(itemData, targetTF)` 记录选中道具并用 `UGUIUtil.GetRootPos` 把选项列表定位到目标处；点全屏透明背景或任意选项均 `CloseSelect`，点选项先关闭再以 `Action<ItemBean>` 回调。使用方：`UIDialogSelectItem`（按 Bean 回调显隐）、`UICreatureManager`（右键弹出，只显示装备+丢弃）

### 道具相关 UI
- **UIDialogSelectItem** - 道具选择弹窗（内嵌 `UIViewItemSelect`，选项显隐由 `DialogSelectItemBean` 回调是否传入决定）

### 魔汁（Juice，首个消耗品类道具）

道具类型不再只有装备部位 + 头像：**魔汁是首个消耗品**（`ItemTypeEnum.Juice = 11`，紧随 Weapon=10），由榨汁产出、对魔物使用加经验。消耗品后续新增幻化药=18/幻原药=19（见下文「幻化药 / 幻原药」）。

- **数据**：`ItemBean.juicerExp`（long 实例字段）存每个魔汁的经验值，榨汁时按投入魔物等级汇总写入（产出端 `CreatureJuicerLogic.SettleJuiceReward`，详见 juicer-system）；旧存档无此字段 JSON 反序列化默认 0 兼容。
- **配置**：excel_items_info 新行 id=200001（item_type=11、`num_max=1` 不堆叠——每个魔汁实例经验不同故不入堆、creature_model_id=0、icon_res=`Item_Juicer_1` 无图集后缀走默认 Items 图集、name textId=200001）。入账走 `userData.AddBackpackItem(itemBean)` 不堆叠重载（每个魔汁是独立 ItemBean）。
- **使用流程**（魔物管理页 `UICreatureManager`）：`EventForItemBackpackClickSelect` 点击统一进 `UseOrEquipItem(itemData)` 分流——Juice → `UseJuiceItem`（`#region 魔汁使用`）、TransformPotion → `UseTransformPotionItem`、RestorePotion → `UseRestorePotionItem`，其余道具照旧 `SetCreatureEquip`。`UseJuiceItem`：无选中生物/魔王兜底返回（列表已隐藏魔汁）→ `IsMaxLevel()` 满级 Toast 61015 拦截 → `UIHandler.ShowDialogNormal` 确认框（textId 61014，格式化生物名+juicerExp）→ 确定回调：`creatureData.levelExp += juicerExp` → `RemoveBackpackItem` → `SaveUserData()` → 三连刷新（`SetCardDetails` 经验显示 + `RefreshSacrificeButton` 献祭按钮点亮 + `InitBackpackItemsData` 列表移除）。**经验只累计 levelExp 不自动升级**（沿用战斗结算加经验语义，升级仍走献祭 CanUpLevel/UpLevelForSacrifice）。
- **列表过滤**：`UIViewItemBackpackList.FilterItems` 保留条件 = `itemData.CanEquipForCreature(creatureData)`（含魔王专属校验：选中非魔王生物时魔王专属装备直接隐藏）或（`GetItemType()==Juice` 且 `!creatureData.IsDemonLord()`）或 `GetItemType()==TransformPotion` 或 `GetItemType()==RestorePotion`（**两药不带 IsDemonLord 排除**——魔王选中时两药可见可用，魔汁仍限非魔王）；`UIDialogSelectItem`（creatureData=null）走 `AddValidItems` 显示全部配置有效道具。**失效道具统一隐藏**：配置缺失（所属Mod未开启/已删除）的道具在所有分支一律跳过不展示（有上下文分支 `itemInfo==null continue`、无上下文分支 `AddValidItems`），数据保留在存档待 Mod 重开后恢复；`ItemBean.GetItemType()` 对 itemsInfo==null 兜底返回 `(ItemTypeEnum)0` 防排序崩溃，`itemsInfo` getter 查询失败只打一次日志（`_isItemsInfoQueried` 标记防刷屏）。
- **气泡**：`UIPopupItemInfo.SetJuiceExp`（SetData 末尾调用）——Juice 类型显示 `ui_JuiceExpText` 并填 textId 61017「经验+{0}」，其余道具隐藏；字段经 AutoLinkUI 按名绑定（prefab Details 节点下 `JuiceExpText`，默认隐藏），为 null 容错跳过；魔汁 dicAttribute 为空故属性区自动隐藏，两者互斥。注意：`ui_JuiceExpText` 与 `ui_UIViewPopupItemAttribute`（属性行模板）目标均是非激活对象，**未经编辑器序列化**（YAML fileID:0）只能靠 Awake AutoLink——`SetData` 入口已带 `ui_UIViewPopupItemAttribute == null` 时补 `AutoLinkUI()` 的容错（防 SetData 先于 Awake 被调用时属性区静默空白，详见 ui-popup agent「字段绑定时序容错」）。
- **相关配置**：LevelInfo 新增 `juicer_exp` 列（1~10 级 = 同级升级经验 100%，另有 id=0 行=20=1级的20%）；excel_language UIText sheet 新增 61014/61015/61016/61017（12 语种），ItemsInfo sheet 新增 id=200001「魔汁」。

### 幻化药 / 幻原药（TransformPotion=18 / RestorePotion=19）

第二、三个消耗品，**所有生物含魔王可用**（与魔汁限非魔王不同）：幻化药把生物 spine 形象整骨替换为配置资源，幻原药清除幻化恢复原形象。**内置幻化药（原 id=200002）已于 2026-09-21 删除**——幻化药道具现全部由 Mod 提供（AeonsEchoSpine 341 个，`source="1"` 可经征服模式奖励获取）；幻原药保留内置（`ItemIdEnum.RestorePotion = 200003`）。使用分流只判 `ItemTypeEnum`（18/19）不判具体 id，代码路径不受影响。

- **数据**：`CreatureBean.transformItemId`（long 持久化字段，0=无幻化；旧存档默认 0 兼容）——**只存道具ID不存资源名**（Mod 保底核心）；`CreatureBeanPartial.ClearTempData()` 增加 transformItemId=0 重置。
- **形象解析唯一入口**：`CreatureBeanPartial.GetTransformSpineRes()`（`#region 幻化相关`）——共用前置校验抽到 `GetTransformItemInfo()`（transformItemId=0→null；配置缺失→每 id 一次 LogError（静态 `loggedMissingTransformIds` 防列表刷屏）+null；类型非 TransformPotion→null（防 Mod id 复用）；other_data 空→null）；`ParseTransformOtherData` 解析键值格式 `show_res:X&ui_show_res:X&ui_show_data:scale;x,y&show_data:scale;x,y`（`&`拆项、`:`拆键值、缺省键省略），`GetTransformSpineRes` 只取 show_res 键。
- **高清展示（ui_show_spine）**：`GetTransformUIShowSpineRes()` 取 ui_show_res 键（可空）——`CreatureHandler.SetCreatureData` 在 isUIShow=true 时优先改用 avator 资源；`GetTransformUIShowData()` 取 ui_show_data 键详情UI尺寸（格式同 ui_data_b；小卡另有 show_data 键，世界显示另有 world_data 键=缩放乘算+spine子节点偏移[**仅 hasTransform(有 show_res) 时消费**，2026-10-01 起消费口径统一标注]，编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先）——`GameUIUtil.SetCreatureUIForDetails` 用它替代原生物 `ChangeUISizeForB`（原值按原骨架校准，不适用 Mod 高清骨架）。
- **展示机制**：`CreatureHandler.SetCreatureData` 中枢注入——幻化时 resName 换幻化资源、`ChangeSkeletonSkin` 两分支包 `if (!hasTransform)` 跳过套皮（传 null 不够，末尾 SetSkin(空) 会清默认外观）；自动覆盖详情UI/列表小图标/对话头像/基地/议会。战斗场景经 `GetFightCreatureObj` 新可选参数 `resNameOverride`（`CreateDefenseCreature` 与 `CreateDefenseCoreCreature` 均传 `GetTransformSpineRes()`）。游戏层 `SpineHandler.GetAnimNameAppoint` 开头守卫：幻化时返回 null（原生物 anim_* 配置名不适用新骨架，交框架按目标骨架动画列表解析，缺失仅日志不播防 ArgumentException；**Idle 例外**=幻化药 idle_anim 键配置的替代待机动画优先按名直播，2026-09-28 起，见 spine-system / mod-system SKILL）。形象尺寸按原生物 creatureModel 缩放（详情UI有自带尺寸段时除外）。
- **优先级与语义**：Portrait > 幻化 > 原形象（`GameUIUtil.SetCreatureUIForDetails` 的 Portrait 分支在 SetCreatureData 之后再覆盖，零逻辑改动仅补注释）；连续吃幻化药后者覆盖前者；幻化整骨替换不套原皮肤。
- **使用流程**（`UICreatureManager`，经 `UseOrEquipItem` 分流）：`UseTransformPotionItem`——配置缺失或 other_data 空→Toast 61021 拦截；确认框 textId 61018（{0}生物名{1}道具名）→ 写入 transformItemId（覆盖旧值=以最后吃的为准）+ `RemoveBackpackItem` 消耗 + `SaveUserData()` 落盘 + 四连刷新（`SetCardDetails` + 生物卡片列表 `ui_UIViewCreatureCardList.RefreshAllCard()`(幻化形象刷新) + `InitBackpackItemsData` + `RefreshBaseControlForDemonLord`）。`UseRestorePotionItem`——transformItemId==0→Toast 61020 不消耗拦截；确认框 61019 → 置 0 恢复（同样四连刷新）。`RefreshBaseControlForDemonLord`：魔王专属，同步基地走路 spine（非基地场景防护）。
- **Mod 保底（核心语义）**：只存道具ID、展示时实时查 ItemsInfoCfg——Mod 提供幻化药时 Mod 移除→配置 null→所有展示路径自动回落原形象；Mod 装回自动恢复；幻原药只判 id==0 不读配置，Mod 没了也能清残留；spine 资源缺失经 `GetSkeletonDataAssetWithMod` 回落 + null-check 不崩。
- **详情气泡预览**（2026-10-02 起）：`UIPopupItemInfo.SetTransformPreview`（SetData 末尾调用）——仅幻化药（TransformPotion 且 other_data 非空）显示预览区 `ui_TransformPreviewContent`（prefab Details 节点下 460x260，默认隐藏），区内 show/ui_show 两段独立显隐：show 段（`ui_ShowArea`，有 show_res 键才显示，标签 61022「战斗形象」）直接 `SpineHandler.SetSkeletonDataAsset(ui_ShowSpine, show_res)` 加载 + `PlayAnim(animNameAppoint: idle_anim 键,缺省 null 走框架候选)`；ui_show 段（`ui_UIShowArea`，有 ui_show_res 键才显示，标签 61023「详情形象」）同法加载 + ui_show_skin 换肤（「|」分隔多皮肤叠加；无皮肤键但当前挂着指定皮肤时 `SetSkin(null)+SetupPoseSlots` 重置防 popup 复用残留）+ ui_show_idle_anim。**预览尺寸走 `GameUIUtil.ApplyCardIconSizeFit` 卡片图标尺寸等比适配**（show 段传 `cardContentHeightForS`、ui_show 段传 `cardContentHeightForB`）：预览 spine 节点与卡片 ui_Icon 同 pivot(0.5,0)+父容器中心锚定，尺寸键 scale;pos 同乘「预览框高/卡片标准容器高」系数还原小卡/大卡显示效果；尺寸键解析收口于静态 `CreatureBean.ParseTransformSizeData`。直接套尺寸键不乘系数会把骨架推出框外被 RectMask2D 裁光（OtherSpine 药 ui_show pos.y=-226 曾因此全空白，2026-10-02 修复）。spine 矩形由布局固定且套在 ShowRect→ShowRectContent 两层 RectMask2D 裁切容器内（超出 230x240 框部分像素级裁切），spine 节点（ShowSpine/UIShowSpine）= SkeletonGraphicExtend + SkeletonAnimation 配对（SkeletonGraphicDefault 材质）。
- **配置**：excel_items_info id=200003（item_type=19、num_max=1、icon_res=`Item_Potion_1`、name=200003）保留；原 id=200002 内置幻化药行已删除（幻化药全部由 Mod 提供，图标仍复用内置 `Item_Potion_1`）；num_max=1 因 `RemoveBackpackItem` 整 Bean 移除不做递减；other_data 语义=spine 资源组合串（`chessRes` 或 `chessRes,avatorRes|uiScale;x,y`，资源名须为 SkeletonDataAsset 的 Addressables 资源名/Mod catalog key，chess 段须含完整动画集 Idle/Walk/Attack/Dead）。excel_language ItemsInfo sheet 的 200002 道具名行已同步删除（200003 保留，12 语种）、UIText sheet 加 61018（幻化确认）/61019（幻原确认）/61020（无幻化拦截）/61021（配置异常拦截）/61022（预览标签「战斗形象」）/61023（预览标签「详情形象」）。
- **Mod 幻化药实例**：AeonsEchoSpine Mod（回响幻化药 341 个，Chess×Avator 笛卡尔积）由 `Mods/AeonsEchoSpine/JsonText/ItemsInfo.txt` 注入，全部配 `source="1"`（征服模式奖励来源：征服通关领奖随机一个魔晶位替换为池内随机幻化药，详见 fight-reward-system）；Mod 道具 `name` 的 modId 拼接由 **Excel 列头 `name[language]` 标记驱动**——`ExcelEditorWindow.CreateEntity` 生成 `ItemsInfoBean.cs` 时自动产出 `CombineModReferenceIds` 重写（指向 Mod 自带语言表同自ID 行，name=0 不拼接，**不能复用主游戏 textId**），无需手写。生产流程见 agent **aeonsecho-spine-mod** / skill `aeonsecho-spine-mod`。

## 关键文件

| 文件 | 路径 |
|------|------|
| ItemBean | Assets/Scripts/Bean/Game/ItemBean.cs |
| ItemsEnum | Assets/Scripts/Enums/ItemsEnum.cs |
| ItemsUtil | Assets/Scripts/Utils/ItemsUtil.cs |
| 道具项（基类+装备+背包） | Assets/Scripts/Component/UI/Common/Item/ |
| 背包列表 | Assets/Scripts/Component/UI/Common/Backpack/ |
| 道具信息气泡 | Assets/Scripts/Component/UI/Popup/ItemInfo/ |
| 道具配置Bean(含 reward_rarity 辅助) | Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs |
| 道具稀有度配置编辑器 | Assets/Editor/ItemRarityConfigEditorWindow.cs |
| 幻化相关 | Assets/Scripts/Bean/Game/CreatureBeanPartial.cs（`GetTransformSpineRes`）· Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs（`UseOrEquipItem`/`UseTransformPotionItem`/`UseRestorePotionItem`） |

## 约束

- 新增道具类型需在 ItemsEnum 中添加枚举
- 道具数据变更后需刷新相关 UI
- 道具弹出信息使用 Popup 类型 UI
