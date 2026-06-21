---
name: game-fight-system
description: Demon Lord Roguelike 游戏的战斗系统(GameFight)开发指南。使用此SKILL当需要创建或修改战斗逻辑、战斗流程、战斗数据管理、战斗生物实体、战斗场景控制、战斗UI、战斗结算等，包括征服模式/无限模式/终焉议会/测试模式等战斗类型。注意：攻击弹道逻辑请使用 attack-mode-system skill，BUFF逻辑请使用 buff-system skill，AI逻辑请使用 ai-system skill。
watched_files:
  - Assets/Scripts/Game/Logic/
  - Assets/Scripts/Bean/Game/FightBean.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntity.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntityForAttack.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntityForDefense.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntityForDefenseCore.cs
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
FightCreatureEntity     - 战斗生物实体（Spine动画、受击/回复/死亡、血条、魔王魔力条MPShow）
                          按类型拆分partial：主文件通用 + ForAttack(进攻) + ForDefense(防守) + ForDefenseCore(魔王)
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
    
    // 2. 游戏更新：生物生成、生物更新、魔王魔力恢复、BUFF更新
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
// partial 拆分：通用接口在主文件；RefreshMPShow 在 FightCreatureEntityForDefenseCore.cs；ChangeRoad 在 FightCreatureEntityForAttack.cs
public partial class FightCreatureEntity
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
    
    // 刷新魔力显示（魔王核心专用：MPShow进度条 + MPText文本"当前/上限"，非核心生物无MPShow节点自动跳过）
    public void RefreshMPShow();
}
```

## 魔王魔力(MP)系统

MP/MPF 两个属性**仅在战斗中有效**，挂在魔王（防守核心）身上：

```csharp
// 属性来源（excel_creature_info）：
// MP  = 魔力上限（魔王用来创建魔物的资源池）
// MPF = 魔力恢复速度（每秒恢复MPF点魔力）
// CMP = 创建该魔物需要消耗的魔力基础值（配在每个魔物的 CreatureInfo 上，原字段名 create_mp 已改名为 CMP）
//   实际召唤耗魔取 creature.GetAttributeInt(CreatureAttributeTypeEnum.CMP)（= 基础CMP×(1+等级/稀有度增加倍率)再经自身/稀有度BUFF修正，如扭蛋稀有度 CMP 减益），勿直接读 CMP 字段
//   等级/稀有度增加倍率求和见 CreatureBean.GetCreateMPAddRate()（LevelInfo.CMP_rate + RarityInfo.CMP_rate）

// 运行时数据（FightCreatureBean）：
public float MPCurrent;   // 当前魔力（战斗开始时默认满值，float用于累积每帧恢复量）
public void ChangeMP(float changeMP, out float leftMP, out float changeMPReal); // 限制在[0, MP上限]

// 注意：CreatureBean.GetAttribute 的 switch 已含 MP/CMP 分支（CreatureBean.cs 是 Bean/Game 下手写可改文件），
// 取魔力上限直接走 creature.GetAttribute(CreatureAttributeTypeEnum.MP)；
// FightCreatureBean.RefreshBaseAttribute 遍历全枚举走 GetAttribute 统一缓存进 dicAttribute。

// 研究加成（仅魔王/防守核心）：FightCreatureBean.RefreshBaseAttribute 末尾，当
//   creatureFightType == FightDefenseCore 时给 dicAttribute[MP]/[MPF] 叠加研究值：
//   MP  += UserUnlockBean.GetUnlockDemonLordMPMaxAddValue()  (强化研究 UnlockEnum.DemonLordMPMax=200300001，每级+10，满级5级+50)
//   MPF += UserUnlockBean.GetUnlockDemonLordMPFAddValue()    (强化研究 UnlockEnum.DemonLordMPF=200400001，每级+1/秒，满级3级+3/s)
//   普通生物不应用，避免影响非核心生物魔力数值。

// 恢复链路：GameFightLogic.UpdateGameForMPRecover(updateTime)
//   每帧给魔王核心恢复 MPF*updateTime 点魔力，然后 RefreshMPShow() 通知刷新显示
// 消耗链路：GameFightLogic.PutCard()
//   放置卡片时检查 MPCurrent >= GetAttributeInt(CMP)，不足则 Toast 提示"魔力不足"(UIText 50006)并取消放置；
//   足够则 ChangeMP(-GetAttributeInt(CMP)) 扣除并 RefreshMPShow()
// 显示链路：魔王预制(FightCreature_DefCore_1)下 MPShow(SpriteRenderer+MatSpriteCreatureLife进度材质)
//   + MPShow/MPText(TextMeshPro 显示"100/100"格式)，与防守生物LifeShow同款机制
// 渲染层级：MPText 用 Overlay 着色器材质(MatTMP_MPTextOverlay，TMP_SDF Overlay：ZTest Always + Overlay队列)，
//   不做深度测试，保证魔力文本始终画在不透明3D地面之上。
//   注意：标准 TMP_SDF 的 ZTest=LEqual 会被地面写入的深度缓冲遮挡，单靠 MeshRenderer.sortingOrder 压不过不透明地面（深度测试与排序无关）；
//   SetDataForDefenseCore 里仍设 sortingOrder=9999 仅作同队列内排序补充，真正不被遮挡靠的是 Overlay 材质。
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
//    -> 检测位置是否已有生物 -> 检测魔王魔力是否足够(GetAttributeInt(CMP) 不足则Toast"魔力不足")
//    -> 扣除魔力并刷新MPShow -> 创建实体 -> 触发事件
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
GameFightLogic.Instance.PickupCrystalForMouse();   // 手动点击拾取时播放音效 sound_btn_15(id 15)
GameFightLogic.Instance.PickupCrystalForCreature(entity, pickupRadius);
// 两种拾取最终都走 PickupCrystal，魔晶飞回收集点(核心)的 DOJump.OnComplete 中：
//   播放入账音效 sound_pay_2(id 480002) → AddCrystal → 触发 GameFightLogic_DropAddCrystal
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
| 战斗生物实体(通用) | `Assets/Scripts/Game/Fight/FightCreatureEntity.cs` |
| 战斗生物实体(进攻:换路诱导/死亡意图) | `Assets/Scripts/Game/Fight/FightCreatureEntityForAttack.cs` |
| 战斗生物实体(防守:死亡意图) | `Assets/Scripts/Game/Fight/FightCreatureEntityForDefense.cs` |
| 战斗生物实体(魔王:魔力MPShow/死亡意图) | `Assets/Scripts/Game/Fight/FightCreatureEntityForDefenseCore.cs` |
| 战斗预制实体 | `Assets/Scripts/Game/Fight/FightPrefabEntity.cs` |
| 战斗处理器 | `Assets/Scripts/Component/Handler/FightHandler.cs` |
| 战斗管理器 | `Assets/Scripts/Component/Manager/FightManager.cs` |
| 战斗控制 | `Assets/Scripts/Component/Game/Control/ControlForGameFight.cs` |
| 战斗主UI | `Assets/Scripts/Component/UI/Game/FightMain/UIFightMain.cs` |
| 战斗结算UI | `Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs` |
| 生物处理器 | `Assets/Scripts/Component/Handler/CreatureHandler.cs` |
| 生物管理器 | `Assets/Scripts/Component/Manager/CreatureManager.cs` |
