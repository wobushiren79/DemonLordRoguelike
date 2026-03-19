# 魔王 Roguelike 项目完整文档

> 项目文档生成时间：2026 年 3 月 19 日  
> Unity 项目 - Roguelike 塔防游戏

---

## 目录

1. [项目概述](#一项目概述)
2. [目录结构](#二目录结构)
3. [框架层 (FrameWork)](#三框架层-framework)
4. [游戏逻辑层 (Scrpits)](#四游戏逻辑层-scrpits)
5. [核心类详解](#五核心类详解)
6. [设计模式](#六设计模式)
7. [技术要点](#七技术要点)

---

## 一、项目概述

这是一个基于 Unity 引擎开发的**Roguelike 塔防游戏**，采用了自定义的 MVC 框架和组件化架构设计。

### 1.1 技术栈

| 类别 | 技术/工具 |
|------|----------|
| 游戏引擎 | Unity |
| 编程语言 | C# |
| UI 框架 | UGUI + DOTween |
| 动画系统 | Spine |
| 资源管理 | Addressables |
| 数据存储 | JSON, SQLite, PlayerPrefs |
| 网络请求 | UnityWebRequest |
| 平台集成 | Steamworks.NET |

### 1.2 项目统计

- **C# 脚本数量**: 921 个
- **主要框架模块**: 13 个
- **游戏逻辑模块**: 8 个
- **设计模式应用**: 9 种

---

## 二、目录结构

```
Assets/
├── FrameWork/                          # 框架代码层 - 通用基础框架
│   ├── Editor/                         # 框架编辑器扩展
│   ├── Addons/                         # 第三方插件
│   │   ├── DOTween/                    # 动画补间插件
│   │   └── Rotary Heart/               # 可序列化字典
│   ├── Plugins/                        # 原生插件
│   │   └── Steamworks.NET/             # Steam SDK
│   └── Scrpits/                        # 框架脚本
│       ├── AI/                         # AI 基础框架
│       ├── Base/                       # 基础类库
│       ├── BaseSystem/                 # 基础系统
│       ├── Bean/                       # 数据模型
│       ├── CallBack/                   # 回调接口
│       ├── Common/                     # 通用组件
│       ├── Component/                  # 框架组件
│       ├── DataStorage/                # 数据存储
│       ├── Enums/                      # 框架枚举
│       ├── Extension/                  # 扩展方法
│       ├── MVC/                        # MVC 实现
│       ├── Tools/                      # 工具类
│       ├── Utils/                      # 工具函数
│       └── Web/                        # 网络请求
│
├── Scrpits/                            # 游戏逻辑代码层
│   ├── AI/                             # 游戏 AI 实现
│   ├── Bean/                           # 游戏数据模型
│   ├── Common/                         # 游戏通用配置
│   ├── Component/                      # 游戏组件
│   ├── Enums/                          # 游戏枚举
│   ├── Game/                           # 核心游戏逻辑
│   │   ├── Base/                       # 游戏逻辑基类
│   │   ├── Buff/                       # BUFF 系统
│   │   ├── DoomCouncil/                # 终焉议会系统
│   │   ├── Fight/                      # 战斗系统
│   │   ├── Launcher/                   # 启动器
│   │   └── Logic/                      # 游戏逻辑实现
│   ├── MVC/                            # 游戏 MVC 实现
│   ├── Struct/                         # 结构体定义
│   └── Utils/                          # 游戏工具
│
├── Data/                               # 游戏数据配置 (Excel/JSON)
├── Resources/                          # Unity 资源
├── Scenes/                             # 场景文件
├── Editor/                             # 编辑器扩展
├── LoadResources/                      # 加载资源
└── AddressableAssetsData/              # Addressables 配置
```

---

## 三、框架层 (FrameWork)

### 3.1 Base - 基础类库

#### 3.1.1 BaseMonoBehaviour

所有 MonoBehaviour 的基类，提供通用方法。

```csharp
public class BaseMonoBehaviour : MonoBehaviour
{
    // 实例化 GameObject
    public GameObject Instantiate(GameObject objContent, GameObject objModel);
    public GameObject Instantiate(GameObject objContent, GameObject objModel, Vector3 position);
    
    // 查找组件
    public T Find<T>(string name);
    public Component Find(string name, Type type);
    public T FindInChildren<T>(string name);
    
    // 通过标签查找
    public T FindWithTag<T>(string tag, string name = null);
    public List<T> FindListWithTag<T>(string tag);
    
    // 自动链接 UI 控件 (通过反射)
    public void AutoLinkUI();
}
```

**功能说明：**
- 封装常用的 `Instantiate` 方法
- 提供便捷的 `Find` 系列方法
- 支持通过 `ui_` 前缀自动链接 UI 控件

#### 3.1.2 BaseSingleton<T>

泛型单例模式基类（非 MonoBehaviour）。

```csharp
public abstract class BaseSingleton<T> where T : new()
{
    protected static T instance;
    protected static object syncRoot = new Object();
    
    public static T Instance
    {
        get
        {
            // 双重检查锁实现线程安全单例
        }
    }
}
```

#### 3.1.3 BaseSingletonMonoBehaviour<T>

MonoBehaviour 单例基类。

```csharp
public class BaseSingletonMonoBehaviour<T> : BaseMonoBehaviour
    where T : BaseMonoBehaviour
{
    private static T _instance;
    public static T Instance { get; }
}
```

#### 3.1.4 BaseMVC

MVC 模式基类，提供上下文对象管理。

```csharp
public abstract class BaseMVC
{
    protected BaseMonoBehaviour mContent;  // 上下文对象
    
    public abstract void InitData();
    public void SetContent(BaseMonoBehaviour content);
    public BaseMonoBehaviour GetContent();
}
```

#### 3.1.5 BaseManager

管理器基类，继承自 `BaseMonoBehaviour`，提供资源加载、数据管理功能。

**核心功能：**
- 数据初始化：`InitData<T>()`
- 资源加载：`GetModel<T>()`, `GetModelsForAddressables<T>()`
- Sprite 加载：`GetSpriteByName()`
- 音频/动画/Tile 加载

**资源加载方式：**
| 方法 | 说明 |
|------|------|
| `GetModel<T>()` | 同步加载单个资源 |
| `GetModelForAddressables<T>()` | Addressables 异步加载 |
| `GetModelsForAddressables<T>()` | Addressables 批量加载 |
| `GetSpriteByName()` | 从 SpriteAtlas 加载精灵 |

#### 3.1.6 BaseHandler<T, M>

处理器基类，采用 Handler-Manager 配对模式。

```csharp
public class BaseHandler<T, M> : BaseSingletonMonoBehaviour<T>
    where M : BaseManager
    where T : BaseMonoBehaviour
{
    private M _manager;
    
    public M manager
    {
        get
        {
            if (_manager == null)
            {
                _manager = gameObject.AddComponentEX<M>();
            }
            return _manager;
        }
    }
}
```

**特点：**
- Handler 是单例，处理纯逻辑
- Manager 是 MonoBehaviour，管理资源和状态
- Handler 自动创建/获取 Manager 实例

#### 3.1.7 BaseEvent

事件基类，提供事件注册/注销功能。

```csharp
public class BaseEvent
{
    // 注册事件
    public void RegisterEvent(string eventName, Action action);
    public void RegisterEvent<A>(string eventName, Action<A> action);
    // ... 支持 0-4 个参数
    
    // 注销事件
    public void UnRegisterEvent(string eventName);
    public void UnRegisterAllEvent();
    
    // 触发事件
    public void TriggerEvent(string eventName);
    public void TriggerEvent<A>(string eventName, A data);
    // ... 支持 0-4 个参数
}
```

### 3.2 AI - AI 基础框架

采用**状态机模式**实现 AI 系统。

#### 3.2.1 AIBaseEntity

AI 实体基类。

```csharp
public class AIBaseEntity : BaseMonoBehaviour
{
    protected List<AIIntentEnum> listIntentEnum;  // 意图列表
    protected AIBaseIntent currentIntent;          // 当前意图
    protected Dictionary<AIIntentEnum, AIBaseIntent> dicIntentPool;  // 意图池
    
    public void ChangeIntent(AIIntentEnum intentEnum);
    public virtual void InitIntentEntity();
    public virtual void IntentUpdate();
    public virtual void IntentFixUpdate();
}
```

#### 3.2.2 AIBaseIntent

AI 意图基类。

```csharp
public abstract class AIBaseIntent
{
    protected AIBaseEntity entity;
    
    public virtual void IntentEntering() { }      // 进入意图
    public virtual void IntentUpdate() { }        // 更新表现
    public virtual void IntentFixUpdate() { }     // 物理更新
    public virtual void IntentLeaving() { }       // 离开意图
}
```

### 3.3 Bean - 数据模型层

框架层 Bean 主要包含通用数据结构。

#### 3.3.1 基础 Bean

| Bean 类 | 功能 |
|---------|------|
| `BaseBean` | 基础数据类（包含 id, name 等） |
| `BaseDataBean` | 基础数据扩展 |
| `BaseInfoBean` | 基础信息类 |

#### 3.3.2 资源 Bean

| Bean 类 | 功能 |
|---------|------|
| `AudioBean` | 音频数据 |
| `AnimBean` | 动画数据 |
| `EffectBean` | 特效数据 |
| `IconBean` | 图标数据 |
| `ImageResBean` | 图片资源数据 |

#### 3.3.3 UI Bean

| Bean 类 | 功能 |
|---------|------|
| `DialogBean` | 对话框数据 |
| `PopupBean` | 弹窗数据 |
| `ToastBean` | 提示数据 |
| `ProgressBean` | 进度条数据 |

### 3.4 Component - 框架组件

采用 **Handler-Manager 配对模式**。

#### 3.4.1 框架层 Manager

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

#### 3.4.2 框架层 Handler

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

#### 3.5.1 EventHandler

全局事件管理器（单例）。

```csharp
public class EventHandler : BaseSingleton<EventHandler>
{
    // 事件字典
    private Dictionary<string, Delegate> eventDict = new Dictionary<string, Delegate>();
    
    // 注册事件
    public void RegisterEvent(string eventName, Action action);
    public void RegisterEvent<A>(string eventName, Action<A> action);
    // ...
    
    // 注销事件
    public void UnRegisterEvent(string eventName, Action action);
    // ...
    
    // 触发事件
    public void TriggerEvent(string eventName);
    public void TriggerEvent<A>(string eventName, A data);
    // ...
}
```

### 3.6 Extension - 扩展方法

| 扩展类 | 功能 |
|--------|------|
| `CheckExtension` | 空值检查扩展 (`IsNull()`, `IsNotNull()`) |
| `ColorExtension` | 颜色扩展 (`ToHexString()`, `ToRGBAString()`) |
| `ComponentExtension` | 组件扩展 (`AddComponentEX()`) |
| `EnumExtension` | 枚举扩展 (`GetEnumName()`) |
| `GameObjectExtension` | GameObject 扩展 (`SetActiveEX()`, `FindChild()`) |
| `IEnumeratorAwaitExension` | 协程等待扩展 |
| `ListArrayDicExtension` | 集合扩展 (`ForEach()`, `AddRange()`) |
| `MonoExtension` | MonoBehaviour 扩展 (`StartCoroutineEx()`) |
| `RandomExtension` | 随机扩展 (`RandomRange()`, `RandomItem()`) |
| `StringExtension` | 字符串扩展 (`IsNullOrEmpty()`, `ToLong()`) |
| `TypeExtension` | 类型扩展 |
| `VectorExtension` | 向量扩展 (`ToVector2()`, `ToVector3()`) |

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

### 3.8 MVC - 框架 MVC 实现

```
GameConfigController  →  控制器
       ↓
GameConfigModel    →  模型
       ↓
GameConfigService  →  服务层
       ↓
IGameConfigView    →  视图接口
```

### 3.9 DataStorage - 数据存储

| 类名 | 功能 |
|------|------|
| `BaseDataRead` | 数据读取基类 |
| `BaseDataStorage` | 数据存储基类 |

### 3.10 Web - 网络请求

| 接口/类 | 功能 |
|---------|------|
| `WebRequest` | 网络请求类 |
| `IWebRequestCallBack` | 请求回调接口 |
| `IWebRequestForSpriteCallBack` | Sprite 请求回调 |
| `IWebRequestForTextureCallBack` | Texture 请求回调 |

---

## 四、游戏逻辑层 (Scrpits)

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
public abstract class BaseGameLogic : BaseEvent
{
    protected GameStateEnum gameState;
    
    public abstract void PreGame();      // 准备游戏数据
    public abstract void StartGame();    // 开始游戏
    public abstract void UpdateGame();   // 游戏更新
    public abstract void EndGame();      // 结束游戏
    public abstract void ClearGame();    // 清理数据
}
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

| 类型 | 类名 |
|------|------|
| 近战 | `AttackModeMelee`, `AttackModeMeleeArea` |
| 远程 | `AttackModeRanged`, `AttackModeRangedArc`, `AttackModeRangedArea`, `AttackModeRangedTracking` |
| 特殊 | `AttackModeExplosion`, `AttackModeFallupon`, `AttackModeLure`, `AttackModeOverlap` |
| 恢复 | `AttackModeRegain`, `AttackModeRegainHP`, `AttackModeRegainDR` |

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

| 类型 | 类名 |
|------|------|
| 属性变化 | `BuffEntityBaseHPChange`, `BuffEntityBaseDRChange` |
| 条件触发 | `BuffEntityConditionalAttack`, `BuffEntityConditionalDead` |
| 周期性 | `BuffEntityPeriodicAttackAgain` |
| 死亡触发 | `BuffEntityConditionalDeadAttack`, `BuffEntityConditionalDeadCreateCrystal` |

#### 4.3.6 DoomCouncil - 终焉议会系统

| 类名 | 功能 |
|------|------|
| `DoomCouncilBaseEntity` | 终焉议会实体基类 |
| `DoomCouncilEntityMoreCrystal` | 更多水晶实体 |
| `DoomCouncilEntityMoreExp` | 更多经验实体 |
| `DoomCouncilEntityReincarnation` | 转生实体 |
| `DoomCouncilEntityRename` | 改名实体 |

### 4.4 Bean - 游戏数据模型

#### 4.4.1 Game Bean

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

#### 4.4.2 UI Bean

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

#### 4.5.3 UI - 游戏 UI

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

### 4.6 AI - 游戏 AI 实现

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

---

## 五、核心类详解

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

## 六、设计模式

### 6.1 使用的设计模式

| 模式 | 应用场景 | 实现类 |
|------|----------|--------|
| **单例模式** | 全局管理器 | `BaseSingleton<T>`, `EventHandler`, 所有 Handler 类 |
| **MVC 模式** | UI 和数据管理 | `GameConfigController/Model/View`, `UserDataController/Model/View` |
| **状态机模式** | AI 系统 | `AIBaseEntity` + `AIBaseIntent` |
| **观察者模式** | 事件系统 | `BaseEvent`, `EventHandler` |
| **工厂模式** | 对象创建 | `ReflexUtil.CreateInstance<T>()` |
| **策略模式** | 攻击模式 | `BaseAttackMode` 及其子类 |
| **模板方法模式** | 游戏流程 | `BaseGameLogic` 定义游戏流程框架 |
| **组合模式** | BUFF 系统 | 多种 BUFF 组合 |
| **责任链模式** | Handler-Manager | Handler 持有 Manager |

### 6.2 架构特点

1. **分层清晰**：框架层与游戏逻辑层分离
2. **组件化设计**：Handler-Manager 配对，职责明确
3. **事件驱动**：全局事件系统解耦各模块
4. **数据驱动**：Bean 层统一管理数据结构
5. **扩展性强**：大量使用泛型和继承

---

## 七、技术要点

### 7.1 资源加载

| 方式 | 工具类 | 说明 |
|------|--------|------|
| **Addressables** | `LoadAddressablesUtil` | 异步资源加载，支持缓存 |
| **Resources** | `LoadResourcesUtil` | 同步资源加载 |
| **AssetBundle** | `LoadAssetUtil` | 资源包加载 |
| **WWW** | `LoadWWWUtil` | 网络资源加载 |

**缓存机制：**
- Manager 中维护资源字典避免重复加载
- `Dictionary<string, T>` 存储已加载资源
- SpriteAtlas 懒加载

### 7.2 UI 系统

**多层级 UI 容器：**
```csharp
public enum UITypeEnum
{
    Base,       // 基础层
    Dialog,     // 对话框层
    Toast,      // 提示层
    Popup,      // 弹窗层
    Overlay     // 覆盖层
}
```

**自动链接 UI：**
```csharp
// 通过反射自动链接 ui_前缀的控件
public void AutoLinkUI()
{
    ReflexUtil.AutoLinkDataForChild(this, "ui_");
}
```

**UI 动画：**
- 集成 DOTween 实现打开/关闭动画
- 支持 `DOFade()`, `DOAnchorPos()` 等补间动画
- 使用 `Ease.OutExpo`, `Ease.InExpo` 等缓动函数

### 7.3 战斗系统

**实体组件：**
- `FightCreatureEntity` - 战斗生物实体
- `FightPrefabEntity` - 战斗预制体实体

**攻击模式：**
- 近战：`AttackModeMelee`, `AttackModeMeleeArea`
- 远程：`AttackModeRanged`, `AttackModeRangedArc`
- 特殊：`AttackModeExplosion`, `AttackModeFallupon`

**BUFF 系统：**
- 属性类：HP 变化、防御变化
- 条件类：攻击触发、死亡触发
- 瞬时类：立即生效
- 周期性：定时触发

### 7.4 数据持久化

| 方式 | 工具类 | 适用场景 |
|------|--------|----------|
| **JSON** | `JsonUtil` | 复杂数据结构 |
| **PlayerPrefs** | - | 简单键值对 |
| **SQLite** | `BaseSystem/Sqlite` | 大量结构化数据 |

---

## 八、总结

这是一个架构设计良好的 Unity Roguelike 塔防游戏项目，具有以下特点：

### 8.1 架构优势

| 特点 | 说明 |
|------|------|
| **完整的框架体系** | 提供了从基础类、MVC、事件、AI 到 UI 的完整框架 |
| **清晰的代码分层** | 框架层与游戏逻辑层职责分明 |
| **高度组件化** | Handler-Manager 模式使代码易于维护和扩展 |
| **事件驱动架构** | 通过全局事件系统实现模块间解耦 |
| **丰富的设计模式** | 单例、MVC、状态机、观察者等模式贯穿整个项目 |

### 8.2 适用场景

- 中大型 Unity 游戏开发
- 需要复杂游戏逻辑的项目
- 需要良好架构支撑的长期维护项目
- Roguelike、塔防、卡牌等类型游戏

### 8.3 学习价值

- 学习 Unity MVC 架构实现
- 理解 Handler-Manager 组件化设计
- 掌握事件驱动架构模式
- 了解 Roguelike 游戏核心系统设计

---

*文档结束*
