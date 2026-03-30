# 魔王 Roguelike 项目架构分析

> 最后更新：2026 年 3 月 30 日
> 模块与 API 详见 [ProjectDocs.md](ProjectDocs.md)

---

## 目录

1. [架构概览](#一架构概览)
2. [核心继承体系](#二核心继承体系)
3. [设计模式](#三设计模式)
4. [启动流程与游戏循环](#四启动流程与游戏循环)
5. [核心依赖关系](#五核心依赖关系)
6. [架构总结](#六架构总结)

---

## 一、架构概览

项目分为两层，框架层提供通用能力，游戏逻辑层实现具体业务：

```
┌─────────────────────────────────────────────────────┐
│                   游戏逻辑层 (Scripts)                │
│  AI | Bean | Component | Game | MVC | Enums | Utils  │
├─────────────────────────────────────────────────────┤
│                   框架层 (FrameWork)                  │
│  Base | AI | Bean | Component | BaseSystem | MVC     │
│  Extension | Utils | Tools | DataStorage | Web       │
│  CallBack | Editor                                   │
└─────────────────────────────────────────────────────┘
```

**核心设计原则：**

1. **分层清晰** - 框架层与游戏逻辑层职责分离，框架层可复用
2. **组件化设计** - Handler-Manager 配对模式，逻辑与资源管理分离
3. **事件驱动** - 全局事件系统解耦各模块
4. **数据驱动** - Bean 层统一管理数据结构
5. **泛型扩展** - 大量使用泛型和继承提高扩展性

---

## 二、核心继承体系

### 2.1 MonoBehaviour 继承链

```
UnityEngine.MonoBehaviour
    │
    ▼
BaseMonoBehaviour                      # 通用方法：Instantiate, Find, AutoLinkUI
    │
    ├── BaseManager                    # 资源加载、数据管理
    │   ├── UIManager
    │   ├── GameManager
    │   ├── FightManager
    │   ├── BuffManager
    │   ├── CreatureManager
    │   └── ...
    │
    ├── BaseComponent                  # 组件基类
    │
    ├── BaseUIComponent                # UI 组件基类
    │   ├── CartogramBarView
    │   ├── ScrollGridCell
    │   └── ...
    │
    └── BaseUIInit                     # UI 初始化，自动链接控件
        └── BaseUIView                 # UI 视图基类
```

### 2.2 单例体系

```
BaseSingleton<T> where T : new()       # 非 Mono 单例（双重检查锁）
    │
    └── EventHandler                   # 全局事件管理器

BaseSingletonMonoBehaviour<T>          # Mono 单例
    │
    └── BaseHandler<T, M>             # Handler 基类（自动持有 Manager）
        ├── GameHandler    --> GameManager
        ├── FightHandler   --> FightManager
        ├── BuffHandler    --> BuffManager
        ├── UIHandler      --> UIManager
        ├── CreatureHandler--> CreatureManager
        ├── AudioHandler   --> AudioManager
        ├── WorldHandler   --> WorldManager
        └── ...
```

### 2.3 MVC 体系

```
BaseMVC                                # MVC 基类（mContent 上下文）
    │
    ├── BaseMVCController<M, V>        # 控制器泛型基类
    │   ├── GameConfigController       # 游戏配置控制器
    │   └── UserDataController         # 用户数据控制器
    │
    └── BaseMVCModel                   # 模型基类
        ├── GameConfigModel
        └── UserDataModel

IBaseMVCView                           # 视图接口
    ├── IGameConfigView
    └── IUserDataView

服务层：
    BaseMVCService                     # 服务层基类
    GameConfigService                  # 游戏配置服务
    UserDataService                    # 用户数据服务
```

### 2.4 AI 状态机体系

```
AIBaseEntity                           # AI 实体基类
    │                                  #   - dicIntentPool 意图池
    │                                  #   - currentIntent 当前意图
    │                                  #   - ChangeIntent() 意图切换
    │
    └── AICreatureEntity               # 生物 AI 实体
        ├── AIAttackCreatureEntity     # 进攻生物
        ├── AIDefenseCreatureEntity    # 防守生物
        └── AIDefenseCoreCreatureEntity# 核心生物

AIBaseIntent                           # AI 意图基类
    │                                  #   - IntentEntering / Leaving
    │                                  #   - IntentUpdate / FixUpdate
    │
    ├── AIIntentCreatureAttack         # 通用攻击意图
    ├── AIIntentCreatureDead           # 通用死亡意图
    │
    ├── 进攻生物意图
    │   ├── AIIntentAttackCreatureIdle
    │   ├── AIIntentAttackCreatureMove
    │   ├── AIIntentAttackCreatureAttack
    │   ├── AIIntentAttackCreatureLured# 被引诱状态
    │   └── AIIntentAttackCreatureDead
    │
    ├── 防守生物意图
    │   ├── AIIntentDefenseCreatureAttack
    │   ├── AIIntentDefenseCreatureDefend
    │   ├── AIIntentDefenseCreatureIdle
    │   └── AIIntentDefenseCreatureDead
    │
    └── 核心生物意图
        ├── AIIntentDefenseCoreCreatureIdle
        └── AIIntentDefenseCoreCreatureDead
```

**状态流转示例（进攻生物）：**

```
Idle --> Move --> Attack --> Dead
 ▲       │        │
 └───────┘        │ (目标死亡/超出范围)
 └────────────────┘
```

### 2.5 事件系统体系

```
BaseEvent                              # 事件基类（实例级别）
    │                                  #   - RegisterEvent / UnRegisterEvent
    │                                  #   - TriggerEvent
    │
    ├── BaseUIInit                     # UI 可注册事件
    ├── BaseUIComponent                # UI 组件可注册事件
    ├── BaseGameLogic                  # 游戏逻辑可注册事件
    └── AIBaseEntity                   # AI 可注册事件

EventHandler (单例)                     # 全局事件管理器
    │                                  #   - 跨模块通信
    │                                  #   - 支持 0-4 泛型参数
    └── Dictionary<string, Delegate>   # 事件字典
```

### 2.6 游戏逻辑体系

```
BaseGameLogic : BaseEvent              # 游戏逻辑基类
    │                                  #   - PreGame / StartGame
    │                                  #   - UpdateGame / EndGame / ClearGame
    │
    ├── GameFightLogic                 # 战斗逻辑基类
    │   ├── GameFightLogicConquer      # 征服模式
    │   ├── GameFightLogicDoomCouncil  # 终焉议会模式
    │   ├── GameFightLogicInfinite     # 无限模式
    │   └── GameFightLogicTest         # 测试模式
    │
    ├── DoomCouncilLogic               # 终焉议会逻辑
    ├── CreatureSacrificeLogic         # 生物献祭逻辑
    └── GashaponMachineLogic           # 扭蛋机逻辑
```

### 2.7 攻击模式体系（策略模式）

```
BaseAttackMode                         # 攻击模式基类
    │
    ├── 近战
    │   ├── AttackModeMelee            # 普通近战
    │   └── AttackModeMeleeArea        # 范围近战
    │
    ├── 远程
    │   ├── AttackModeRanged           # 普通远程
    │   ├── AttackModeRangedArc        # 弧形远程
    │   ├── AttackModeRangedArcArea    # 弧形范围远程
    │   ├── AttackModeRangedArea       # 范围远程
    │   ├── AttackModeRangedPiercing   # 穿透远程
    │   ├── AttackModeRangedSplit      # 分裂远程
    │   └── AttackModeRangedTracking   # 追踪远程
    │
    ├── 特殊
    │   ├── AttackModeExplosion        # 爆炸
    │   ├── AttackModeFallupon         # 降临
    │   ├── AttackModeFalluponArea     # 范围降临
    │   ├── AttackModeLure             # 引诱
    │   └── AttackModeOverlap          # 重叠
    │
    └── 恢复
        ├── AttackModeRegain           # 恢复基类
        ├── AttackModeRegainHP         # HP 恢复
        └── AttackModeRegainDR         # 防御恢复
```

### 2.8 BUFF 前置条件体系

```
BuffBasePreEntity                      # BUFF 前置条件基类
    │
    ├── BuffPreEntityForAttackDamage   # 攻击伤害条件
    ├── BuffPreEntityForHPRateLess     # HP 比例低于条件
    ├── BuffPreEntityForKillNum        # 击杀数量条件
    └── BuffPreEntityForUnderAttackDamage # 受击伤害条件
```

### 2.9 UI 组件体系

```
BaseUIComponent                        # UI 组件基类
    │
    ├── CartogramBarView               # 柱状图
    ├── CartogramBaseView              # 图表基类
    ├── ScrollGridCell                 # 滚动网格单元
    ├── SelectView                     # 选择器
    ├── SelectColorView                # 颜色选择器
    └── ...

BaseUIInit                             # UI 初始化基类
    │
    ├── BaseUIView                     # UI 视图基类
    │   ├── 各类游戏 UI (UIFightMain, UIBaseMain 等)
    │   └── 各类弹窗 UI (UIPopupItemInfo 等)
    │
    └── 各类 UIView 子组件
        ├── UIViewFightMainAttCreateProgress
        ├── UIViewBasePortalItem
        ├── UIViewCreatureCardItem
        └── ...
```

### 2.10 编辑器工具体系

```
EditorWindow (Unity)
    │
    ├── ExcelEditorWindow              # Excel 配置导出
    ├── MVCEditorWindow                # MVC 代码生成
    ├── UIEditorWindow                 # UI 代码生成
    ├── BaseUICreateWindow             # UI 创建向导
    ├── AddressableWindow              # Addressable 管理
    ├── SpineWindow                    # Spine 工具
    ├── NodeBaseEditorWindow           # 节点编辑器
    └── ...
```

---

## 三、设计模式

| 模式 | 应用场景 | 核心实现 |
|------|----------|----------|
| **单例模式** | 全局管理器 | `BaseSingleton<T>`、`BaseSingletonMonoBehaviour<T>`、所有 Handler |
| **MVC 模式** | 数据与 UI 分离 | `BaseMVCController<M,V>` + `BaseMVCModel` + `IBaseMVCView` + `BaseMVCService` |
| **状态机模式** | AI 行为控制 | `AIBaseEntity` + `AIBaseIntent` 意图切换 |
| **观察者模式** | 模块间通信 | `BaseEvent` 实例事件 + `EventHandler` 全局事件 + `BaseObservable` |
| **策略模式** | 攻击方式选择 | `BaseAttackMode` 及 16 种子类 |
| **模板方法模式** | 游戏流程标准化 | `BaseGameLogic` 定义 Pre/Start/Update/End/Clear 流程 |
| **工厂模式** | 运行时对象创建 | `ReflexUtil.CreateInstance<T>()` 反射创建 |
| **组合模式** | BUFF 叠加 | 多种 BuffEntity 组合生效 |
| **配对模式** | 逻辑与资源分离 | `BaseHandler<T,M>` 自动关联 Manager |

### Handler-Manager 配对模式详解

```
┌───────────────────────┐         ┌───────────────────────┐
│  Handler (单例)        │         │  Manager (MonoBehaviour)│
│  - 纯逻辑处理          │ ──持有──►│  - 资源加载与缓存       │
│  - 外部调用入口         │         │  - 状态数据维护         │
│  - 自动创建 Manager    │         │  - 生命周期管理         │
└───────────────────────┘         └───────────────────────┘

调用方式：XXXHandler.Instance.DoSomething()
内部访问：handler.manager.GetResource()
```

**优势：**
- Handler 作为单例提供全局访问点
- Manager 作为 MonoBehaviour 管理 Unity 资源和生命周期
- 职责分离：逻辑 vs 资源，易于测试和维护

---

## 四、启动流程与游戏循环

### 4.1 启动流程

```
LauncherGame.Launch()
    │
    ├── 1. 初始化框架层 Handler（自动创建 Manager）
    │   ├── GameDataHandler    --> 加载游戏配置
    │   ├── AudioHandler       --> 初始化音频
    │   ├── UIHandler          --> 初始化 UI 系统
    │   ├── TextHandler        --> 初始化多语言系统
    │   └── ...
    │
    ├── 2. 初始化 MVC
    │   ├── GameConfigController --> 加载游戏配置数据
    │   └── UserDataController   --> 加载用户存档
    │
    └── 3. 进入主场景
        └── WorldHandler.EnterMainForBaseScene()
```

### 4.2 游戏主循环

```
GameHandler.Update()
    │
    ▼
manager.gameLogic.UpdateGame()        # 当前游戏逻辑更新
    │
    ├── FightHandler.UpdateData()     # 战斗系统更新
    │   ├── 遍历战斗生物实体
    │   ├── 处理攻击判定
    │   └── 处理伤害计算
    │
    ├── BuffHandler.UpdateData()      # BUFF 系统更新
    │   ├── 检查 BUFF 持续时间
    │   ├── 触发周期性 BUFF
    │   └── 移除过期 BUFF
    │
    ├── CreatureHandler.Update()      # 生物系统更新
    │   └── 更新生物状态
    │
    └── AI 更新（通过 AIBaseEntity）
        ├── 各 AI 实体执行 IntentUpdate()
        └── 根据条件执行 ChangeIntent()
```

### 4.3 游戏状态流转

```
PreGame (准备数据)
    │
    ▼
StartGame (开始游戏)
    │
    ▼
UpdateGame (游戏循环) ◄──┐
    │                    │
    │ (每帧执行)          │
    └────────────────────┘
    │
    ▼ (胜利/失败条件触发)
EndGame (结束游戏)
    │
    ▼
ClearGame (清理数据)
```

---

## 五、核心依赖关系

### 5.1 模块依赖图

```
┌──────────────────────────────────────────────────┐
│                     UI 层                          │
│  UIHandler ← Dialog/Popup/Toast/Game UI           │
├──────────────────────────────────────────────────┤
│                   游戏逻辑层                        │
│  GameHandler ← GameFightLogic/DoomCouncilLogic    │
│  FightHandler ← FightCreatureEntity/AttackMode    │
│  BuffHandler ← BuffEntity/BuffPreEntity           │
│  CreatureHandler ← AICreatureEntity               │
│  ItemsUtil ← ItemBean/ItemEquip                   │
├──────────────────────────────────────────────────┤
│                   数据层                            │
│  GameDataHandler ← Bean (Game/MVC/UI)             │
│  UserDataController ← UserDataModel/Service       │
├──────────────────────────────────────────────────┤
│                   基础设施层                        │
│  EventHandler (全局事件总线)                        │
│  AudioHandler / EffectHandler / CameraHandler     │
│  SpineHandler / IconHandler / WorldHandler        │
│  TextHandler (多语言) / SteamHandler              │
└──────────────────────────────────────────────────┘
```

### 5.2 事件驱动通信

```
EventHandler.Instance.TriggerEvent(EventsInfo.XXX)
    │
    ▼
所有注册该事件的监听器收到通知
    │
    ├── UIHandler         --> 刷新 UI 显示
    ├── GameHandler       --> 处理游戏逻辑
    ├── AudioHandler      --> 播放音效
    ├── EffectHandler     --> 播放特效
    ├── TextHandler       --> 更新多语言文本
    └── 其他 Handler...    --> 各自处理

常用事件（定义在 EventsInfo 中）：
    - 战斗相关：生物攻击、受伤、死亡
    - UI 相关：界面刷新、弹窗显示
    - 游戏状态：开始、暂停、结束
    - 数据变化：道具变更、经验获取
    - 多语言：语言切换
```

### 5.3 战斗系统数据流

```
AI 状态机决策
    │
    ▼
攻击模式选择 (BaseAttackMode)
    │
    ▼
创建攻击数据 (FightAttackBean)
    │
    ▼
伤害计算
    │
    ├── 检查 BUFF 前置条件 (BuffPreEntity)
    ├── 应用属性类 BUFF (HP/DR 变化)
    ├── 触发条件类 BUFF (攻击/受伤/死亡)
    │
    ▼
结果应用
    │
    ├── 更新生物属性 (CreatureAttributeBean)
    ├── 触发事件通知 (EventHandler)
    └── UI 反馈 (伤害数字、血条等)
```

### 5.4 Localization 数据流

```
TextManager 加载 Language_UIText_*.txt
    │
    ▼
LanguageBean / UITextBean 解析
    │
    ▼
UITextLanguageView 绑定 key
    │
    ▼
语言切换时自动更新所有 UITextLanguageView
    │
    ▼
触发 EventsInfo 语言变更事件
    │
    ▼
各 UI 模块刷新显示文本
```

---

## 六、架构总结

### 优势

| 特点 | 说明 |
|------|------|
| 完整的框架体系 | 从基础类、MVC、事件、AI、多语言到 UI 的完整框架 |
| 清晰的代码分层 | 框架层与游戏逻辑层职责分明，框架可复用 |
| 高度组件化 | Handler-Manager 配对模式，职责明确 |
| 事件驱动架构 | 全局事件系统实现模块间解耦 |
| 丰富的设计模式 | 9 种设计模式贯穿项目，代码规范统一 |
| 扩展性强 | 泛型 + 继承，新增功能无需修改框架 |
| 完善的工具链 | 编辑器扩展覆盖代码生成、资源管理、配置导出 |

### 扩展指南

- **新增攻击模式：** 继承 `BaseAttackMode`，实现攻击逻辑
- **新增 BUFF：** 继承 `BuffBaseEntity`，放入对应类型目录
- **新增 BUFF 前置条件：** 继承 `BuffBasePreEntity`
- **新增 AI 意图：** 继承 `AIBaseIntent`，在 Entity 中注册
- **新增游戏模式：** 继承 `GameFightLogic`，实现 Pre/Start/Update/End/Clear
- **新增 UI：** 继承 `BaseUIView`，通过 `UIHandler` 管理
- **新增 UI 组件：** 继承 `BaseUIComponent`
- **新增终焉议会效果：** 继承 `DoomCouncilBaseEntity`
- **新增多语言文本：** 在 Excel 中添加，通过 `ExcelEditorWindow` 导出
- **新增 MVC 模块：** 使用 `MVCEditorWindow` 生成代码模板

---

*架构分析结束 - 模块与 API 详见 [ProjectDocs.md](ProjectDocs.md)*
