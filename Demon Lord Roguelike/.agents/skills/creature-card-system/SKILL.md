---
name: creature-card-system
description: Demon Lord Roguelike 游戏的生物卡片(CreatureCard)系统开发指南。使用此SKILL当需要创建或修改生物卡片UI、卡片交互、卡片状态、卡片列表展示、卡片详情弹窗等，包括战斗卡片/阵容卡片/管理卡片/献祭卡片/进阶卡片等类型。
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

## 创建/使用生物卡片

### 1. 基础卡片使用

```csharp
// 获取卡片组件并设置数据
UIViewCreatureCardItem cardItem = GetComponent<UIViewCreatureCardItem>();
cardItem.SetData(creatureData, CardUseStateEnum.Show);
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
    
    // 创建生物实体...
    TriggerEvent(EventsInfo.GameFightLogic_PutCard, selectCreatureCard);
    selectCreatureCard = null;
}
```

### 卡片排序

```csharp
// UIViewCreatureCardList 内置排序方法
// 按稀有度排序
cardList.OrderListCreature(1);
// 按等级排序
cardList.OrderListCreature(2);
// 按阵容排序
cardList.OrderListCreature(3);
// 按名字排序
cardList.OrderListCreature(4);
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 卡片基类 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItem.cs` |
| 卡片组件声明 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemComponent.cs` |
| 战斗卡片 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForFight.cs` |
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
