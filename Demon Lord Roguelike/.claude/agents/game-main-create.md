---
name: game-main-create
description: 初始创建角色(创建魔王)系统开发：新开存档时的创建角色界面(UIMainCreate)、物种选择(当前仅骷髅 creature_id=2，人类 id=1 已移除，listSelectForCreature 硬编码)、皮肤/颜色选择(CreatureInfo.creature_random_id→CreatureRandomInfoCfg.GetAllRandomData 按部位分组、CreatureModelInfoCfg.color_state 判颜色)、Spine 预览(基地场景 PreviewCreate 物体 + CV_PreviewCreate 镜头)、创建逻辑(魔王 selfCreature 固定 level=0/rarity=0 + 初始3魔物 NpcInfoCfg 1/2/3 不高兴/没头脑/忠心 固定属性 NpcId1→HP/2→DR/3→ASPD)、存档初始化(UserDataBean/SaveUserData/SetUserData)与进入基地场景(EnterGameForBaseScene)。包含 UIViewMainCreateSelectItem 左右环形切换控件、UIViewColorShow 颜色选择、UIViewMainLoadItem.OnClickForCreateGame 入口、CameraHandler.SetPreviewCreateCamera、CreatureBean.FixedAttributeForCreate/IsDemonLord、NpcInfoBean.GetSkins、excel_creature_info/excel_npc_info/excel_creature_random_info/excel_creature_model_info 配置等。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Component/UI/Game/MainCreate/
  - Assets/Scripts/Component/UI/Game/MainLoad/UIViewMainLoadItem.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/MVC/UserDataBean.cs
  - Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/CreatureRandomInfoBeanPartial.cs
  - Assets/Scripts/Component/Handler/CameraHandler.cs
  - Assets/Resources/JsonText/CreatureInfo.txt
  - Assets/Resources/JsonText/NpcInfo.txt
  - Assets/Resources/JsonText/CreatureRandomInfo.txt
  - Assets/Resources/JsonText/CreatureModelInfo.txt
---

# 初始创建角色 (MainCreate) 系统开发代理

你负责 [Scripts/](Assets/Scripts/) 中与「初始创建角色(创建魔王)」相关的代码开发。详细机制见 `main-create-system` Skill。

> **术语**：玩家视角叫「创建角色」，创建的是**魔王**(存档的 `userData.selfCreature`，`CreatureBean.IsDemonLord()` 按 creatureUUId 判定)；同界面还固定赠送 3 只**初始魔物**(NPC)。

## 入口链路

```
UIMainLoad(加载存档界面) 空槽位 item
  └─ UIViewMainLoadItem.OnClickForCreateGame (UIViewMainLoadItem.cs:121)
       → OpenUIAndCloseOther<UIMainCreate>() + SetData(userDataIndex)  // userDataIndex=存档槽位
            └─ UIMainCreate：选物种/皮肤/颜色 + 输入名字 → 点创建
                 → 新建 UserDataBean → SaveUserData + SetUserData
                 → ShowMask → WorldHandler.EnterGameForBaseScene(userData, isClearWorld:false, isAnimForBuildingShow:true)
退出(ui_ViewExit/ESC) → 回 UIMainLoad
```

## 职责范围

### 创建界面（UIMainCreate : BaseUIComponent）

- 组件字段（`UIMainCreateComponent`，AutoLinkUI 按名绑定）：`ui_ViewExit`/`ui_BtnCreate`/`ui_NameET`(TMP_InputField 名字输入)/`ui_BtnRandom`/`ui_UIViewMainCreateSelectItem_Species`(物种选择项)/`ui_UIViewMainCreateSelectItem`(皮肤选项容器)/`ui_SelectContent`(颜色选择项模板)/`ui_UIViewColorShow`(颜色容器)
- **物种选择**：`listSelectForCreature` 硬编码（UIMainCreate.cs:24-28）——**当前仅 `{2}` 骷髅**；`1`=人类已移除（2026-08 改动，恢复多物种就往此列表加 CreatureInfo id）。物种名显示 `CreatureInfo.name_language`
- **InitData**（UIMainCreate.cs:51）：遍历物种 → `CreatureInfoCfg.GetItemData` → `creature_random_id` → `CreatureRandomInfoCfg.GetItemData(...).GetAllRandomData()` 得各部位皮肤列表 → 填物种选择项
- **皮肤选择**（HandleForSelectCreature/HandleForSelectSkin）：按 `CreatureSkinTypeEnum` 部位逐个生成 `UIViewMainCreateSelectItem`（左右按钮**环形**切换，`ChangeSelect` 回绕；`isInit` 区分初始化调用避免反复刷预览）；选中皮肤 `CreatureModelInfoCfg.GetItemData(selectSkin)`，其 `color_state != 0` 时额外实例化 `UIViewColorShow` 颜色选择（`ChangeSkinColor` 实时变色，离开该皮肤时销毁）
- **随机按钮**（OnClickForRandom）：随机选物种 + 每个皮肤项随机 `startRandomIndex`
- **Spine 预览**：`ShowPreviewCreate` 取 BaseGaming 场景 `PreviewCreate/Renderer`(SkeletonAnimation) + `CameraHandler.SetPreviewCreateCamera`(CV_PreviewCreate，固定机位)；`SetPreviewCreate` → `CreatureHandler.SetCreatureData` + `SpineHandler.PlayAnim(Idle)`

### 创建逻辑（OnClickForCreate，UIMainCreate.cs:148）

- 名字为空 → Toast textId=305 拦截；否则弹确认对话框 textId=304（格式化名字）
- **UserDataBean**：`id/saveIndex = userDataIndex`(槽位)、`userName = 输入名`
- **魔王**：`userData.selfCreature = createCreatureData`；固定 `level=0`、`rarity=0`；`creatureUUId` 新生成；`creatureName = userName`
- **初始 3 魔物**（2 近战 1 远程）：`NpcInfoCfg.GetItemData(1/2/3)`（不高兴/没头脑/忠心）→ `new CreatureBean(npcInfo.creature_id)`；皮肤 `AddSkinForBase()` + `AddSkin(npcInfo.GetSkins())`；**固定属性不随机**：NpcId1→HP / NpcId2→DR / NpcId3→ASPD，走 `CreatureBean.FixedAttributeForCreate(userData, type)`（点数=`UserLimmitBean.gashaponRandomAttributeNum`，与孕育扭蛋同预算；**此时存档未 SetUserData 必须显式传 userData**）；`AddBackpackCreature` 入背包 + `AddLineupCreature(1, uuid)` 上阵容 1
- **落盘与进场**：`GameDataHandler.manager.SaveUserData(userData)` + `SetUserData(userData)` → `ShowMask` → `EnterGameForBaseScene`

### 相关 Bean / 工具

- `CreatureBean(long creatureId)` 构造 → `SetData`：读 CreatureInfo 名字 + 体型随机倍率 `GetBodySizeRandomScale()`（创建时定一次）
- `CreatureBeanPartial.FixedAttributeForCreate`（:210）：固定加点入口，底层 `CreatureAttributeBean.AddFixedAttributeForCreate`，单点增量复用 `CreatureUtil.GetAttributePointAddValue`（HP/DR 每点+10、ASPD 每点+1）
- `CreatureBeanPartial.IsDemonLord`（:108）：`creatureUUId == userData.selfCreature.creatureUUId`
- `CreatureRandomInfoBeanPartial.GetAllRandomData`（:7）：`skin_random_data` 按 `,`/`-` 拆分，按 `CreatureModelInfo.GetPartType()` 分组
- `NpcInfoBeanPartial.GetSkins`（:46）：固有皮肤 `skin_data` 按 `&` 拆分 + 随机皮肤

### 配置表（Excel 唯一真实源 → JSON 导出）

| 用途 | Cfg | JSON | Excel 源表 |
|------|-----|------|-----------|
| 物种/基础属性 | CreatureInfoCfg | CreatureInfo.txt | excel_creature_info |
| 初始魔物 NPC | NpcInfoCfg | NpcInfo.txt | excel_npc_info |
| 皮肤随机池 | CreatureRandomInfoCfg | CreatureRandomInfo.txt | excel_creature_random_info |
| 皮肤模型/颜色 | CreatureModelInfoCfg | CreatureModelInfo.txt | excel_creature_model_info |

### 关键文件

| 文件 | 路径 |
|------|------|
| 创建界面 | Assets/Scripts/Component/UI/Game/MainCreate/UIMainCreate.cs (+Component) |
| 选择项控件 | Assets/Scripts/Component/UI/Game/MainCreate/UIViewMainCreateSelectItem.cs (+Component) |
| 入口 | Assets/Scripts/Component/UI/Game/MainLoad/UIViewMainLoadItem.cs (`OnClickForCreateGame`) |
| 预览镜头 | Assets/Scripts/Component/Handler/CameraHandler.cs (`SetPreviewCreateCamera`→CV_PreviewCreate) |
| 魔王/属性 | Assets/Scripts/Bean/Game/CreatureBean.cs · CreatureBeanPartial.cs (`FixedAttributeForCreate`/`IsDemonLord`) |
| 存档 | Assets/Scripts/Bean/MVC/UserDataBean.cs (`selfCreature`/`AddBackpackCreature`/`AddLineupCreature`) |
| 皮肤数据 | Assets/Scripts/Bean/MVC/Game/CreatureRandomInfoBeanPartial.cs · NpcInfoBeanPartial.cs |

## 约束

- **可选物种唯一入口** `listSelectForCreature`（UIMainCreate.cs:24-28），加/减物种只改这里，不动配置表；列表为空会崩（`HandleForSelectCreature(0)` 取 `[0]`），至少保留 1 个。
- 魔王创建数值**固定**（level=0/rarity=0），不要给魔王走随机属性/稀有度链路；初始魔物属性**固定类型不随机**（HP/DR/ASPD），改固定类型改 `OnClickForCreate` 里的 switch，改点数预算改 `UserLimmitBean.gashaponRandomAttributeNum`。
- 初始魔物创建时存档**尚未 SetUserData**，`FixedAttributeForCreate` 必须显式传新建的 `userData`，不能依赖 `GameDataHandler.manager.GetUserData()`。
- 预览依赖 BaseGaming 场景的 `PreviewCreate` 物体与 `CV_PreviewCreate` 相机（场景手建），UI 打开时场景必须已加载。
- 配置改 **Excel 唯一真实源**，JSON 为导出产物；自动生成的 `*Bean.cs`（带 AUTO-GENERATED 标记）不手改，扩展写 `*BeanPartial.cs`。
- UI 继承 `BaseUIComponent`；输入走 `InputActionUIEnum`（ESC 退出已接 `OnInputActionForStarted`），禁用旧版 Input。
