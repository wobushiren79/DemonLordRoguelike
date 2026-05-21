---
name: game-fight-system
description: Demon Lord Roguelike 游戏的战斗系统(GameFight)开发指南。使用此SKILL当需要创建或修改战斗逻辑、战斗流程、战斗数据管理、战斗生物实体、战斗场景控制、战斗UI、战斗结算等，包括征服模式/无限模式/终焉议会/测试模式等战斗类型。注意：攻击弹道逻辑请使用 attack-mode-system skill，BUFF逻辑请使用 buff-system skill，AI逻辑请使用 ai-system skill。
watched_files:
  - Assets/Scripts/Game/Logic/
  - Assets/Scripts/Bean/Game/FightBean.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntity.cs
  - Assets/Scripts/Game/Fight/FightPrefabEntity.cs
  - Assets/Scripts/Component/Handler/FightHandler.cs
  - Assets/Scripts/Component/Manager/FightManager.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameFight.cs
  - Assets/Scripts/Component/UI/Game/FightMain/UIFightMain.cs
  - Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Manager/CreatureManager.cs
---

# 战斗系统开发指南

## 核心架构

```
GameFightLogic          - 战斗逻辑基类（生命周期管理、场景加载、生物生成、游戏状态）
FightBean               - 战斗运行时数据（进攻/防守生物、核心生物、战斗记录）
FightCreatureEntity     - 战斗生物实体（Spine动画、受击/回复/死亡、血条）
FightPrefabEntity       - 战斗预制实体（掉落水晶/魔力等场景物品）
FightHandler            - 战斗处理器（攻击模块创建、预制管理、倒计时）
FightManager            - 战斗管理器（对象池：攻击模块、战斗预制、数据缓存）
ControlForGameFight     - 战斗场景输入控制
UIFightMain             - 战斗主界面
```

## 战斗类型体系

```
GameFightTypeEnum
├── Test        - 测试模式（GameFightLogicTest）
├── Infinite    - 无限模式（GameFightLogicInfinite）
├── Conquer     - 征服模式（GameFightLogicConquer）
└── DoomCouncil - 终焉议会（GameFightLogicDoomCouncil）
```

## 战斗逻辑生命周期

```csharp
public class GameFightLogic : BaseGameLogic
{
    // 1. 准备阶段：加载场景 -> 创建核心生物 -> 开启控制 -> 打开UI
    public override async void PreGame() { }
    
    // 2. 游戏更新：生物生成、生物更新、BUFF更新
    public override void UpdateGame() { }
    
    // 3. 游戏结束触发
    public override void EndGame() { }
    
    // 4. 清理：销毁生物、清理场景、回收资源
    public override async Task ClearGame() { }
}
```

### 扩展战斗逻辑（以征服模式为例）

```csharp
public class GameFightLogicConquer : GameFightLogic
{
    // 加载场景后恢复上一关的防守生物
    public override async Task PreGameForAfterLoadFightScene() { }
    
    // 状态切换处理（重点：Settlement结算）
    public override void ChangeGameState(GameStateEnum gameState)
    {
        switch (gameState)
        {
            case GameStateEnum.Settlement:
                // 清理 -> 打开结算UI -> 下一关/深渊馈赠/返回基地
                break;
        }
    }
    
    // 开始下一关
    public void StartNextGame() { }
}
```

## 战斗数据结构

```csharp
public class FightBean
{
    public GameFightTypeEnum gameFightType;     // 战斗类型
    public float gameTime;                       // 游戏时间
    public float gameSpeed = 1;                  // 游戏速度
    
    // 场景
    public int sceneRoadNum = 6;                 // 路线数量
    public int sceneRoadLength = 10;             // 路线长度
    
    // 进攻数据
    public FightAttackBean fightAttackData;
    
    // 防守数据
    public DictionaryList<string, CreatureBean> dlDefenseCreatureData;
    public DictionaryList<string, FightCreatureEntity> dlDefenseCreatureEntity;
    
    // 进攻实例
    public DictionaryList<string, FightCreatureEntity> dlAttackCreatureEntity;
    
    // 核心生物
    public FightCreatureBean fightDefenseCoreData;
    public FightCreatureEntity fightDefenseCoreCreature;
    
    // 战斗记录
    public FightRecordsBean fightRecordsData;
}
```

### 生物查询工具方法

```csharp
// 通过ID获取生物（自动判断进攻/防守/核心）
FightCreatureEntity creature = fightData.GetCreatureById(creatureUUID);

// 获取指定位置防守生物
FightCreatureEntity creature = fightData.GetDefenseCreatureByPos(pos);

// 获取某一路上的所有进攻/防守生物
List<FightCreatureEntity> list = fightData.GetAttackCreatureByRoad(roadIndex);
List<FightCreatureEntity> list = fightData.GetDefenseCreatureByRoad(roadIndex);

// 检测是否存在进攻生物
bool hasEnemy = fightData.CheckHasAttackCreature();

// 检测指定位置是否有防守生物
bool hasCreature = fightData.CheckDefenseCreatureByPos(pos);
```

## 战斗生物实体

### 创建与销毁

```csharp
// 创建进攻生物（由GameFightLogic定时生成）
CreatureHandler.Instance.CreateAttackCreature(attackDetailData, roadNum);

// 创建防守生物（先创建预览obj）
GameObject previewObj = CreatureHandler.Instance.CreateDefenseCreature(creatureData);

// 放置后创建实体
CreatureHandler.Instance.CreateDefenseCreatureEntity(previewObj, creatureData, position);

// 移除生物
CreatureHandler.Instance.RemoveFightCreatureEntity(entity, CreatureFightTypeEnum.FightDefense);
```

### 生物交互接口

```csharp
public class FightCreatureEntity
{
    // 受到攻击（自动处理闪避、暴击、扣护甲、扣血、死亡检测）
    public void UnderAttack(BaseAttackMode baseAttackMode);
    
    // 回复HP
    public void RegainHP(BaseAttackMode baseAttackMode);
    
    // 回复护甲
    public void RegainDR(BaseAttackMode baseAttackMode);
    
    // 添加BUFF
    public void AddBuff(BaseAttackMode baseAttackMode);
    
    // 检测死亡
    public void CheckDead(Action noDead, Action dead);
    
    // 设置死亡
    public void SetCreatureDead();
    
    // 掉落水晶
    public void DropCrystal(int state); // 0所有 1仅进攻 2仅防守
    
    // 是否死亡
    public bool IsDead();
    
    // 设置朝向
    public void SetFaceDirection(Direction2DEnum direction);
    
    // 播放Spine动画
    public TrackEntry PlayAnim(SpineAnimationStateEnum anim, bool loop);
}
```

## 战斗流程控制

### 游戏状态

```csharp
public enum GameStateEnum
{
    Pre,        // 准备中
    Gaming,     // 游戏中
    Settlement, // 结算中
    End         // 游戏结束
}
```

### 结算与胜利判定

```csharp
public virtual void CheckGameEnd()
{
    // 胜利条件：没有下一波敌人 且 场上无敌人
    if (fightData.fightAttackData.queueAttackDetails.Count == 0 
        && !fightData.CheckHasAttackCreature())
    {
        fightData.gameIsWin = true;
        ChangeGameState(GameStateEnum.Settlement);
    }
    
    // 失败条件：魔王（核心生物）死亡
    if (fightData.fightDefenseCoreCreature.IsDead())
    {
        fightData.gameIsWin = false;
        ChangeGameState(GameStateEnum.Settlement);
    }
}
```

## 战斗场景控制

```csharp
public class ControlForGameFight : BaseControl
{
    // WASD/方向键：移动相机
    // 鼠标右键拖拽：移动相机
    // 鼠标左键：放置卡片 / 拾取水晶 / 确认删除
    // 鼠标右键：取消选择
}
```

### 卡片操作流程

```csharp
// 1. 点击卡片 -> GameFightLogic.SelectCard(cardView)
//    -> 创建预览生物跟随鼠标
// 2. 移动鼠标 -> 预览生物跟随，显示放置预览
// 3. 左键点击空地 -> GameFightLogic.PutCard()
//    -> 检测位置是否已有生物 -> 创建实体 -> 触发事件
// 4. 右键 -> GameFightLogic.UnSelectCard() -> 取消选择
```

### 删除生物流程

```csharp
// 1. 点击删除按钮 -> GameFightLogic.SelectCreatureDestroy()
//    -> 显示删除预览跟随鼠标
// 2. 左键点击生物 -> GameFightLogic.SelectCreatureDestoryHandle()
//    -> 移除该位置防守生物
// 3. 右键 -> GameFightLogic.UnSelectCreatureDestroy() -> 取消
```

## 战斗UI

### UIFightMain 核心职责

```csharp
public partial class UIFightMain : BaseUIComponent
{
    // 初始化：生成所有防守卡片、设置进攻进度条
    public void InitData();
    
    // 刷新：进攻进度、魔力值等
    public void RefreshUIData();
    
    // 事件监听
    // GameFightLogic_SelectCard      - 选中卡片
    // GameFightLogic_UnSelectCard    - 取消选择
    // GameFightLogic_PutCard         - 放置卡片
}
```

## 战斗预制管理

### 掉落水晶

```csharp
// 创建掉落
FightDropCrystalBean dropData = FightManager.Instance.GetFightDropCrystalBean(crystalNum, dropPos);
FightHandler.Instance.CreateDropCrystal(dropData);

// 拾取（鼠标点击或生物自动拾取）
GameFightLogic.Instance.PickupCrystalForMouse();
GameFightLogic.Instance.PickupCrystalForCreature(entity, pickupRadius);
```

### 自定义战斗预制

```csharp
// 获取通用战斗预制
FightManager.Instance.GetFightPrefabCommon(assetsPath, (prefab) =>
{
    // 使用 prefab...
});

// 移除预制
FightHandler.Instance.RemoveFightPrefab(prefabEntity);
```

## 战斗事件列表

```csharp
EventsInfo.GameFightLogic_CreatureDeadEnd       // 生物死亡结束
EventsInfo.GameFightLogic_EndGame               // 战斗结束
EventsInfo.GameFightLogic_SelectCard            // 选中卡片
EventsInfo.GameFightLogic_UnSelectCard          // 取消选择卡片
EventsInfo.GameFightLogic_PutCard               // 放置卡片
EventsInfo.GameFightLogic_UnderAttack           // 受到攻击
EventsInfo.GameFightLogic_UnderAttack_Dead      // 受击死亡
EventsInfo.GameFightLogic_DropAddCrystal        // 掉落增加水晶
EventsInfo.GameFightLogic_CreatureDeadDropCrystal // 生物死亡掉落水晶
EventsInfo.GameFightLogic_CreatureChangeState   // 生物状态改变
EventsInfo.Buff_FightCreatureChange             // 战斗生物BUFF改变
```

## 常用代码模板

### 创建新战斗模式

```csharp
// 1. 继承 GameFightLogic
public class GameFightLogicCustom : GameFightLogic
{
    // 2. 重写准备阶段
    public override async Task PreGameForAfterLoadFightScene()
    {
        await base.PreGameForAfterLoadFightScene();
        // 自定义初始化...
    }
    
    // 3. 重写状态切换
    public override void ChangeGameState(GameStateEnum gameState)
    {
        base.ChangeGameState(gameState);
        switch (gameState)
        {
            case GameStateEnum.Settlement:
                // 自定义结算逻辑...
                break;
        }
    }
    
    // 4. 重写胜利判定（如需）
    public override void CheckGameEnd()
    {
        // 自定义胜利/失败条件...
    }
}
```

### 遍历场上所有生物

```csharp
var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();

// 进攻生物
foreach (var creature in gameLogic.fightData.dlAttackCreatureEntity.List)
{
    if (creature == null || creature.IsDead()) continue;
    // 处理...
}

// 防守生物
foreach (var creature in gameLogic.fightData.dlDefenseCreatureEntity.List)
{
    if (creature == null || creature.IsDead()) continue;
    // 处理...
}

// 核心生物
var coreCreature = gameLogic.fightData.fightDefenseCoreCreature;
```

### 修改游戏速度

```csharp
var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
gameLogic.fightData.gameSpeed = 2.0f;  // 2倍速
gameLogic.fightData.gameSpeed = 0.5f;  // 0.5倍速
gameLogic.fightData.gameSpeed = 1.0f;  // 正常速度
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 战斗逻辑基类 | `Assets/Scripts/Game/Logic/GameFightLogic.cs` |
| 征服模式 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs` |
| 无限模式 | `Assets/Scripts/Game/Logic/GameFightLogicInfinite.cs` |
| 终焉议会 | `Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs` |
| 测试模式 | `Assets/Scripts/Game/Logic/GameFightLogicTest.cs` |
| 战斗数据Bean | `Assets/Scripts/Bean/Game/FightBean.cs` |
| 战斗生物实体 | `Assets/Scripts/Game/Fight/FightCreatureEntity.cs` |
| 战斗预制实体 | `Assets/Scripts/Game/Fight/FightPrefabEntity.cs` |
| 战斗处理器 | `Assets/Scripts/Component/Handler/FightHandler.cs` |
| 战斗管理器 | `Assets/Scripts/Component/Manager/FightManager.cs` |
| 战斗控制 | `Assets/Scripts/Component/Game/Control/ControlForGameFight.cs` |
| 战斗主UI | `Assets/Scripts/Component/UI/Game/FightMain/UIFightMain.cs` |
| 战斗结算UI | `Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs` |
| 生物处理器 | `Assets/Scripts/Component/Handler/CreatureHandler.cs` |
| 生物管理器 | `Assets/Scripts/Component/Manager/CreatureManager.cs` |
