# 魔王 Roguelike 项目架构分析文档

## 一、项目概述

这是一个基于 Unity 引擎开发的 Roguelike 塔防游戏项目，采用了自定义的 MVC 框架和组件化架构设计。项目代码分为**框架层（FrameWork）**和**游戏逻辑层（Scrpits）**两大部分。

---

## 二、主要目录结构和功能说明

```
Assets/
├── FrameWork/                          # 框架代码层 - 通用基础框架
│   └── Scrpits/
│       ├── AI/                         # AI 基础框架（状态机模式）
│       ├── Base/                       # 基础类库（MVC、单例、事件等）
│       ├── BaseSystem/                 # 基础系统（事件、SQLite、Steam）
│       ├── Bean/                       # 数据模型（通用 Bean 类）
│       ├── CallBack/                   # 回调接口定义
│       ├── Common/                     # 通用组件
│       ├── Component/                  # 框架组件（Manager、Handler、UI）
│       ├── DataStorage/                # 数据存储读写
│       ├── Enums/                      # 框架枚举定义
│       ├── Extension/                  # 扩展方法
│       ├── MVC/                        # 框架 MVC 实现
│       ├── Tools/                      # 工具类
│       ├── Utils/                      # 工具函数库
│       └── Web/                        # 网络请求
│
├── Scrpits/                            # 游戏逻辑代码层 - 具体游戏业务
│   ├── AI/                             # 游戏 AI 实现（生物 AI）
│   ├── Bean/                           # 游戏数据模型
│   ├── Common/                         # 游戏通用配置
│   ├── Component/                      # 游戏组件
│   ├── Enums/                          # 游戏枚举定义
│   ├── Game/                           # 核心游戏逻辑
│   │   ├── Base/                       # 游戏逻辑基类
│   │   ├── Buff/                       # BUFF 系统
│   │   ├── DoomCouncil/                # 终焉议会系统
│   │   ├── Fight/                      # 战斗系统
│   │   └── Launcher/                   # 启动器
│   ├── MVC/                            # 游戏 MVC 实现
│   ├── Struct/                         # 结构体定义
│   └── Utils/                          # 游戏工具函数
│
├── Data/                               # 游戏数据配置
├── Resources/                          # Unity 资源
├── Scenes/                             # 场景文件
├── Editor/                             # 编辑器扩展
└── LoadResources/                      # 加载资源
```

---

## 三、核心框架（FrameWork/Scrpits）的主要模块

### 3.1 Base - 基础类库

| 类名 | 功能说明 |
|------|----------|
| `BaseMonoBehaviour` | 所有 MonoBehaviour 的基类，提供实例化、查找等通用方法 |
| `BaseSingleton<T>` | 泛型单例模式基类（非 MonoBehaviour） |
| `BaseSingletonMonoBehaviour<T>` | MonoBehaviour 单例基类 |
| `BaseMVC` | MVC 模式基类，提供上下文对象管理 |
| `BaseMVCController<M,V>` | MVC 控制器基类 |
| `BaseMVCModel` | MVC 模型基类 |
| `BaseManager` | 管理器基类，继承自 BaseMonoBehaviour，提供资源加载、数据管理 |
| `BaseComponent` | 组件基类 |
| `BaseHandler<T,M>` | 处理器基类，采用 Handler-Manager 配对模式 |
| `BaseEvent` | 事件基类，提供事件注册/注销功能 |
| `BaseObservable` | 观察者模式基类 |
| `BaseUIManager` | UI 管理器基类 |
| `BaseUIView` | UI 视图基类 |
| `BaseUIInit` | UI 初始化基类，自动链接 UI 控件 |

### 3.2 AI - AI 基础框架

采用**状态机模式**实现 AI 系统：

```
AIBaseEntity (实体基类)
    ├── 管理意图池 (dicIntentPool)
    ├── 当前意图 (currentIntent)
    └── 意图切换逻辑

AIBaseIntent (意图基类)
    ├── IntentEntering (进入意图)
    ├── IntentUpdate (更新表现)
    ├── IntentFixUpdate (物理更新)
    └── IntentLeaving (离开意图)
```

**核心文件：**
- `AIBaseEntity.cs` - AI 实体基类
- `AIBaseIntent.cs` - AI 意图基类
- `AIBaseCommon.cs` - AI 通用类

### 3.3 Bean - 数据模型层

框架层 Bean 主要包含通用数据结构：

| 类别 | Bean 类 |
|------|--------|
| 基础 Bean | `BaseBean`, `BaseDataBean`, `BaseInfoBean` |
| 资源 Bean | `AudioBean`, `AnimBean`, `EffectBean`, `IconBean`, `ImageResBean` |
| UI Bean | `DialogBean`, `PopupBean`, `ToastBean`, `ProgressBean` |
| 数据 Bean | `DataBean`, `DataStorageListBean`, `DictionaryListBean` |
| 工具 Bean | `ColorBean`, `NumberBean`, `TimeBean`, `Vector3Bean`, `Vector3IntBean` |
| 游戏 Bean | `GameConfigBean`, `ScenesChangeBean`, `GameTimeCountDownBean` |
| 特殊 Bean | `SpineSkinBean`, `TileBean`, `MeshDataCustom` |

### 3.4 Component - 框架组件

采用 **Handler-Manager 配对模式**：

```
Handler (单例，处理逻辑)
    └── 持有 Manager 实例

Manager (MonoBehaviour，管理资源和状态)
    └── 继承自 BaseManager
```

**框架层 Manager：**
| Manager | 功能 |
|---------|------|
| `AIManager` | AI 管理 |
| `AudioManager` | 音频管理 |
| `CameraManager` | 摄像机管理 |
| `EffectManager` | 特效管理 |
| `GameDataManager` | 游戏数据管理 |
| `IconManager` | 图标管理 |
| `InputManager` | 输入管理 |
| `SpineManager` | Spine 动画管理 |
| `TextManager` | 文本管理 |
| `UIManager` | UI 管理 |
| `CShaderManager` | 自定义 Shader 管理 |

**框架层 Handler：**
| Handler | 功能 |
|---------|------|
| `AIHandler` | AI 逻辑处理 |
| `AudioHandler` | 音频逻辑处理 |
| `CameraHandler` | 摄像机逻辑处理 |
| `EffectHandler` | 特效逻辑处理 |
| `GameDataHandler` | 游戏数据处理 |
| `IconHandler` | 图标处理 |
| `InputHandler` | 输入处理 |
| `SpineHandler` | Spine 处理 |
| `SteamHandler` | Steam 处理 |
| `TextHandler` | 文本处理 |
| `UIHandler` | UI 逻辑处理 |
| `FPSHandler` | FPS 显示 |
| `CShaderHandler` | Shader 处理 |
| `BaseUIHandler` | UI 基础处理 |

### 3.5 BaseSystem - 基础系统

```
BaseSystem/
├── Event/
│   ├── EventEntity.cs      # 事件实体（EventSignal 泛型类）
│   └── EventHandler.cs     # 事件管理器（单例）
├── Sqlite/                  # SQLite 数据库支持
└── Steam/                   # Steam SDK 集成
```

**事件系统特点：**
- 支持 0-4 个参数的泛型事件
- 自动类型检查
- 支持全局事件回调监听
- 提供 Dispose 机制防止内存泄漏

### 3.6 Extension - 扩展方法

| 扩展类 | 功能 |
|--------|------|
| `CheckExtension` | 空值检查扩展 |
| `ColorExtension` | 颜色扩展 |
| `ComponentExtension` | 组件扩展 |
| `EnumExtension` | 枚举扩展 |
| `GameObjectExtension` | GameObject 扩展 |
| `IEnumeratorAwaitExension` | 协程等待扩展 |
| `ListArrayDicExtension` | 集合扩展 |
| `MonoExtension` | MonoBehaviour 扩展 |
| `RandomExtension` | 随机扩展 |
| `StringExtension` | 字符串扩展 |
| `TypeExtension` | 类型扩展 |
| `VectorExtension` | 向量扩展 |

### 3.7 Utils - 工具函数库

| 工具类 | 功能 |
|--------|------|
| `BeanUtil` | Bean 数据工具 |
| `CheckUtil` | 检查工具 |
| `ClassUtil` | 类工具 |
| `CptUtil` | 组件工具 |
| `DGEaseUtil` | DOTween Ease 工具 |
| `ExcelUtil` | Excel 工具 |
| `FastNoise` / `SimplexNoiseUtil` | 噪声生成 |
| `FileUtil` | 文件工具 |
| `GameUtil` | 游戏工具 |
| `JsonUtil` | JSON 序列化工具 |
| `LoadAddressablesUtil` | Addressables 加载工具 |
| `LoadAssetUtil` | 资源加载工具 |
| `LoadResourcesUtil` | Resources 加载工具 |
| `LoadWWWUtil` | WWW 加载工具 |
| `LogUtil` | 日志工具 |
| `MathUtil` | 数学工具 |
| `MeshUtil` | 网格工具 |
| `RandomUtil` | 随机工具 |
| `RayUtil` | 射线工具 |
| `ReflexUtil` | 反射工具 |
| `SceneUtil` | 场景工具 |
| `SystemUtil` | 系统工具 |
| `TextureUtil` | 纹理工具 |
| `TimeUtil` | 时间工具 |
| `TypeConversionUtil` | 类型转换工具 |
| `UGUIUtil` | UGUI 工具 |
| `UnitUtil` | 单位工具 |
| `VectorUtil` | 向量工具 |

### 3.8 Tools - 工具类

| 工具类 | 功能 |
|--------|------|
| `CreateTools` | 创建工具 |
| `DataTools` | 数据处理工具 |
| `RandomTools` | 随机工具 |
| `Serialization` | 序列化工具 |
| `WorldRandTools` | 世界随机工具 |

### 3.9 MVC - 框架 MVC 实现

```
GameConfigController  →  控制器
       ↓
GameConfigModel    →  模型
       ↓
GameConfigService  →  服务层
       ↓
IGameConfigView    →  视图接口
```

### 3.10 DataStorage - 数据存储

| 类名 | 功能 |
|------|------|
| `BaseDataRead` | 数据读取基类 |
| `BaseDataStorage` | 数据存储基类 |

### 3.11 Web - 网络请求

| 接口/类 | 功能 |
|---------|------|
| `WebRequest` | 网络请求类 |
| `IWebRequestCallBack` | 请求回调接口 |
| `IWebRequestForSpriteCallBack` | Sprite 请求回调 |
| `IWebRequestForTextureCallBack` | Texture 请求回调 |

---

## 四、游戏逻辑脚本（Scrpits）的主要模块

### 4.1 Common - 游戏通用配置

| 类名 | 功能 |
|------|------|
| `EventsInfo` | 全局事件常量定义 |
| `GameCommonInfo` | 游戏通用信息 |
| `GameInputActions` | 输入动作定义 |
| `LayerInfo` | 图层信息 |
| `PathInfo` | 路径信息 |
| `ProjectConfigInfo` | 项目配置信息 |

### 4.2 Enums - 游戏枚举定义

| 枚举类 | 功能 |
|--------|------|
| `AIIntentEnum` | AI 意图枚举 |
| `ComponentEnum` | 组件枚举 |
| `CreatureEnum` | 生物枚举 |
| `DialogEnum` | 对话框枚举 |
| `GameStateEnum` | 游戏状态枚举 |
| `ItemsEnum` | 道具枚举 |
| `MsgEnum` | 消息枚举 |
| `NpcEnum` | NPC 枚举 |
| `PopupEnum` | 弹窗枚举 |
| `ScenesEnum` | 场景枚举 |
| `ToastEnum` | 提示枚举 |

### 4.3 Game - 核心游戏逻辑

#### 4.3.1 Launcher - 启动器

```
BaseLauncher (启动器基类)
    ├── LauncherGame (游戏启动器)
    └── LauncherTest (测试启动器)
```

#### 4.3.2 Base - 游戏逻辑基类

```csharp
BaseGameLogic (继承自 BaseEvent)
    ├── gameState: GameStateEnum
    ├── PreGame()      - 准备游戏数据
    ├── StartGame()    - 开始游戏
    ├── UpdateGame()   - 游戏更新
    ├── EndGame()      - 结束游戏
    └── ClearGame()    - 清理数据
```

#### 4.3.3 Logic - 游戏逻辑实现

| 逻辑类 | 功能 |
|--------|------|
| `GameFightLogic` | 战斗逻辑基类 |
| `GameFightLogicConquer` | 征服模式战斗 |
| `GameFightLogicDoomCouncil` | 终焉议会战斗 |
| `GameFightLogicInfinite` | 无限模式战斗 |
| `GameFightLogicTest` | 测试战斗 |
| `DoomCouncilLogic` | 终焉议会逻辑 |
| `CreatureSacrificeLogic` | 生物献祭逻辑 |
| `GashaponMachineLogic` | 扭蛋机逻辑 |

#### 4.3.4 Fight - 战斗系统

```
Fight/
├── FightCreatureEntity.cs      # 战斗生物实体
├── FightPrefabEntity.cs        # 战斗预制体实体
└── AttackMode/                 # 攻击模式
    ├── BaseAttackMode.cs       # 攻击模式基类
    ├── AttackModeMelee.cs      # 近战攻击
    ├── AttackModeRanged.cs     # 远程攻击
    ├── AttackModeExplosion.cs  # 爆炸攻击
    └── ... (多种攻击模式)
```

**攻击模式类型：**
- 近战：`AttackModeMelee`, `AttackModeMeleeArea`
- 远程：`AttackModeRanged`, `AttackModeRangedArc`, `AttackModeRangedArea`, `AttackModeRangedTracking`
- 特殊：`AttackModeExplosion`, `AttackModeFallupon`, `AttackModeLure`, `AttackModeOverlap`
- 恢复：`AttackModeRegain`, `AttackModeRegainHP`, `AttackModeRegainDR`

#### 4.3.5 Buff - BUFF 系统

```
Buff/
├── BuffEntity/                 # BUFF 实体
│   ├── BuffBaseEntity.cs       # BUFF 实体基类
│   ├── Attribute/              # 属性类 BUFF
│   ├── Conditional/            # 条件类 BUFF
│   ├── Instant/                # 瞬时类 BUFF
│   └── Periodic/               # 周期性 BUFF
└── BuffPre/                    # BUFF 前置条件
    ├── BuffBasePreEntity.cs    # 前置条件基类
    └── ... (各种前置条件)
```

**BUFF 实体类型：**
- `BuffEntityBaseHPChange` - HP 变化
- `BuffEntityBaseDRChange` - 防御变化
- `BuffEntityConditionalAttack` - 条件攻击
- `BuffEntityConditionalDead` - 死亡触发
- `BuffEntityPeriodicAttackAgain` - 周期性再次攻击

#### 4.3.6 DoomCouncil - 终焉议会系统

| 类名 | 功能 |
|------|------|
| `DoomCouncilBaseEntity` | 终焉议会实体基类 |
| `DoomCouncilEntityMoreCrystal` | 更多水晶实体 |
| `DoomCouncilEntityMoreExp` | 更多经验实体 |
| `DoomCouncilEntityReincarnation` | 转生实体 |
| `DoomCouncilEntityRename` | 改名实体 |

### 4.4 Bean - 游戏数据模型

#### 4.4.1 Game Bean - 游戏数据

| Bean 类 | 功能 |
|---------|------|
| `CreatureBean` | 生物数据 |
| `CreatureAttributeBean` | 生物属性 |
| `CreatureCardItemBean` | 生物卡片 |
| `CreatureNpcBean` | 生物 NPC |
| `FightBean` | 战斗数据 |
| `FightCreatureBean` | 战斗生物数据 |
| `FightAttackBean` | 战斗攻击数据 |
| `BuffBean` / `BuffEntityBean` | BUFF 数据 |
| `ItemBean` | 道具数据 |
| `DoomCouncilBean` | 终焉议会数据 |
| `UserTempBean` | 用户临时数据 |
| `UserUnlockBean` | 用户解锁数据 |
| `AbyssalBlessingEntityBean` | 深渊馈赠数据 |

#### 4.4.2 MVC Bean - MVC 数据

| Bean 类 | 功能 |
|---------|------|
| `UserDataBean` | 用户数据 |
| `CreatureInfoBean` | 生物信息 |
| `BuffInfoBean` | BUFF 信息 |
| `ItemsInfoBean` | 道具信息 |
| `FightSceneBean` | 战斗场景信息 |
| `ResearchInfoBean` | 研究信息 |

#### 4.4.3 UI Bean - UI 数据

| Bean 类 | 功能 |
|---------|------|
| `DialogBossShowBean` | BOSS 展示对话框 |
| `DialogRenameBean` | 改名对话框 |
| `DialogSelectBean` | 选择对话框 |
| `DialogSelectCreatureBean` | 生物选择对话框 |
| `DialogSelectItemBean` | 道具选择对话框 |

### 4.5 Component - 游戏组件

#### 4.5.1 Manager - 游戏管理器

| Manager | 功能 |
|---------|------|
| `GameManager` | 游戏管理（持有 gameLogic） |
| `FightManager` | 战斗管理 |
| `BuffManager` | BUFF 管理 |
| `CreatureManager` | 生物管理 |
| `CameraManager` | 摄像机管理 |
| `AudioManager` | 音频管理 |
| `EffectManager` | 特效管理 |
| `WorldManager` | 世界管理 |
| `GameControlManager` | 游戏控制管理 |
| `GameDataManager` | 游戏数据管理 |
| `VolumeManager` | 音量管理 |

#### 4.5.2 Handler - 游戏处理器

| Handler | 功能 |
|---------|------|
| `GameHandler` | 游戏逻辑处理 |
| `FightHandler` | 战斗处理 |
| `BuffHandler` | BUFF 处理 |
| `CreatureHandler` | 生物处理 |
| `CameraHandler` | 摄像机处理 |
| `AudioHandler` | 音频处理 |
| `EffectHandler` | 特效处理 |
| `WorldHandler` | 世界处理 |
| `GameControlHandler` | 游戏控制处理 |
| `GameDataHandler` | 游戏数据处理 |
| `IconHandler` | 图标处理 |
| `SpineHandler` | Spine 处理 |
| `UIHandler` | UI 处理 |
| `VolumeHandler` | 音量处理 |

#### 4.5.3 Game - 游戏组件

```
Game/
├── Control/
│   ├── ControlForGameBase.cs      # 基础游戏控制
│   └── ControlForGameFight.cs     # 战斗游戏控制
├── Scene/
│   ├── ScenePrefabBase.cs         # 场景预制体基类
│   ├── ScenePrefabForBase.cs      # 基地场景
│   ├── ScenePrefabForDoomCouncil.cs  # 终焉议会场景
│   └── ScenePrefabForRewardSelect.cs # 奖励选择场景
└── RewardSelectBoxComponent.cs    # 奖励选择框组件
```

#### 4.5.4 UI - 游戏 UI

```
UI/
├── Common/           # 通用 UI 组件
│   ├── AbyssalBlessing/
│   ├── Backpack/
│   ├── BaseInfo/
│   ├── Buff/
│   ├── CreatureCard/
│   ├── ItemEquip/
│   └── ...
├── Dialog/           # 对话框 UI
│   ├── UIDialogBossShow
│   ├── UIDialogNormal
│   ├── UIDialogSelect
│   └── ...
├── Game/             # 游戏 UI
│   ├── BaseCore/
│   ├── BaseMain/
│   ├── FightMain/
│   ├── DoomCouncil/
│   ├── GashaponMachine/
│   └── ...
├── Popup/            # 弹窗 UI
│   ├── UIPopupCreatureCardDetails
│   ├── UIPopupDoomCouncilMainDetails
│   └── ...
└── Test/             # 测试 UI
```

### 4.6 MVC - 游戏 MVC 实现

```
MVC/
├── Controller/
│   └── UserDataController.cs
├── Model/
│   └── UserDataModel.cs
├── Service/
│   └── UserDataService.cs
└── View/
    └── IUserDataView.cs
```

### 4.7 AI - 游戏 AI 实现

```
AI/
└── Creature/
    ├── AICreatureEntity.cs          # 生物 AI 实体
    ├── AIIntentCreatureAttack.cs    # 攻击意图
    ├── AIIntentCreatureDead.cs      # 死亡意图
    ├── FightAttackCreature/         # 进攻生物 AI
    │   ├── AIAttackCreatureEntity
    │   ├── AIIntentAttackCreatureAttack
    │   ├── AIIntentAttackCreatureIdle
    │   ├── AIIntentAttackCreatureMove
    │   └── ...
    ├── FightDefenseCreature/        # 防守生物 AI
    │   ├── AIDefenseCreatureEntity
    │   ├── AIIntentDefenseCreatureAttack
    │   ├── AIIntentDefenseCreatureDefend
    │   └── ...
    └── FightDefenseCoreCreature/    # 核心生物 AI
        ├── AIDefenseCoreCreatureEntity
        └── ...
```

### 4.8 Utils - 游戏工具

| 工具类 | 功能 |
|--------|------|
| `AnimUtil` | 动画工具 |
| `FightCreatureSearchUtil` | 战斗生物搜索工具 |
| `GameUIUtil` | 游戏 UI 工具 |

---

## 五、主要的类继承关系和依赖

### 5.1 核心继承体系

```
┌─────────────────────────────────────────────────────────────┐
│                    UnityEngine.MonoBehaviour                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    BaseMonoBehaviour                         │
│  - Instantiate(), Find(), FindWithTag(), AutoLinkUI()       │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────┐
    │  BaseManager    │ │BaseComponent│ │ BaseUIInit  │
    │  (资源管理)     │ │ (组件基类)  │ │(UI 初始化)  │
    └─────────────────┘ └─────────────┘ └─────────────┘
              │                               │
              ▼                               ▼
    ┌─────────────────┐             ┌─────────────────┐
    │  UIManager      │             │   BaseUIView    │
    │  GameManager    │             │   (UI 视图)     │
    │  FightManager   │             └─────────────────┘
    │  ...            │
    └─────────────────┘
```

### 5.2 MVC 继承体系

```
┌─────────────────────────────────────────────────────────────┐
│                       BaseMVC                                │
│  - mContent (上下文对象)                                     │
│  - InitData(), SetContent(), GetContent()                   │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────┐
    │BaseMVCController│ │BaseMVCModel │ │IBaseMVCView│
    │<M,V>            │ │             │ │ (接口)      │
    └─────────────────┘ └─────────────┘ └─────────────┘
              │
              ▼
    ┌─────────────────┐
    │GameConfigController│
    │UserDataController │
    └─────────────────┘
```

### 5.3 单例继承体系

```
┌─────────────────────────────────────────────────────────────┐
│               BaseSingleton<T> where T : new()              │
│  - Instance (懒汉式单例)                                     │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
    ┌─────────────────┐             ┌─────────────────┐
    │  EventHandler   │             │BaseSingletonMono│
    │  (事件管理器)   │             │Behaviour<T>     │
    └─────────────────┘             └─────────────────┘
                                            │
                                            ▼
                                  ┌─────────────────┐
                                  │BaseHandler<T,M> │
                                  │(Handler 基类)   │
                                  └─────────────────┘
```

### 5.4 AI 状态机体系

```
┌─────────────────────────────────────────────────────────────┐
│                    AIBaseEntity                              │
│  - listIntentEnum (意图列表)                                 │
│  - currentIntent (当前意图)                                  │
│  - dicIntentPool (意图池)                                    │
│  - ChangeIntent(), InitIntentEntity()                       │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
    ┌─────────────────┐             ┌─────────────────┐
    │AICreatureEntity │             │  AIBaseIntent   │
    │(生物 AI 实体)    │             │  (意图基类)     │
    └─────────────────┘             └─────────────────┘
                                            │
                          ┌─────────────────┼─────────────────┐
                          ▼                 ▼                 ▼
                ┌─────────────────┐ ┌─────────────┐ ┌─────────────┐
                │AIIntentAttack...│ │AIIntent...  │ │AIIntent...  │
                │(攻击意图)       │ │(移动意图)   │ │(死亡意图)   │
                └─────────────────┘ └─────────────┘ └─────────────┘
```

### 5.5 事件系统体系

```
┌─────────────────────────────────────────────────────────────┐
│                    BaseEvent                                 │
│  - RegisterEvent(), UnRegisterEvent()                       │
│  - TriggerEvent(), UnRegisterAllEvent()                     │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
    ┌─────────────────┐             ┌─────────────────┐
    │  EventHandler   │             │   BaseUIInit    │
    │  (全局事件管理) │             │BaseGameLogic    │
    │  (单例)         │             │AIBaseEntity     │
    └─────────────────┘             └─────────────────┘
```

### 5.6 Handler-Manager 配对模式

```
┌─────────────────────────────────────────────────────────────┐
│              BaseHandler<T, M>                               │
│  where M : BaseManager                                       │
│  where T : BaseMonoBehaviour                                 │
│                                                              │
│  - manager (自动创建/获取 Manager 实例)                        │
└─────────────────────────────────────────────────────────────┘

示例：
┌─────────────────┐         ┌─────────────────┐
│  UIHandler      │ ──────► │   UIManager     │
│  (单例，逻辑)   │  持有   │   (MonoBehaviour)│
└─────────────────┘         └─────────────────┘

┌─────────────────┐         ┌─────────────────┐
│  GameHandler    │ ──────► │   GameManager   │
│  (单例，逻辑)   │  持有   │   (MonoBehaviour)│
└─────────────────┘         └─────────────────┘
```

### 5.7 游戏逻辑继承体系

```
┌─────────────────────────────────────────────────────────────┐
│                    BaseGameLogic                             │
│  (继承自 BaseEvent)                                          │
│  - gameState (游戏状态)                                      │
│  - PreGame(), StartGame(), UpdateGame()                     │
│  - EndGame(), ClearGame()                                   │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────┐
    │GameFightLogic   │ │DoomCouncil  │ │Gashapon     │
    │(战斗逻辑基类)   │ │Logic        │ │MachineLogic │
    └─────────────────┘ │(终焉议会)   │ │(扭蛋机)     │
              │         └─────────────┘ └─────────────┘
              ▼
    ┌─────────────────┐
    │GameFightLogic   │
    │ForConquer/      │
    │ForInfinite/     │
    │ForTest          │
    └─────────────────┘
```

### 5.8 核心依赖关系图

```
┌─────────────────────────────────────────────────────────────┐
│                      启动流程                                │
└─────────────────────────────────────────────────────────────┘

LauncherGame.Launch()
       │
       ▼
WorldHandler.EnterMainForBaseScene()
       │
       ▼
┌─────────────────────────────────────────────────────────────┐
│                      游戏循环                                │
└─────────────────────────────────────────────────────────────┘

GameHandler.Update()
       │
       ▼
manager.gameLogic.UpdateGame()
       │
       ├──► FightHandler.UpdateData()      (战斗更新)
       ├──► BuffHandler.UpdateData()       (BUFF 更新)
       ├──► CreatureHandler.Update()       (生物更新)
       └──► AIHandler.Update()             (AI 更新)

┌─────────────────────────────────────────────────────────────┐
│                      事件驱动                               │
└─────────────────────────────────────────────────────────────┘

EventHandler.Instance.TriggerEvent(EventsInfo.XXX)
       │
       ▼
所有注册该事件的监听器收到通知
       │
       ├──► UIHandler (刷新 UI)
       ├──► GameHandler (处理游戏逻辑)
       └──► 其他监听器
```

---

## 六、设计模式总结

### 6.1 使用的设计模式

| 模式 | 应用场景 |
|------|----------|
| **单例模式** | `BaseSingleton<T>`, `EventHandler`, 所有 Handler 类 |
| **MVC 模式** | `GameConfigController/Model/View`, `UserDataController/Model/View` |
| **状态机模式** | AI 系统 (`AIBaseEntity` + `AIBaseIntent`) |
| **观察者模式** | 事件系统 (`BaseEvent`, `EventHandler`) |
| **工厂模式** | `ReflexUtil.CreateInstance<T>()` 通过反射创建实例 |
| **策略模式** | 攻击模式 (`BaseAttackMode` 及其子类) |
| **模板方法模式** | `BaseGameLogic` 定义游戏流程框架 |
| **组合模式** | BUFF 系统 (多种 BUFF 组合) |
| **责任链模式** | Handler-Manager 配对 |

### 6.2 架构特点

1. **分层清晰**：框架层与游戏逻辑层分离
2. **组件化设计**：Handler-Manager 配对，职责明确
3. **事件驱动**：全局事件系统解耦各模块
4. **数据驱动**：Bean 层统一管理数据结构
5. **扩展性强**：大量使用泛型和继承

---

## 七、关键技术点

### 7.1 资源加载

- **Addressables**: 异步资源加载 (`LoadAddressablesUtil`)
- **Resources**: 同步资源加载 (`LoadResourcesUtil`)
- **AssetBundle**: 资源包加载 (`LoadAssetUtil`)
- **缓存机制**: Manager 中维护资源字典避免重复加载

### 7.2 UI 系统

- **多层级 UI 容器**: `UITypeEnum` 定义不同层级 (Base, Dialog, Toast, Popup, Overlay)
- **自动链接 UI**: `AutoLinkUI()` 通过反射自动链接控件
- **UI 动画**: 集成 DOTween 实现打开/关闭动画
- **UI 管理**: 单例管理所有打开的 UI

### 7.3 战斗系统

- **实体组件**: `FightCreatureEntity` 管理战斗生物
- **攻击模式**: 多种攻击方式策略化
- **BUFF 系统**: 条件/瞬时/周期性 BUFF
- **AI 状态机**: 生物行为状态机控制

### 7.4 数据持久化

- **Json 序列化**: `JsonUtil` 封装 Newtonsoft.Json
- **PlayerPrefs**: 简单数据保存
- **SQLite**: 复杂数据存储 (BaseSystem/Sqlite)

---

## 八、总结

这是一个架构设计良好的 Unity Roguelike 塔防游戏项目，具有以下特点：

1. **完整的框架体系**：提供了从基础类、MVC、事件、AI 到 UI 的完整框架
2. **清晰的代码分层**：框架层与游戏逻辑层职责分明
3. **高度组件化**：Handler-Manager 模式使代码易于维护和扩展
4. **事件驱动架构**：通过全局事件系统实现模块间解耦
5. **丰富的设计模式应用**：单例、MVC、状态机、观察者等模式贯穿整个项目

项目适合中大型 Unity 游戏开发，特别是需要复杂游戏逻辑和良好架构的项目。
