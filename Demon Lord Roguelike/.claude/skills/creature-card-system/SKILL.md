---
name: creature-card-system
description: Demon Lord Roguelike 游戏的生物卡片(CreatureCard)系统开发指南。使用此SKILL当需要创建或修改生物卡片UI、卡片交互、卡片状态、卡片列表展示、卡片详情弹窗等，包括战斗卡片/阵容卡片/管理卡片/献祭卡片/进阶卡片等类型。
watched_files:
  - Assets/Scripts/Component/UI/Common/CreatureCard/
  - Assets/Scripts/Component/UI/Popup/UIPopupCreatureCardDetails.cs
  - Assets/Scripts/Bean/Game/CreatureCardItemBean.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
  - Assets/Scripts/Common/EventsInfo.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Utils/GameUIUtil.cs
  - Assets/Scripts/Game/Logic/GameFightLogic.cs
  - Assets/Scripts/Component/UI/Game/LineupManager/UILineupManager.cs
  - Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs
  - Assets/Scripts/Component/UI/Game/CreatureSacrifice/UICreatureSacrifice.cs
  - Assets/Scripts/Component/UI/Game/FightMain/UIFightMain.cs
---

# 生物卡片系统开发指南

## 核心概念

### 生物卡片数据结构

```
CreatureCardItemBean     - 生物卡片运行时数据（包含生物数据、用途、状态、位置等）
CreatureBean             - 生物数据（包含属性、等级、稀有度、装备等）
```

### 卡片用途体系 (CardUseStateEnum)

卡片用途决定卡片在哪个场景使用以及交互行为：

| 枚举值 | 说明 |
|--------|------|
| `Show` | 展示（带详情气泡） |
| `ShowNoPopup` | 展示但不弹详情 |
| `Fight` | 战斗场景 |
| `Lineup` | 阵容场景 |
| `LineupBackpack` | 阵容背包 |
| `CreatureManager` | 魔物管理 |
| `CreatureSacrifice` | 魔物献祭 |
| `CreatureAscendTarget` | 魔物进阶-目标 |
| `CreatureAscendMaterial` | 魔物进阶-材料 |
| `SelectCreature` | 生物选择 |

### 卡片状态体系 (CardStateEnum)

卡片状态控制UI显示效果（遮罩、选中框、CD等）：

```
战斗状态
├── FightIdle = 101      // 待机（可点击选择）
├── FightSelect = 102    // 选中（显示选中框）
├── Fighting = 103       // 上场战斗中（显示遮罩）
└── FightRest = 104      // 休息CD中（显示遮罩+倒计时）

阵容状态
├── LineupNoSelect = 201 // 未选中
└── LineupSelect = 202   // 选中（显示遮罩）

管理状态
├── CreatureManagerNoSelect = 301
└── CreatureManagerSelect = 302（显示选中框）

献祭状态
├── CreatureSacrificeNoSelect = 401
└── CreatureSacrificeSelect = 402（显示选中框）

进阶状态
├── CreatureAscendNoSelect = 501
└── CreatureAscendSelect = 502（显示选中框）

选择状态
├── SelectCreatureNoSelect = 601
└── SelectCreatureSelect（显示选中框）
```

### 卡片类型继承体系

```
UIViewCreatureCardItem (基类)
├── UIViewCreatureCardItemForFight          // 战斗卡片（支持拖拽、CD、选中动画）
├── UIViewCreatureCardItemForLineup         // 阵容卡片
├── UIViewCreatureCardItemForCreatureManager // 魔物管理卡片
├── UIViewCreatureCardItemForCreatureSacrifice // 献祭卡片
├── UIViewCreatureCardItemForCreatureAscend // 进阶卡片
└── UIViewCreatureCardItemForSelectCreature // 选择卡片

UIViewCreatureCardList       // 卡片列表（滚动网格）
UIViewCreatureCardDetails    // 卡片详情面板
UIPopupCreatureCardDetails   // 卡片详情弹窗
```

> **献祭卡片(`CreatureSacrifice*` 状态 / `UIViewCreatureCardItemForCreatureSacrifice`)的业务流程见 [`sacrifice-system`](../sacrifice-system/SKILL.md) Skill**：祭品选择、成功率公式、献祭升级、保底等机制都在那里；本 Skill 只负责献祭卡片的 UI 表现与状态。

> **献祭升级提示特效 `ui_SacrificeEffect`**（`UIViewCreatureCardItemComponent` 的 `Image` 字段，prefab `UIViewCreatureCardItem` 上挂，材质 `Mat_UIViewCreatureCardItem_Sacrifice.mat`）：`SetData` 内部调用 `SetSacrificeEffect(creatureData, cardUseState)` 控制显隐——**仅当 `cardUseState == CreatureManager` 且 已解锁祭坛(`UnlockEnum.Altar`) 且 `creatureData.CanUpLevel()`** 时显示，其它使用状态恒隐藏。判定条件与 `UICreatureManager.RefreshSacrificeButton`（献祭升级按钮显隐）一致，用于在魔物管理列表里高亮"可献祭升级"的生物。

## 创建/使用生物卡片

### 1. 基础卡片使用

```csharp
// 获取卡片组件并设置数据
UIViewCreatureCardItem cardItem = GetComponent<UIViewCreatureCardItem>();
cardItem.SetData(creatureData, CardUseStateEnum.Show);
// SetData 内部会自动填充卡片上的 MPText（SetCreateMP）：显示召唤该生物需要消耗的魔力 creatureData.GetAttributeInt(CreatureAttributeTypeEnum.CMP)
//（= 基础CMP×(1+等级/稀有度增加倍率) 再经自身/稀有度BUFF修正，如扭蛋 CMP 减益；卡片详情 UIViewCreatureCardDetails.SetMP 同此），
// 战斗中放置卡片时从魔王当前魔力(MPCurrent)中扣除，不足则 Toast"魔力不足"
```

### 2. 战斗卡片使用

```csharp
// 战斗卡片需要额外设置原始位置
UIViewCreatureCardItemForFight fightCard = GetComponent<UIViewCreatureCardItemForFight>();
Vector2 originalPos = fightCard.rectTransform.anchoredPosition;
fightCard.SetData(creatureData, CardUseStateEnum.Fight, originalPos);

// 设置卡片状态
fightCard.SetCardState(CardStateEnum.FightSelect);   // 选中
fightCard.SetCardState(CardStateEnum.Fighting);      // 上场
fightCard.SetCardState(CardStateEnum.FightRest);     // 休息
```

### 3. 卡片列表使用

```csharp
// 设置列表数据
UIViewCreatureCardList cardList = GetComponent<UIViewCreatureCardList>();
List<CreatureBean> listCreature = GameDataHandler.Instance.manager.GetUserData().listCreature;
cardList.SetData(listCreature, CardUseStateEnum.CreatureManager, OnCellChange);

// 列表单元格变化回调
public void OnCellChange(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
{
    // 自定义处理，如设置选中状态
    if (selectCreatureId == itemData.creatureUUId)
    {
        itemView.SetCardState(CardStateEnum.CreatureManagerSelect);
    }
}

// 刷新指定卡片
cardList.RefreshCardByIndex(0);
cardList.RefreshCardByCreatureUUId("uuid_xxx");
cardList.RefreshAllCard();
```

### 4. 卡片详情面板

```csharp
// 设置详情数据
UIViewCreatureCardDetails details = GetComponent<UIViewCreatureCardDetails>();
details.SetData(creatureData);

// 设置详情面板方向（左/右弹出）
details.SetDetailsDirection(Direction2DEnum.Left);
details.SetDetailsDirection(Direction2DEnum.Right);

// 是否展示装备（默认true）
details.isShowEquipItem = false;
details.RefreshCard();
```

### 5. 创建新的卡片子类

继承 `UIViewCreatureCardItem` 并重写 `RefreshCardState`：

```csharp
// Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForXXX.cs
public partial class UIViewCreatureCardItemForXXX : UIViewCreatureCardItem
{
    public override void RefreshCardState(CardStateEnum cardState)
    {
        base.RefreshCardState(cardState);
        switch (cardState)
        {
            case CardStateEnum.XXXNoSelect:
                // 未选中状态UI
                break;
            case CardStateEnum.XXXSelect:
                // 选中状态UI（通常显示选中框）
                ui_SelectBg.gameObject.SetActive(true);
                break;
        }
    }
}
```

## 卡片事件系统

### 卡片相关事件

```csharp
// 卡片避让（鼠标悬停时相邻卡片移动）
EventsInfo.UIViewCreatureCardItem_SelectKeep
    // 参数: int targetIndex, Vector2 targetPos, bool isKeep

// 鼠标进入卡片
EventsInfo.UIViewCreatureCardItem_OnPointerEnter
    // 参数: UIViewCreatureCardItem targetView

// 鼠标离开卡片
EventsInfo.UIViewCreatureCardItem_OnPointerExit
    // 参数: UIViewCreatureCardItem targetView

// 点击选择卡片
EventsInfo.UIViewCreatureCardItem_OnClickSelect
    // 参数: UIViewCreatureCardItem targetView
```

> **场上魔物描边高亮**：`UIViewCreatureCardItemForFight.OnPointerEnter` 末尾调 `ShowFieldCreatureOutline()` —— 若本卡对应魔物已上场(`fightData.GetCreatureById(uuid, FightDefense) != null`)，调 `CreatureHandler.ShowCreatureOutlinePreview(entity)` 给场上魔物套一圈亮蓝描边；`OnPointerExit` 调 `CreatureHandler.HideCreatureOutlinePreview()` 收起。描边实现见 creature-system / game-creature(共享单例预览预制 `FightCreature_OutlinePreview` + OutlineOnly 材质；平面 Spine 精灵固定法线导致 Rim 边缘光不可见，故改用描边)。

> **战斗卡片深渊馈赠展示**：`UIViewCreatureCardItemForFight` 上的 `ui_AbyssalBlessingContent`(GridLayout 容器) + `ui_AbyssalBlessingItem`(Image 模板，prefab 中默认隐藏) 用来展示「**实际作用在本魔物身上**」的深渊馈赠图标。`RefreshAbyssalBlessing()`(在 `SetData` 末尾及监听 `EventsInfo.Buff_AbyssalBlessingChange` 时调用) → `CollectAbyssalBlessingForCreature` 遍历 `BuffHandler.manager.dicAbyssalBlessingBuffsActivie`，对每个馈赠用 **`AbyssalBlessingUtil.DoesAbyssalBuffAffectCreature(buff, creatureData, FightDefense)`**(在 `Assets/Scripts/Utils/AbyssalBlessingUtil.cs`) 判定其任一 BUFF 是否真作用于本魔物——口径与属性管线(`FightCreatureBean.CollectFromBuffList`)/攻速管线一致：含**全体防守加成**(强身健体/伤害性极强/唯快不破/坚不可摧/时光沙漏，每张防守卡都显示一份)与**定向到本魔物**的馈赠(大力出奇迹/膘肥体壮/钢铁憨憨/急性子，按锁定 UUID 精确匹配)；排除作用敌方(慢条斯理)/防守核心/掉落(钱多多)/奖励(奖励多多·再来一瓶)/复制(增殖)等不改本魔物数值的馈赠。按收集个数用 `GetOrCreateAbyssalBlessingItem` 缓存池(`listAbyssalBlessingItem`)复用/克隆模板，`IconHandler.SetAbyssalBlessingIcon` 设图标，无馈赠时隐藏整个容器。卡片上魔物固定为 `FightDefense`。⚠️ **复制魔物(增殖)产生的克隆体是新 UUID，只会显示「全体防守馈赠」(靠 trigger_creature_type 自动生效)，不显示/不继承针对原魔物的单体定向馈赠**。深渊馈赠机制见 abyssal-blessing-system / game-abyssal-blessing。

### 战斗卡片事件

```csharp
// 选中卡片
EventsInfo.GameFightLogic_SelectCard
    // 参数: UIViewCreatureCardItem targetView

// 取消选中卡片
EventsInfo.GameFightLogic_UnSelectCard
    // 参数: UIViewCreatureCardItem targetView

// 放置卡片（生物上场）
EventsInfo.GameFightLogic_PutCard
    // 参数: UIViewCreatureCardItem targetView

// 生物状态变化
EventsInfo.GameFightLogic_CreatureChangeState
    // 参数: string creatureUUID, CreatureStateEnum creatureState
```

### 事件监听示例

```csharp
// 在UI中注册卡片点击事件
this.RegisterEvent<UIViewCreatureCardItem>(
    EventsInfo.UIViewCreatureCardItem_OnClickSelect, 
    EventForCardClickSelect
);

public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
{
    // 处理卡片点击逻辑
    CreatureBean creatureData = selectItemView.cardData.creatureData;
}
```

## 常用代码模板

### 设置生物图标

```csharp
// 简单图标（卡片内使用）
GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);

// 详情图标（带场景背景）
GameUIUtil.SetCreatureUIForDetails(ui_Icon, ui_CardScene, creatureData);

// 自定义大小
GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale: 2);
```

### 获取卡片数据

```csharp
// 从卡片获取生物数据
CreatureBean creatureData = cardItem.cardData.creatureData;

// 获取卡片当前状态
CardStateEnum state = cardItem.cardData.cardState;

// 获取卡片用途
CardUseStateEnum useState = cardItem.cardData.cardUseState;
```

### 战斗中处理卡片选择

```csharp
// 在 GameFightLogic 中处理卡片选择
public void SelectCard(UIViewCreatureCardItem targetView)
{
    selectCreatureCard = targetView;
    TriggerEvent(EventsInfo.GameFightLogic_SelectCard, targetView);
}

public void UnSelectCard()
{
    if (selectCreatureCard != null)
    {
        TriggerEvent(EventsInfo.GameFightLogic_UnSelectCard, selectCreatureCard);
        selectCreatureCard = null;
    }
}

public void PutCard(Vector3 worldPosition)
{
    if (selectCreatureCard == null)
        return;
    
    // 检测魔王魔力是否足够（GetAttributeInt(CMP)=基础CMP×(1+等级/稀有度增加倍率)经BUFF修正后的召唤消耗），不足则Toast"魔力不足"(UIText 50006)并中止
    // 足够则 ChangeMP(-GetAttributeInt(CMP)) 扣除魔力并刷新魔王MPShow显示
    // 创建生物实体...
    TriggerEvent(EventsInfo.GameFightLogic_PutCard, selectCreatureCard);
    selectCreatureCard = null;
}
```

### 卡片排序（筛选排序弹窗）

`UIViewCreatureCardList` 不再用一排固定排序按钮，改为单个 `OrderBtn`：点击弹出 `UIDialogOrderFilter`（在按钮处弹出，悬浮详情=「筛选排序」UIText 2000014）。弹窗可**多选**筛选类型，**按选择顺序决定排序优先级**（index0=主键），再点「正序/倒序」确认。

```csharp
// 点击 OrderBtn -> 打开筛选排序弹窗（开放 稀有度/等级/阵容/名字/同类，默认带入当前选择）
UIHandler.Instance.ShowDialogOrderFilter(
    ui_OrderBtn_Button.transform as RectTransform,
    OnConfirmOrderFilter,                                   // (List<OrderFilterTypeEnum> types, bool isAscending)
    listFilterType,                                         // 开放的筛选类型（null=全部）
    new List<OrderFilterTypeEnum>(currentFilterTypes));     // 默认选中（按优先级）

// 确认回调：按所选优先级 + 正/倒序排序
protected void OnConfirmOrderFilter(List<OrderFilterTypeEnum> filterTypes, bool isAscending)
{
    currentFilterTypes = filterTypes ?? new List<OrderFilterTypeEnum>();
    currentAscending = isAscending;
    OrderListCreature(currentFilterTypes, currentAscending);
}

// 动态多键排序：filterTypes 顺序即主/次键优先级，isAscending 作用于全部键
public void OrderListCreature(List<OrderFilterTypeEnum> filterTypes, bool isAscending, bool isRefreshUI = true)
```

> `OrderFilterTypeEnum`：Rarity=1 / Level=2 / Lineup=3 / Name=4 / Class=5（同类=相同生物ID归并）。
> 排序键由 `GetOrderKeySelector` 给出：Rarity→rarity、Level→level、Lineup→阵容序号(不在阵容置 int.MaxValue)、Name→creatureName、Class→creatureId。

### 空列表提示

`UIViewCreatureCardList` 含一个 `UIViewNullText`（挂 `UITextLanguageView`+`TextMeshProUGUI`）。`SetData` 末尾调用 `RefreshNullText()`：列表为空时显示「没有相关魔物」（UIText **2000016**），非空则隐藏。`textId` 在代码里设置（非 prefab 写死），故无需改 prefab。背包道具列表 `UIViewItemBackpackList` 同理，空时显示「没有相关道具」（UIText **2000015**）。

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 卡片基类 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItem.cs` |
| 卡片组件声明 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemComponent.cs` |
| 战斗卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForFight.cs`(主体：生命周期/快捷按键/状态/触摸事件/深渊馈赠展示) + `UIViewCreatureCardItemForFightAnim.cs`(partial：动画参数/Tween 句柄/创建·选择·避让动画 `AnimForCreateShow`/`PlaySelectEnterAnim`/`PlaySelectExitAnim`/`PlaySelectKeepAnim`/`PlaySelectKeepReturnAnim`/`ClearAnim`/`KillAnim*`) |
| 阵容卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForLineup.cs` |
| 管理卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForCreatureManager.cs` |
| 献祭卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForCreatureSacrifice.cs` |
| 进阶卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForCreatureAscend.cs` |
| 选择卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForSelectCreature.cs` |
| 卡片列表 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardList.cs` |
| 卡片详情 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardDetails.cs` |
| 详情弹窗 | `Assets/Scripts/Component/UI/Popup/UIPopupCreatureCardDetails.cs` |
| 卡片数据Bean | `Assets/Scripts/Bean/Game/CreatureCardItemBean.cs` |
| 卡片用途/状态枚举 | `Assets/Scripts/Enums/GameStateEnum.cs` |
| 卡片相关事件 | `Assets/Scripts/Common/EventsInfo.cs` |
| 生物数据Bean | `Assets/Scripts/Bean/Game/CreatureBean.cs` |
| 生物工具 | `Assets/Scripts/Utils/CreatureUtil.cs` |
| UI工具 | `Assets/Scripts/Utils/GameUIUtil.cs` |
| 战斗逻辑 | `Assets/Scripts/Game/Logic/GameFightLogic.cs` |
| 阵容管理UI | `Assets/Scripts/Component/UI/Game/LineupManager/UILineupManager.cs` |
| 魔物管理UI | `Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs` |
| 魔物献祭UI | `Assets/Scripts/Component/UI/Game/CreatureSacrifice/UICreatureSacrifice.cs` |
| 战斗主界面UI | `Assets/Scripts/Component/UI/Game/FightMain/UIFightMain.cs` |
