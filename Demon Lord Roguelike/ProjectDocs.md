# 魔王 Roguelike 项目文档

> 最后更新：2026 年 3 月 25 日
> Unity Roguelike 塔防游戏
> 架构设计详见 [ProjectFrame.md](ProjectFrame.md)

---

## 目录

1. [项目概述](#一项目概述)
2. [目录结构](#二目录结构)
3. [框架层 (FrameWork)](#三框架层-framework)
4. [游戏逻辑层 (Scrpits)](#四游戏逻辑层-scrpits)
5. [技术要点](#五技术要点)

---

## 一、项目概述

基于 Unity 引擎开发的 **Roguelike 塔防游戏**，采用自定义 MVC 框架和组件化架构设计。

### 1.1 技术栈

| 类别 | 技术/工具 |
|------|----------|
| 游戏引擎 | Unity |
| 编程语言 | C# |
| UI 框架 | UGUI + DOTween |
| 动画系统 | Spine |
| 资源管理 | Addressables |
| 数据存储 | JSON (Newtonsoft.Json)、SQLite、PlayerPrefs |
| 网络请求 | UnityWebRequest |
| 平台集成 | Steamworks.NET |

### 1.2 项目规模

- **C# 脚本**: 921 个
- **框架模块**: 13 个
- **游戏逻辑模块**: 8 个
- **设计模式**: 9 种

---

## 二、目录结构

```
Assets/
├── FrameWork/                          # 框架代码层 - 通用基础框架
│   ├── Editor/                         # 框架编辑器扩展
│   ├── Addons/                         # 第三方插件
│   │   ├── DOTween/                    # 动画补间插件
│   │   └── Rotary Heart/              # 可序列化字典
│   ├── Plugins/                        # 原生插件
│   │   └── Steamworks.NET/             # Steam SDK
│   └── Scrpits/                        # 框架脚本
│       ├── AI/                         # AI 基础框架（状态机模式）
│       ├── Base/                       # 基础类库（MVC、单例、事件等）
│       ├── BaseSystem/                 # 基础系统（事件、SQLite、Steam）
│       ├── Bean/                       # 数据模型
│       ├── CallBack/                   # 回调接口
│       ├── Common/                     # 通用组件
│       ├── Component/                  # 框架组件（Manager、Handler、UI）
│       ├── DataStorage/                # 数据存储读写
│       ├── Enums/                      # 框架枚举
│       ├── Extension/                  # 扩展方法
│       ├── MVC/                        # MVC 实现
│       ├── Tools/                      # 工具类
│       ├── Utils/                      # 工具函数
│       └── Web/                        # 网络请求
│
├── Scrpits/                            # 游戏逻辑代码层
│   ├── AI/                             # 游戏 AI 实现（生物 AI）
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

#### BaseMonoBehaviour

所有 MonoBehaviour 的基类，提供通用方法。

```csharp
public class BaseMonoBehaviour : MonoBehaviour
{
    // 实例化 GameObject
    public GameObject Instantiate(GameObject objContent, GameObject objModel);
    public GameObject Instantiate(GameObject objContent, GameObject objModel, Vector3 position);

    // 查找组件
    public T Find<T>(string name);
    public T FindInChildren<T>(string name);

    // 通过标签查找
    public T FindWithTag<T>(string tag, string name = null);
    public List<T> FindListWithTag<T>(string tag);

    // 通过反射自动链接 ui_ 前缀的 UI 控件
    public void AutoLinkUI();
}
```

#### BaseSingleton\<T\>

泛型单例模式基类（非 MonoBehaviour），双重检查锁实现线程安全。

```csharp
public abstract class BaseSingleton<T> where T : new()
{
    protected static T instance;
    protected static object syncRoot = new Object();
    public static T Instance { get; }
}
```

#### BaseSingletonMonoBehaviour\<T\>

MonoBehaviour 单例基类。

```csharp
public class BaseSingletonMonoBehaviour<T> : BaseMonoBehaviour
    where T : BaseMonoBehaviour
{
    private static T _instance;
    public static T Instance { get; }
}
```

#### BaseMVC

MVC 模式基类，提供上下文对象管理。

```csharp
public abstract class BaseMVC
{
    protected BaseMonoBehaviour mContent;
    public abstract void InitData();
    public void SetContent(BaseMonoBehaviour content);
    public BaseMonoBehaviour GetContent();
}
```

#### BaseManager

管理器基类，继承自 `BaseMonoBehaviour`，提供资源加载和数据管理。

| 方法 | 说明 |
|------|------|
| `InitData<T>()` | 数据初始化 |
| `GetModel<T>()` | 同步加载单个资源 |
| `GetModelForAddressables<T>()` | Addressables 异步加载 |
| `GetModelsForAddressables<T>()` | Addressables 批量加载 |
| `GetSpriteByName()` | 从 SpriteAtlas 加载精灵 |

#### BaseHandler\<T, M\>

Handler-Manager 配对模式的处理器基类。Handler 为单例处理纯逻辑，自动创建/获取 Manager 实例。

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
                _manager = gameObject.AddComponentEX<M>();
            return _manager;
        }
    }
}
```

#### BaseEvent

事件基类，支持 0-4 个参数的泛型事件注册/注销/触发。

```csharp
public class BaseEvent
{
    public void RegisterEvent(string eventName, Action action);
    public void RegisterEvent<A>(string eventName, Action<A> action);
    // ... 支持 0-4 个参数

    public void UnRegisterEvent(string eventName);
    public void UnRegisterAllEvent();

    public void TriggerEvent(string eventName);
    public void TriggerEvent<A>(string eventName, A data);
    // ... 支持 0-4 个参数
}
```

#### 其他基础类

| 类名 | 功能 |
|------|------|
| `BaseMVCController<M,V>` | MVC 控制器基类 |
| `BaseMVCModel` | MVC 模型基类 |
| `BaseComponent` | 组件基类 |
| `BaseObservable` | 观察者模式基类 |
| `BaseUIManager` | UI 管理器基类 |
| `BaseUIView` | UI 视图基类 |
| `BaseUIInit` | UI 初始化基类，自动链接 UI 控件 |

### 3.2 AI - AI 基础框架

采用**状态机模式**实现。

```csharp
// AI 实体基类
public class AIBaseEntity : BaseMonoBehaviour
{
    protected List<AIIntentEnum> listIntentEnum;               // 意图列表
    protected AIBaseIntent currentIntent;                      // 当前意图
    protected Dictionary<AIIntentEnum, AIBaseIntent> dicIntentPool; // 意图池

    public void ChangeIntent(AIIntentEnum intentEnum);
    public virtual void InitIntentEntity();
    public virtual void IntentUpdate();
    public virtual void IntentFixUpdate();
}

// AI 意图基类
public abstract class AIBaseIntent
{
    protected AIBaseEntity entity;

    public virtual void IntentEntering() { }   // 进入意图
    public virtual void IntentUpdate() { }     // 更新表现
    public virtual void IntentFixUpdate() { }  // 物理更新
    public virtual void IntentLeaving() { }    // 离开意图
}
```

核心文件：`AIBaseEntity.cs`、`AIBaseIntent.cs`、`AIBaseCommon.cs`

### 3.3 Bean - 数据模型层

| 类别 | Bean 类 |
|------|---------|
| 基础 | `BaseBean`、`BaseDataBean`、`BaseInfoBean` |
| 资源 | `AudioBean`、`AnimBean`、`EffectBean`、`IconBean`、`ImageResBean` |
| UI | `DialogBean`、`PopupBean`、`ToastBean`、`ProgressBean` |
| 数据 | `DataBean`、`DataStorageListBean`、`DictionaryListBean` |
| 工具 | `ColorBean`、`NumberBean`、`TimeBean`、`Vector3Bean`、`Vector3IntBean` |
| 游戏 | `GameConfigBean`、`ScenesChangeBean`、`GameTimeCountDownBean` |
| 特殊 | `SpineSkinBean`、`TileBean`、`MeshDataCustom` |

### 3.4 Component - 框架组件

#### Manager（资源和状态管理）

| Manager | 功能 |
|---------|------|
| `AIManager` | AI 管理 |
| `AudioManager` | 音频管理 |
| `CameraManager` | 摄像机管理 |
| `CShaderManager` | 自定义 Shader 管理 |
| `EffectManager` | 特效管理 |
| `GameDataManager` | 游戏数据管理 |
| `IconManager` | 图标管理 |
| `InputManager` | 输入管理 |
| `SpineManager` | Spine 动画管理 |
| `TextManager` | 文本管理 |
| `UIManager` | UI 管理 |

#### Handler（逻辑处理，均为单例）

| Handler | 功能 |
|---------|------|
| `AIHandler` | AI 逻辑处理 |
| `AudioHandler` | 音频逻辑处理 |
| `BaseUIHandler` | UI 基础处理 |
| `CameraHandler` | 摄像机逻辑处理 |
| `CShaderHandler` | Shader 处理 |
| `EffectHandler` | 特效逻辑处理 |
| `FPSHandler` | FPS 显示 |
| `GameDataHandler` | 游戏数据处理 |
| `IconHandler` | 图标处理 |
| `InputHandler` | 输入处理 |
| `SpineHandler` | Spine 处理 |
| `SteamHandler` | Steam 处理 |
| `TextHandler` | 文本处理 |
| `UIHandler` | UI 逻辑处理 |

### 3.5 BaseSystem - 基础系统

```
BaseSystem/
├── Event/
│   ├── EventEntity.cs      # 事件实体（EventSignal 泛型类）
│   └── EventHandler.cs     # 全局事件管理器（单例）
├── Sqlite/                  # SQLite 数据库支持
└── Steam/                   # Steam SDK 集成
```

**EventHandler** - 全局事件管理器（单例），支持 0-4 个参数的泛型事件，自动类型检查，提供 Dispose 机制防止内存泄漏。

```csharp
public class EventHandler : BaseSingleton<EventHandler>
{
    private Dictionary<string, Delegate> eventDict;

    public void RegisterEvent(string eventName, Action action);
    public void RegisterEvent<A>(string eventName, Action<A> action);
    public void UnRegisterEvent(string eventName, Action action);
    public void TriggerEvent(string eventName);
    public void TriggerEvent<A>(string eventName, A data);
}
```

### 3.6 Extension - 扩展方法

| 扩展类 | 功能 | 关键方法 |
|--------|------|----------|
| `CheckExtension` | 空值检查 | `IsNull()`、`IsNotNull()` |
| `ColorExtension` | 颜色转换 | `ToHexString()`、`ToRGBAString()` |
| `ComponentExtension` | 组件操作 | `AddComponentEX()` |
| `EnumExtension` | 枚举处理 | `GetEnumName()` |
| `GameObjectExtension` | GO 操作 | `SetActiveEX()`、`FindChild()` |
| `IEnumeratorAwaitExension` | 协程等待 | 协程 await 支持 |
| `ListArrayDicExtension` | 集合操作 | `ForEach()`、`AddRange()` |
| `MonoExtension` | Mono 扩展 | `StartCoroutineEx()` |
| `RandomExtension` | 随机扩展 | `RandomRange()`、`RandomItem()` |
| `StringExtension` | 字符串处理 | `IsNullOrEmpty()`、`ToLong()` |
| `TypeExtension` | 类型操作 | - |
| `VectorExtension` | 向量转换 | `ToVector2()`、`ToVector3()` |

### 3.7 Utils - 工具函数库

| 类别 | 工具类 |
|------|--------|
| 数据处理 | `BeanUtil`、`JsonUtil`、`ExcelUtil`、`TypeConversionUtil` |
| 资源加载 | `LoadAddressablesUtil`、`LoadAssetUtil`、`LoadResourcesUtil`、`LoadWWWUtil` |
| 数学/随机 | `MathUtil`、`RandomUtil`、`FastNoise`、`SimplexNoiseUtil` |
| 图形渲染 | `TextureUtil`、`MeshUtil`、`UGUIUtil` |
| 游戏通用 | `GameUtil`、`SceneUtil`、`RayUtil`、`VectorUtil`、`CptUtil` |
| 系统工具 | `FileUtil`、`LogUtil`、`SystemUtil`、`TimeUtil`、`UnitUtil` |
| 反射/类型 | `ReflexUtil`、`ClassUtil`、`CheckUtil` |
| 动画 | `DGEaseUtil`（DOTween Ease 工具） |

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
GameConfigController  -->  控制器
       |
GameConfigModel       -->  模型
       |
GameConfigService     -->  服务层
       |
IGameConfigView       -->  视图接口
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

### 4.2 Enums - 游戏枚举

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

#### Launcher - 启动器

```
BaseLauncher (启动器基类)
    ├── LauncherGame (游戏启动器)
    └── LauncherTest (测试启动器)
```

#### Base - 游戏逻辑基类

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

#### Logic - 游戏逻辑实现

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

#### Fight - 战斗系统

```
Fight/
├── FightCreatureEntity.cs      # 战斗生物实体
├── FightPrefabEntity.cs        # 战斗预制体实体
└── AttackMode/                 # 攻击模式（策略模式）
    └── BaseAttackMode.cs       # 攻击模式基类
```

**攻击模式类型：**

| 类型 | 类名 |
|------|------|
| 近战 | `AttackModeMelee`、`AttackModeMeleeArea` |
| 远程 | `AttackModeRanged`、`AttackModeRangedArc`、`AttackModeRangedArea`、`AttackModeRangedTracking` |
| 特殊 | `AttackModeExplosion`、`AttackModeFallupon`、`AttackModeLure`、`AttackModeOverlap` |
| 恢复 | `AttackModeRegain`、`AttackModeRegainHP`、`AttackModeRegainDR` |

#### Buff - BUFF 系统

```
Buff/
├── BuffEntity/                 # BUFF 实体
│   ├── BuffBaseEntity.cs       # BUFF 实体基类
│   ├── Attribute/              # 属性类 BUFF（HP/DR 变化）
│   ├── Conditional/            # 条件类 BUFF（攻击/死亡触发）
│   ├── Instant/                # 瞬时类 BUFF（立即生效）
│   └── Periodic/               # 周期性 BUFF（定时触发）
└── BuffPre/                    # BUFF 前置条件
    └── BuffBasePreEntity.cs    # 前置条件基类
```

**BUFF 实体类型：**

| 类型 | 类名 | 说明 |
|------|------|------|
| 属性变化 | `BuffEntityBaseHPChange` | HP 变化 |
| 属性变化 | `BuffEntityBaseDRChange` | 防御变化 |
| 条件触发 | `BuffEntityConditionalAttack` | 攻击时触发 |
| 条件触发 | `BuffEntityConditionalDead` | 死亡时触发 |
| 死亡特殊 | `BuffEntityConditionalDeadAttack` | 死亡时发动攻击 |
| 死亡特殊 | `BuffEntityConditionalDeadCreateCrystal` | 死亡时生成水晶 |
| 周期性 | `BuffEntityPeriodicAttackAgain` | 周期性再次攻击 |

#### DoomCouncil - 终焉议会系统

| 类名 | 功能 |
|------|------|
| `DoomCouncilBaseEntity` | 终焉议会实体基类 |
| `DoomCouncilEntityMoreCrystal` | 更多水晶 |
| `DoomCouncilEntityMoreExp` | 更多经验 |
| `DoomCouncilEntityReincarnation` | 转生 |
| `DoomCouncilEntityRename` | 改名 |

### 4.4 Bean - 游戏数据模型

#### Game Bean

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

#### MVC Bean

| Bean 类 | 功能 |
|---------|------|
| `UserDataBean` | 用户数据 |
| `CreatureInfoBean` | 生物信息 |
| `BuffInfoBean` | BUFF 信息 |
| `ItemsInfoBean` | 道具信息 |
| `FightSceneBean` | 战斗场景信息 |
| `ResearchInfoBean` | 研究信息 |

#### UI Bean

| Bean 类 | 功能 |
|---------|------|
| `DialogBossShowBean` | BOSS 展示对话框 |
| `DialogRenameBean` | 改名对话框 |
| `DialogSelectBean` | 选择对话框 |
| `DialogSelectCreatureBean` | 生物选择对话框 |
| `DialogSelectItemBean` | 道具选择对话框 |

### 4.5 Component - 游戏组件

#### Manager

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

#### Handler

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

#### Game - 游戏场景与控制

```
Game/
├── Control/
│   ├── ControlForGameBase.cs        # 基础游戏控制
│   └── ControlForGameFight.cs       # 战斗游戏控制
├── Scene/
│   ├── ScenePrefabBase.cs           # 场景预制体基类
│   ├── ScenePrefabForBase.cs        # 基地场景
│   ├── ScenePrefabForDoomCouncil.cs # 终焉议会场景
│   └── ScenePrefabForRewardSelect.cs# 奖励选择场景
└── RewardSelectBoxComponent.cs      # 奖励选择框组件
```

#### UI

```
UI/
├── Common/           # 通用 UI 组件
│   ├── AbyssalBlessing/    # 深渊馈赠
│   ├── Backpack/           # 背包
│   ├── BaseInfo/           # 基础信息
│   ├── Buff/               # BUFF 显示
│   ├── CreatureCard/       # 生物卡片
│   ├── ItemEquip/          # 道具装备
│   └── ...
├── Dialog/           # 对话框 UI
│   ├── UIDialogBossShow    # BOSS 展示
│   ├── UIDialogNormal      # 普通对话框
│   ├── UIDialogSelect      # 选择对话框
│   └── ...
├── Game/             # 游戏 UI
│   ├── BaseCore/           # 核心基地
│   ├── BaseMain/           # 主基地
│   ├── FightMain/          # 战斗主界面
│   ├── DoomCouncil/        # 终焉议会
│   ├── GashaponMachine/    # 扭蛋机
│   └── ...
├── Popup/            # 弹窗 UI
│   ├── UIPopupCreatureCardDetails     # 生物卡片详情
│   ├── UIPopupDoomCouncilMainDetails  # 终焉议会详情
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
    ├── AICreatureEntity.cs               # 生物 AI 实体
    ├── AIIntentCreatureAttack.cs         # 攻击意图
    ├── AIIntentCreatureDead.cs           # 死亡意图
    ├── FightAttackCreature/              # 进攻生物 AI
    │   ├── AIAttackCreatureEntity
    │   ├── AIIntentAttackCreatureAttack
    │   ├── AIIntentAttackCreatureIdle
    │   ├── AIIntentAttackCreatureMove
    │   └── ...
    ├── FightDefenseCreature/             # 防守生物 AI
    │   ├── AIDefenseCreatureEntity
    │   ├── AIIntentDefenseCreatureAttack
    │   ├── AIIntentDefenseCreatureDefend
    │   └── ...
    └── FightDefenseCoreCreature/         # 核心生物 AI
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

## 五、技术要点

### 5.1 资源加载

| 方式 | 工具类 | 说明 |
|------|--------|------|
| Addressables | `LoadAddressablesUtil` | 异步资源加载，支持缓存 |
| Resources | `LoadResourcesUtil` | 同步资源加载 |
| AssetBundle | `LoadAssetUtil` | 资源包加载 |
| WWW | `LoadWWWUtil` | 网络资源加载 |

**缓存机制：** Manager 中维护 `Dictionary<string, T>` 资源字典避免重复加载，SpriteAtlas 懒加载。

### 5.2 UI 系统

**UI 层级：**

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

**自动链接 UI：** 通过反射自动链接 `ui_` 前缀的控件。

```csharp
public void AutoLinkUI()
{
    ReflexUtil.AutoLinkDataForChild(this, "ui_");
}
```

**UI 动画：** 集成 DOTween，支持 `DOFade()`、`DOAnchorPos()` 等补间动画，使用 `Ease.OutExpo`、`Ease.InExpo` 等缓动函数。

### 5.3 战斗系统

- **实体组件：** `FightCreatureEntity`（战斗生物）、`FightPrefabEntity`（战斗预制体）
- **攻击模式：** 基于策略模式，近战/远程/特殊/恢复四大类
- **BUFF 系统：** 属性/条件/瞬时/周期性四种���型
- **AI 状态机：** 生物行为由状态机控制（Idle -> Move -> Attack -> Dead）

### 5.4 数据持久化

| 方式 | 工具 | 适用场景 |
|------|------|----------|
| JSON | `JsonUtil`（封装 Newtonsoft.Json） | 复杂数据结构 |
| PlayerPrefs | Unity 内置 | 简单键值对 |
| SQLite | `BaseSystem/Sqlite` | 大量结构化数据 |

---

*文档结束 - 架构设计详见 [ProjectFrame.md](ProjectFrame.md)*
