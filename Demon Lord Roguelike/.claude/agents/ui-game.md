---
name: ui-game
description: 游戏主UI开发：战斗界面、基地界面、世界地图、设置界面、对话框界面等所有 Game 层级的 UI。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Component/UI/Game/
  - Assets/Prefabs/UI/Game/
---

# 游戏 UI (Game UI) 开发代理

你负责 [Scripts/Component/UI/Game/](Assets/Scripts/Component/UI/Game/) 中所有游戏主 UI 的开发。

## 职责范围

### 主菜单 UI
- **UIMainStart** - 游戏开始界面
- **UIMainCreate** - 创建角色界面
- **UIMainLoad** - 加载存档界面
- **UIMainMaker** - 制作人员界面

### 基地 UI
- **UIBaseMain** - 基地主界面
- **UIBaseCore** - 核心建筑界面
- **UIBasePortal** - 传送门界面
- **UIBaseResearch** - 研究界面

### 战斗 UI
- **UIFightMain** - 战斗主界面
- **UIFightSettlement** - 战斗结算界面
- **UIFightAbyssalBlessing** - 深渊祝福选择

### 游戏功能 UI
- **UIGameSetting** - 游戏设置界面
- **UIGameSystem** - 游戏系统界面
- **UIGameWorldMap** - 世界地图界面
- **UIGameConversation** - 对话界面
- **UIRewardSelect** - 奖励选择界面
- **UICreatureVat** - 魔物进阶界面（CreatureVat 培养槽）

### 魔物进阶 (UICreatureVat / CreatureVat 培养槽) 要点

- **打开入口与退出去向（`actionForExit` 注入式）**：`public Action actionForExit` 由**各入口**注入，`OnClickForExit` 只 `actionForExit?.Invoke()`（不含任何默认分支，退出逻辑全在入口）。三个入口：① 基地核心 `UIBaseCore.OnClickForVat` → 注入回 `UIBaseCore`；② 测试 `LauncherTest`（`TestSceneTypeEnum.CreatureVat`）→ 注入回 `UIBaseCore`（与核心一致）；③ 场景 Vat 建筑按 E 交互（`ControlForGameBase` 的 `ControlInteractionEnum.VatInteraction`，见 control-system skill）→ 注入回 `UIBaseMain`（退出直接回场景，不经核心）。与 `UIAchievement` 同款「入口注入退出回调」模式。
- **进阶效果**：目标魔物稀有度 +1，并把开始时即确定的「预定 BUFF」写入 `creatureData.dicRarityBuff[新稀有度]`（旧实现「完成」只清空槽、对生物无任何加成）。
- **目标列表**：仅 Idle 且未满级（`RarityInfoCfg.GetAscendTimeByRarity(rarity) > 0`，即排除 L）。**默认排序**（`InitCreaturekDataForTarget` 内 `List.Sort`）：稀有度升序（N→L），同稀有度按等级降序。
- **素材列表**：Idle + 排除目标 + 排除上阵（`UserDataBean.CheckIsInAnyLineup`）+ 仅保留稀有度**高于**目标的魔物；可选上限做成研究 `GetMaterialMax()`=`UserUnlock.GetUnlockCreatureVatMaterialMax()`（基础 `UserLimmitBean.creatureVatMaterialMax`=5 + `UnlockEnum.CreatureVatMaterialNum`(100000008) 研究等级,满级10），超出弹 Toast（文本 id 80011）。**默认排序**（`InitCreaturekDataForMaterial` 内 `List.Sort`）：目标**下一阶段稀有度**（=目标稀有度+1）置顶，其余按稀有度升序，同稀有度按等级降序（因素材只保留高于目标者，下一阶段即最低合格稀有度）。
- **素材上限文本（LimmitText）**：`RefreshMaterialLimitText()` 显示「已选/上限」，达上限时数量转通用警示红（`ColorUtil.WrapLimitFull`）；在 `InitCreaturekDataForMaterial` 与素材选择变化时刷新。
- **预定 BUFF 生成**：开始进阶时调用 `BuffUtil.CreateAscendRarityBuff(newRarity, materials)` 得到 `ascendBuff`（素材在 newRarity 槽位的 BUFF 按 id 聚合，每 id 提供 10%×数量 命中概率，命中继承并重随机数值≥素材原值，未命中回退通用随机；UR/L 无类型则为 null）。
- **进阶详情 UI（AscendData，素材选择阶段展示）**：仅在「素材选择阶段（`userAscendDetails==null`）+ 已选目标」时显示 `ui_AscendData` 并隐藏 `ui_ProgressContent`（培养阶段反之）；统一在 `RefreshAscendData()` 切换，由 `RefreshVatState` 及目标选择事件触发。`ui_ProgressContent` 未在 Component 文件序列化，靠运行时 `AutoLinkUI` 按名绑定。
  - **升阶前/后卡牌**：`ui_UIViewCreatureCardItem_BeforeAscend/_AfterAscend` 用 `CardUseStateEnum.ShowNoPopup`（关闭 popup 详情）；After 卡用 `BuildAscendPreviewCreature(target, newRarity)`（值字段复制+稀有度+1，引用字段共享只读展示）。两卡 `PlayCardDropIn` 从上掉落 + OutBack 缩放（DOTween）。
  - **AscendIcon**：向右戳循环 Animator（`Assets/LoadResources/Anim/UI/UICreatureVatAscendIcon.controller`，动 `m_AnchoredPosition.x`）。
  - **BUFF 增益概率面板（AscendBuffs）**：`BuffUtil.GetCreatureAscendBuffChances(newRarity, materials)` 算各 BUFF 命中概率（素材 BUFF 在前、`随机增益` 兜底在后，默认无素材时 100%）；子项 `UIViewCreatureVatAscendBuffItem`（`ui_AscendBuffItemName`/`ui_AscendBuffItemRate`/`ui_BG_Image`/`ui_BG_PopupButtonCommonView`）实时克隆/复用缓存（`listAscendBuffItem`），一排最多 5 个、超出 y 轴下移，出现/消失/移动均 DOTween。`SetData(chance, rarity)`：名字字体+BG背景按稀有度配色（`RarityInfo.buff_color`，与 `UIViewBuffShowItem` 同口径）；BG 带 `PopupButtonCommonView` 悬浮提示该 BUFF 内容（`GetBuffContentForPreview(chance)`，取 `content_language`）。**占位参数按「进阶增益范围预览」研究(`UnlockEnum.CreatureVatBuffPreview`, unlock_id 100000006)是否解锁分档**：未解锁 → `{..}` 统一 Regex 替成 `???`；已解锁 → `BuildUnlockedRangeContent`：唯一随机值 `{Percentage}`(=`trigger_value_rate_min~trigger_value_rate`) 显示整数百分点 `min~max` 范围（素材命中该 id 时下限抬高，与 `BuffBean.CreateRandomWithFloor` 同口径，下限取 `chance.floorValueRate`），`{Time_S}`/`{KillNum}` 等固定条件显示实际值（与 `UIViewBuffShowItem` 同口径）。随机增益兜底项(`buffId≤0`)始终给通用说明。
- **耗时**：按**源稀有度**查表 `RarityInfoCfg.GetAscendTimeByRarity`（秒）作为 `timeMax`；被动 tick 每秒 +1 秒。
- **魔晶加速（研究门控）**：加速按钮做成研究解锁——`UserUnlock.GetUnlockCreatureVatAddProgressLevel()`(`UnlockEnum.CreatureVatAddProgress`=100000007,level_max=5)。**0级(未研究)隐藏加速按钮**；已研究时**恒消耗 1 魔晶**，研究等级 = 单次进度增加秒数 = 进度倍率（Lv1=1魔晶+1秒 … Lv5=1魔晶+5秒），按钮文本 80009「加速进阶 +{等级}秒/晶」（{0}=等级=每次加速秒数），`OnClickForAddProgress` 消耗 `payCrystal=1` 并 `AddProgress(等级)`。
- **临时进阶数据**：`UserAscendBean` / `UserAscendDetailsBean`（随存档序列化）—— `progress` 现为「已累积秒数」，`targetRarity`/`timeMax`/`ascendBuff` 字段，`AddProgress(+1秒)`、`IsComplete()`、`GetProgressNormalized()`；进度条/完成判定改用后两者。`AddAscendData(index, creatureData, targetRarity, timeMax, ascendBuff)`。
- **进度条配色（`RefreshVatProgress`）**：`ui_ProgressText`（百分比文本）与 `ui_Progress`（fillAmount 进度条）的 `color` 均按归一化进度 `ColorUtil.GetProgressColor(progressNormalized)` 分段着色（0-20红/20-40橙/40-60黄/60-80浅绿/80-100蓝，与献祭成功率、孵化缸 BUFF 概率同口径）。
- **存档时机**：开始进阶存一次、点完成存一次；培养过程（进度 tick / 魔晶加速）不主动存档。
- **测试模式不落盘**：`OnClickForStart`/`OnClickForComplete` 直接 `SaveUserData()`，不落盘由全局 `GameDataManager.isTestSimulation` 在存档层统一拦截（魔物进阶测试 `TestSceneTypeEnum.CreatureVat` 与献祭测试共用，见 test-system skill），本 UI 不再自带测试标记。
- **完成进阶收尾（`OnClickForComplete`）**：落地数据(升稀有度+授予 BUFF)→`RemoveAscendData`(复位 Idle)→存档→`BuildingVatSetState(0)` 清空容器→**反馈**：胜利音效 `AudioEnum.sound_win_1` + 容器处庆祝粒子 `EffectHandler.ShowCreatureAscendCompleteEffect(vat.position+(0,1.2,0), rarityColor)`——**专用粒子 `EffectAscendComplete_1`**(白模板,2 套 ParticleSystem:上升流光 streak + 径向环爆,URP additive,Addressables 组 `Effect`),运行时按升阶后**新稀有度主色** `RarityInfo.ui_board_color` 给所有 PS `startColor` 上色(即"稀有度流光";`ShowEffect(EffectBean)` 走 `isPlayInShow=false`→回调里 tint 后 `PlayEffect()`) + 成功 Toast `ToastHintText(GetTextById(80013), 1)`(绿色)→**刷新列表**：`targetCreatureSelect=null`+清素材选择+`InitCreaturekDataForTarget()` 重建目标列表（否则列表仍是进阶前旧稀有度），再 `RefreshVatState()`/`RefreshVatProgress()`。
- **被动进度**：`GameDataHandler.HandleForAscendData` 每秒 tick 给每个进阶容器 `AddProgress()`，仅广播 `CreatureAscend_AddProgress`，不存档。
- **配置**：进阶耗时来自 `excel_rarity_info` 新列 `ascend_time`（按源稀有度秒数：N=180/R=600/SR=1800/SSR=7200/UR=36000/L=0），手写字段在 `RarityInfoBeanPartial.cs`，访问走静态 `GetAscendTimeByRarity(int rarity)`（rarity≤0 视为 N，缺失/满级返回 0）。

### 通用 UI
- **UICommonLoading** - 通用加载界面
- **UICommonMask** - 通用遮罩
- **UIScreenLock** - 屏幕锁定

## 代码模板（普通UI）

```csharp
public class UIExample : BaseUIView
{
    public Button ui_Submit;
    public Text ui_Title;

    public override void Awake() { base.Awake(); }
    public override void OpenUI() { base.OpenUI(); }
    public override void RefreshUI(bool isOpenInit = false) { base.RefreshUI(isOpenInit); }
    public override void OnClickForButton(Button viewButton) { base.OnClickForButton(viewButton); }
}
```

## 约束

- 普通 UI 继承 BaseUIView，使用 `UI` 前缀命名
- Prefab 放置在 `Assets/Prefabs/UI/Game/`
- 新增 UI 后需更新 MD/ProjectUI.md 文档
- 按钮点击在 OnClickForButton 中统一处理
