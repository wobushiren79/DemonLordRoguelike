---
name: main-create-system
description: Demon Lord Roguelike 游戏的初始创建角色(创建魔王/MainCreate)系统开发指南。使用此SKILL当需要创建或修改新开存档的创建角色界面、可选物种(当前仅骷髅 creature_id=2，人类 id=1 已移除，listSelectForCreature 硬编码)、皮肤/颜色选择(CreatureRandomInfo 皮肤随机池按部位分组、CreatureModelInfo.color_state 颜色开关、UIViewColorShow 实时变色)、创建预览(PreviewCreate 物体+CV_PreviewCreate 镜头+Spine Idle)、创建逻辑(魔王 selfCreature 固定 level=0/rarity=0、初始3魔物 NpcInfoCfg 1/2/3 不高兴/没头脑/忠心 固定属性 NpcId1→HP/2→DR/3→ASPD 与孕育同点数预算)、存档初始化(UserDataBean/SaveUserData/SetUserData/EnterGameForBaseScene)等，包括 UIMainCreate、UIViewMainCreateSelectItem 环形切换控件、UIViewMainLoadItem.OnClickForCreateGame 入口、CameraHandler.SetPreviewCreateCamera、CreatureBean.FixedAttributeForCreate/IsDemonLord/SetData、CreatureRandomInfoBean.GetAllRandomData、NpcInfoBean.GetSkins、excel_creature_info/excel_npc_info/excel_creature_random_info/excel_creature_model_info 配置等。
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

# 初始创建角色 (MainCreate) 系统开发指南

## 核心概念

**初始创建角色**是玩家新开存档时的第一个界面：为存档创建**魔王**（玩家自身角色，存入 `userData.selfCreature`）并固定赠送 **3 只初始魔物**。流程：选物种 → 逐部位选皮肤（可变色）→ 输入名字 → 确认创建 → 落盘存档 → 进入基地场景。

> **术语**：玩家视角叫「创建角色」；创建的主角是**魔王**(`CreatureBean.IsDemonLord()` 按 `creatureUUId == userData.selfCreature.creatureUUId` 判定)，赠送的 3 只是**初始魔物**(NPC：不高兴/没头脑/忠心，2 近战 1 远程)。

## 完整链路

```
UIMainLoad(加载存档界面) 空槽位 item
  └─ UIViewMainLoadItem.OnClickForCreateGame (UIViewMainLoadItem.cs:121)
       → OpenUIAndCloseOther<UIMainCreate>() + SetData(userDataIndex)   // userDataIndex=存档槽位号
            └─ UIMainCreate.OpenUI → ShowPreviewCreate(true) + InitData
                 ├─ 物种选择 ui_UIViewMainCreateSelectItem_Species（listSelectForCreature，当前仅骷髅）
                 ├─ HandleForSelectCreature(0) → new CreatureBean(creatureId) + 逐部位生成皮肤选择项
                 ├─ 皮肤项 color_state!=0 → 实例化 UIViewColorShow 颜色选择(ChangeSkinColor 实时变色)
                 ├─ Spine 预览：BaseGaming 场景 PreviewCreate/Renderer + CV_PreviewCreate 镜头
                 └─ 点创建(ui_BtnCreate) → OnClickForCreate
                      ├─ 名字空 → Toast(305) 拦截；否则确认对话框(304)
                      ├─ new UserDataBean(id/saveIndex=userDataIndex, userName)
                      ├─ 魔王 selfCreature：level=0/rarity=0/新 UUID/名字=userName
                      ├─ 初始3魔物：NpcInfoCfg 1/2/3 → CreatureBean + GetSkins + FixedAttributeForCreate
                      │    → AddBackpackCreature + AddLineupCreature(1, uuid)
                      ├─ SaveUserData + SetUserData
                      └─ ShowMask → EnterGameForBaseScene(userData, isClearWorld:false, isAnimForBuildingShow:true)
退出(ui_ViewExit / ESC) → OpenUIAndCloseOther<UIMainLoad>
```

## 各环节详解

### 1. 入口（UIViewMainLoadItem）

- 存档槽位 item 无数据时显示「创建游戏」按钮，`OnClickForCreateGame`（UIViewMainLoadItem.cs:121）打开 `UIMainCreate` 并 `SetData(userDataIndex)` 传入槽位号。
- 有数据时则是「进入游戏」`OnClickForEnterGame`（直接 `SetUserData` + `EnterGameForBaseScene`，不经过创建界面）。
- 创建界面退出（`ui_ViewExit` 或 ESC）回 `UIMainLoad`。

### 2. 物种选择（listSelectForCreature 硬编码）

- **可选物种唯一入口**：`UIMainCreate.listSelectForCreature`（UIMainCreate.cs:24-28），当前为 `{ 2 }`——**仅骷髅**；`1`=人类已于 2026-08 移除（恢复多物种就往列表加 CreatureInfo 的 id）。
- ⚠️ 列表**至少保留 1 个**：`HandleForSelectCreature(0)` 取 `listSelectForCreature[0]`，空列表会越界。
- `InitData`（UIMainCreate.cs:51）遍历物种： `CreatureInfoCfg.GetItemData(id)` → 取 `creature_random_id` → `CreatureRandomInfoCfg.GetItemData(...).GetAllRandomData()` 得 `Dictionary<CreatureSkinTypeEnum, List<long>>`（部位→皮肤id列表）缓存进 `dicSelectData`；物种选择项显示 `CreatureInfo.name_language`。
- 「随机」按钮 `OnClickForRandom`：`Random.Range` 选物种 → `HandleForSelectCreature(select, isRandom:true)`，每个皮肤项随机 `startRandomIndex`（单物种时等价于只随机皮肤）。

### 3. 皮肤 / 颜色选择

- **皮肤选择项控件** `UIViewMainCreateSelectItem`（BaseUIView）：左右按钮 `ChangeSelect` **环形回绕**（超出回到 0、小于 0 回到末尾）；`SetData(listSelect, action, startIndex)` 首调 `ChangeSelect(index, isInit:true)`；`isInit` 用于初始化时不重复刷预览（`HandleForSelectSkin` 里 `if (!isInit) SetPreviewCreate(...)`）。
- 皮肤项名字显示 `{部位名} {i+1}`（`CreatureUtil.GetCreatureSkinTypeEnumName(skinType)`）。
- **颜色选择**：选中皮肤 `CreatureModelInfoCfg.GetItemData(skinId)` 的 `color_state != 0` 时，在 `ui_SelectContent` 模板下实例化 `UIViewColorShow`（挂在 `ui_UIViewColorShow` 容器），调色回调 `ActionForSelectColor` → `createCreatureData.ChangeSkinColor(skinType, color)` 实时变色；切换皮肤/物种时销毁旧颜色选择（`DestroyImmediate` + `dicSelectColorShow` 管理），颜色默认白。
- 皮肤应用：`new SpineSkinBean(selectSkin, hasColorForSkin, colorForSkin)` → `createCreatureData.AddSkin(...)`。

### 4. Spine 预览

- `ShowPreviewCreate(true)`：`WorldHandler.GetCurrentScene(BaseGaming)` 找 `PreviewCreate` 物体（场景手建）→ 激活 + `CameraHandler.SetPreviewCreateCamera(int.MaxValue, true)`（CV_PreviewCreate，固定机位，`SetCameraForBaseScene` 封装）。
- `SetPreviewCreate(creatureData)`：`CreatureHandler.SetCreatureData(previewSpine, data)` 装配皮肤 → `SpineHandler.PlayAnim(Idle, data, true)` 播待机。
- 关闭界面 `ShowPreviewCreate(false)` 隐藏预览物体。
- ⚠️ 预览依赖 BaseGaming 场景已加载（创建界面本就在 BaseGaming 之上打开）。

### 5. 创建逻辑（OnClickForCreate，UIMainCreate.cs:148）

校验与确认：
- `ui_NameET.text` 为空 → `ToastHintText(textId=305)` 拦截。
- 确认对话框 `DialogBean`，内容 `string.Format(textId=304, 名字)`，`actionSubmit` 里执行真正创建。

**UserDataBean 初始化**：
```csharp
userData.id = userDataIndex;          // 槽位号
userData.saveIndex = userDataIndex;
userData.userName = ui_NameET.text;
```

**魔王**（`userData.selfCreature`）：
- 数据即界面上搭配的 `createCreatureData`：补 `creatureUUId`(新 UUID)、`creatureName = userName`。
- **固定** `level = 0`、`rarity = 0`（N 档无稀有度 BUFF）——魔王不走随机属性/稀有度链路。
- 魔王在管理界面的特殊处理（置顶/L 档显示/隐藏等级与献祭按钮）见 `ui-game` Agent 与 `sacrifice-system` Skill，均基于 `IsDemonLord()`。

**初始 3 魔物**（固定配置，2 近战 1 远程）：
- `NpcInfoCfg.GetItemData(i+1)`（id 1/2/3 = 不高兴/没头脑/忠心）→ `new CreatureBean(npcInfo.creature_id)`；名字取 `npcInfo.name_language`。
- 皮肤：`AddSkinForBase()`（基础皮）+ `AddSkin(npcInfo.GetSkins())`。
- **固定属性（不再随机）**：`npcInfo.id` switch → 1→HP / 2→DR / 3→ASPD，调 `creatureData.FixedAttributeForCreate(userData, type)`：
  - 点数预算 = `userData.GetUserLimmitData().gashaponRandomAttributeNum`（基础默认 5，与孕育扭蛋同口径）；
  - 底层 `CreatureAttributeBean.AddFixedAttributeForCreate`，单点增量复用 `CreatureUtil.GetAttributePointAddValue`（HP/DR 每点+10、ASPD 每点+1，故 5 点=HP+50 / DR+50 / ASPD+5）；
  - ⚠️ **此时存档尚未 `SetUserData`**，必须显式传入新建的 `userData`，不能走 `GameDataHandler.manager.GetUserData()`。
- `userData.AddBackpackCreature(creatureData)` 入背包 + `userData.AddLineupCreature(1, creatureUUId)` 直接上阵容 1。

**落盘与进场**：
```csharp
GameDataHandler.Instance.manager.SaveUserData(userData);  // 写盘
GameDataHandler.Instance.manager.SetUserData(userData);   // 设为当前存档
UIHandler.ShowMask(1, null, () =>
    WorldHandler.Instance.EnterGameForBaseScene(userData, isClearWorld:false, isAnimForBuildingShow:true), false);
```
（与 `UIViewMainLoadItem.OnClickForEnterGame` 的进场参数一致；`isAnimForBuildingShow:true` 播放建筑出现动画，对应新档开场。）

### 6. 相关 Bean 方法速查

| 方法 | 位置 | 说明 |
|------|------|------|
| `CreatureBean(long creatureId)` → `SetData` | CreatureBean.cs:54/70 | 读 CreatureInfo 名字 + `GetBodySizeRandomScale()` 体型倍率（创建时定一次，扭蛋/创建账号共用入口） |
| `FixedAttributeForCreate(userData, type)` | CreatureBeanPartial.cs:210 | 初始魔物固定加点（点数=gashaponRandomAttributeNum） |
| `IsDemonLord()` | CreatureBeanPartial.cs:108 | `creatureUUId == selfCreature.creatureUUId` |
| `GetAllRandomData()` | CreatureRandomInfoBeanPartial.cs:7 | `skin_random_data` 按 `,`/`-` 拆分、按 `CreatureModelInfo.GetPartType()` 分组成部位→皮肤列表 |
| `GetSkins(hasRandomData=true)` | NpcInfoBeanPartial.cs:46 | NPC 固有皮肤 `skin_data` 按 `&` 拆分（+随机皮肤），经 CreatureModelInfo 得部位 |
| `SetPreviewCreateCamera(p, enable)` | CameraHandler.cs:201 | → `SetCameraForBaseScene(..., "CV_PreviewCreate")` |

### 7. 配置表（Excel 唯一真实源 → JSON 导出产物）

| 用途 | Cfg 类 | JSON | Excel 源表 | 创建界面用到的字段 |
|------|--------|------|-----------|-------------------|
| 物种/基础属性 | CreatureInfoCfg | CreatureInfo.txt | excel_creature_info | `name_language`、`creature_random_id`、体型区间 |
| 初始魔物 NPC | NpcInfoCfg | NpcInfo.txt | excel_npc_info | `creature_id`、`name_language`、`skin_data` |
| 皮肤随机池 | CreatureRandomInfoCfg | CreatureRandomInfo.txt | excel_creature_random_info | `skin_random_data` |
| 皮肤模型/颜色 | CreatureModelInfoCfg | CreatureModelInfo.txt | excel_creature_model_info | `color_state`、部位类型 |

多语言 textId：304=创建确认对话框内容（含 {0} 名字占位）、305=名字为空提示（真实源 excel_language 对应工作表）。

## 约束与注意事项

- **加/减可选物种只改 `listSelectForCreature`**（UIMainCreate.cs:24-28），不动配置表；至少保留 1 个 id，且该 id 必须在 CreatureInfo 存在并配好 `creature_random_id` 皮肤池。
- 魔王创建数值**固定**（level=0/rarity=0），不要给魔王接随机属性/稀有度/BUFF 链路。
- 初始魔物**属性固定类型不随机**（HP/DR/ASPD）：改固定类型改 `OnClickForCreate` 里的 switch；改点数预算改 `UserLimmitBean.gashaponRandomAttributeNum`（会影响孕育扭蛋，两者同预算，改动需知会）。
- 初始魔物创建时存档未 `SetUserData`，任何依赖当前存档的取值都必须显式传 `userData`。
- 预览物体 `PreviewCreate` 与相机 `CV_PreviewCreate` 为场景手建资源，改名/删除会静默失效（`ShowPreviewCreate` 直接 `transform.Find`）。
- 配置改 **Excel 唯一真实源**，JSON 为导出产物；自动生成的 `*Bean.cs`（带 AUTO-GENERATED 标记）不手改，扩展写 `*BeanPartial.cs`。
- UI 继承 `BaseUIComponent`；输入走 `InputActionUIEnum`（ESC 已接 `OnInputActionForStarted`），禁用旧版 Input。
- 初始魔物的 BUFF/稀有度口径与孕育扭蛋的对照详见 `gashapon-system` Skill（`RandomAttributeForCreate`/`RandomRarityBuffForCreate` 同族方法）。
