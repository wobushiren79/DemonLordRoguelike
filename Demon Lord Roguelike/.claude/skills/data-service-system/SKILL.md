---
name: data-service-system
description: Demon Lord Roguelike 游戏的数据服务系统开发指南。使用此SKILL当需要创建或修改数据持久化、JSON读写、用户存档、配置数据管理、SQLite操作等，包括BaseDataService<T>泛型基类、UserDataService、GameConfigBean、数据存储(DataStorage)、自动备份机制等。
watched_files:
  - Assets/FrameWork/Scripts/MVC/BaseDataService.cs
  - Assets/Scripts/MVC/Service/UserDataService.cs
  - Assets/FrameWork/Scripts/DataStorage/
  - Assets/FrameWork/Scripts/BaseSystem/Sqlite/
  - Assets/FrameWork/Scripts/Utils/JsonUtil.cs
  - Assets/FrameWork/Scripts/Utils/ExcelUtil.cs
  - Assets/FrameWork/Scripts/Component/Manager/GameDataManager.cs
  - Assets/FrameWork/Scripts/Component/Handler/GameDataHandler.cs
  - Assets/Scripts/Bean/MVC/UserDataBean.cs
---

# 数据服务系统开发指南

## 核心概念

项目采用 **BaseDataService\<T\>** 泛型数据服务模式，Manager 直接操作 Service 进行数据读写，不再使用传统 MVC 的 Model/Controller/View 分层。

### 数据服务体系架构

```
GameDataManager (MonoBehaviour)
    │  资源加载、缓存管理
    │
    ├── BaseDataService<GameConfigBean>  ---> JSON 文件读写
    │   └── GameConfigBean: 游戏全局配置（语言、音量、设置等）
    │
    ├── BaseDataService<ModIdMapBean>    ---> JSON 文件读写
    │   └── ModIdMapBean: Mod 名称到 ID 的映射
    │
    └── UserDataService                  ---> JSON 文件读写 + 自动备份
        └── UserDataBean: 用户存档（生物、道具、水晶、声望等）

持久化方式:
    JSON (Newtonsoft.Json)  → 复杂数据结构（存档、配置）
    PlayerPrefs             → 简单键值对
    SQLite                  → 大量结构化数据
    Excel (EPPlus)          → 配置表导入导出
```

---

## BaseDataService\<T\> 泛型基类

**文件**: `Assets/FrameWork/Scripts/MVC/BaseDataService.cs`

### 核心方法

```csharp
public class BaseDataService<T> where T : class, new()
{
    protected string fileName;           // JSON 文件名（不含扩展名）
    protected string fileDirectory;      // 文件目录路径
    protected T data;                    // 数据实例

    // 初始化服务（指定文件名和目录）
    public void InitData(string fileName, string fileDirectory);

    // 获取数据
    public T GetData();

    // 设置数据
    public void SetData(T data);

    // 从 JSON 文件加载数据
    public T LoadData();

    // 保存数据到 JSON 文件
    public void SaveData();

    // 保存数据（异步）
    public async Task SaveDataAsync();

    // 检查数据文件是否存在
    public bool HasDataFile();
}
```

### 创建新的数据服务

```csharp
// 1. 定义数据 Bean
[Serializable]
public class MyFeatureDataBean
{
    public int version = 1;
    public Dictionary<string, int> settings = new Dictionary<string, int>();
    public List<string> records = new List<string>();
}

// 2. 创建数据服务
public class MyFeatureDataService : BaseDataService<MyFeatureDataBean>
{
    public MyFeatureDataService()
    {
        InitData("MyFeatureData", Application.persistentDataPath);
    }

    // 自定义业务方法
    public void AddRecord(string record)
    {
        var data = GetData();
        data.records.Add(record);
        SaveData();
    }

    public void SetSetting(string key, int value)
    {
        var data = GetData();
        data.settings[key] = value;
        SaveData();
    }
}
```

// 3. 在 Manager 中使用
```csharp
public class MyFeatureManager : BaseManager
{
    private MyFeatureDataService dataService;

    public override void Awake()
    {
        base.Awake();
        dataService = new MyFeatureDataService();
    }

    public MyFeatureDataBean GetFeatureData()
    {
        return dataService.GetData();
    }

    public void SaveFeatureData()
    {
        dataService.SaveData();
    }
}
```

---

## UserDataService 详细说明

**文件**: `Assets/Scripts/MVC/Service/UserDataService.cs`

### 核心特性

- 继承 `BaseDataService<UserDataBean>`
- 自动备份机制：保存时自动创建备份文件
- 存档损坏时自动恢复

### UserDataBean 结构

```csharp
public class UserDataBean
{
    // 基础信息
    public string userName;                          // 用户名
    public int userLevel;                            // 用户等级
    public long crystal;                             // 魔晶数量
    public int reputation;                           // 声望
    public long exp;                                 // 经验值
    
    // 生物数据
    public UserBackpackCreatureBean userBackpackCreatureData; // 背包生物容器([JsonIgnore]拆分→UserBackpackCreature_{slot})；列表在 .listBackpackCreature
    public Dictionary<int, List<string>> dicLineupCreature;   // 阵容（阵容ID -> 生物UUID列表）
    
    // 道具数据
    public UserBackpackItemsBean userBackpackItemsData;  // 背包道具容器([JsonIgnore]拆分→UserBackpackItem_{slot})；列表在 .listBackpackItems
    
    // 解锁数据
    public UserUnlockBean userUnlockData;             // 解锁内容([JsonIgnore]拆分→UserUnlock_{slot})
    public UserAscendBean userAscendData;             // 升阶数据
    public UserLimmitBean userLimmitData;              // 限制数据
    
    // 临时数据
    public UserTempBean userTempData;                 // 临时数据（当前战斗等）
}
```

### 读写用户数据

```csharp
// 获取用户数据
UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();

// 保存用户数据（有自动备份）
GameDataHandler.Instance.manager.SaveUserData();

// 修改属性示例
userData.AddCrystal(1000);              // 增加魔晶
userData.AddReputation(100);            // 增加声望
userData.AddBackpackItem(newItem);      // 添加道具
userData.RemoveBackpackItem(item);      // 移除道具
userData.AddBackpackCreature(creature); // 添加生物
userData.RemoveBackpackCreature(uuid);  // 移除生物
userData.AddLineupCreature(lineupId, uuid); // 添加到阵容
userData.MoveLineupCreature(lineupId, uuid, posIndex); // 阵容内拖拽换位(重排顺序)

// 背包列表已包裹进容器Bean，直接读取列表须经访问器(增删仍走上面的方法)
List<ItemBean> items = userData.GetUserBackpackItemsData().listBackpackItems;
List<CreatureBean> creatures = userData.GetUserBackpackCreatureData().listBackpackCreature;
```

### 存档备份机制

```csharp
// UserDataService 自动处理
// 保存流程:
// 1. 将当前数据写入 UserData.json
// 2. 复制上一份到 UserData_backup.json
// 3. 如果写入失败，尝试从备份恢复
```

### 拆分存档(解锁/成就/背包独立文件)

`UserUnlockBean` / `UserAchievementBean` / `UserBackpackItemsBean`(包裹背包道具列表) / `UserBackpackCreatureBean`(包裹背包生物列表) 数据量大，已从 `UserData_{slot}` 主存档**拆分为同槽目录下的独立文件**，避免主存档膨胀。背包两个列表原先是 `UserDataBean` 上裸露的 `List<>` 字段，现已各自包进独立的容器 Bean(`Assets/Scripts/Bean/Game/UserBackpack*Bean.cs`)再拆分，与解锁/成就的 Bean 包裹方式一致：

```
{persistentDataPath}/UserData_{slot}/
├── UserData_{slot}                 # 主存档(含备份 _Backups_0/1/2)
├── UserUnlock_{slot}               # 解锁数据(独立, UserUnlockBean)
├── UserAchievement_{slot}          # 成就&统计数据(独立, UserAchievementBean)
├── UserBackpackItem_{slot}         # 背包道具(独立, UserBackpackItemsBean{ listBackpackItems })
└── UserBackpackCreature_{slot}     # 背包生物(独立, UserBackpackCreatureBean{ listBackpackCreature })
```

- `UserDataBean.userUnlockData` / `userAchievementData` / `userBackpackItemsData` / `userBackpackCreatureData` 均为包裹型 Bean 字段并标注 `[Newtonsoft.Json.JsonIgnore]`，**不随主存档序列化**；统一用懒初始化取数器访问：`GetUserUnlockData()` / `GetUserAchievementData()` / `GetUserBackpackItemsData()` / `GetUserBackpackCreatureData()`。背包列表经容器取出：`GetUserBackpackItemsData().listBackpackItems` / `GetUserBackpackCreatureData().listBackpackCreature`。
- **背包增删/查询方法仍在 `UserDataBean` 上**（`AddBackpackItem`/`RemoveBackpackItem`/`AddBackpackCreature`/`RemoveBackpackCreature`/`GetBackpackCreature`），内部改为操作容器 Bean 的列表——因为它们与魔晶(`AddBackpackItemForSpecial`→`AddCrystal`)、阵容(`RemoveBackpackCreature`→`RemoveLineupCreature`)、事件耦合，故由聚合根 `UserDataBean` 编排，容器 Bean 仅作纯数据存储。调用方法的外部代码无需改动，只有**直接读列表**的地方改走访问器。
- **拆分读写全部封装在 `UserDataService` 内部**（不另建子类服务）：`Save` 存完主存档后用即建的 `BaseDataService<UserUnlockBean>` / `<UserAchievementBean>` / `<UserBackpackItemsBean>` / `<UserBackpackCreatureBean>` 实例写独立文件；`Load` 读主存档后注入拆分数据；`Delete` 一并删除。`GameDataManager` 仍只调 `userDataService.Save/Load/Delete`，对拆分无感知。
- 关键技巧：`BaseDataService<T>` 是可实例化的具体类（约束 `where T : class, new()`），`StoragePath` public、`FileName` 由构造函数传入，故 UserDataService 用 `new BaseDataService<T>(fileName){ StoragePath = ... }` 即可复用泛型读写，无需为每个类型建子类。
- **不迁移旧存档**：旧版主存档内嵌的 unlock/achievement/背包 在加 `JsonIgnore` 后被忽略，独立文件不存在时注入空数据（视为新开始）。
- 独立文件**无备份**：`UserDataService` 的"使用备份"回滚只还原主存档，不联动拆分文件，存在轻微不同步（按需自行扩展）。
- **存档编辑器联动**：`SaveDataEditorWindow` 也按拆分维度独立加载/展示/回写这些文件（解锁/成就/背包道具/背包生物各一棵 JToken 树），保存时分别反序列化回注 `data`，避免覆盖丢失。新增拆分字段时务必同步更新该编辑器。

---

## GameConfigBean - 游戏配置

```csharp
public class GameConfigBean
{
    public LanguageEnum language;         // 当前语言
    public float musicVolume;             // 音乐音量 (0-1)
    public float sfxVolume;               // 音效音量 (0-1)
    public bool isFullscreen;             // 是否全屏
    public int resolutionIndex;           // 分辨率索引
    // ... 其他全局配置
}

// 使用
GameConfigBean config = GameDataHandler.Instance.manager.GetGameConfig();
config.language = LanguageEnum.cn;
config.musicVolume = 0.8f;
GameDataHandler.Instance.manager.SaveGameConfig();
```

---

## JSON 工具类 (JsonUtil)

**文件**: `Assets/FrameWork/Scripts/Utils/JsonUtil.cs`

```csharp
// 序列化
string json = JsonUtil.ToJson(myObject);

// 反序列化
MyClass obj = JsonUtil.FromJson<MyClass>(jsonString);

// 从文件读取
MyClass data = JsonUtil.LoadFromFile<MyClass>(filePath);

// 保存到文件
JsonUtil.SaveToFile(filePath, myObject);

// 使用 Unity 的 Newtonsoft.Json 序列化器
UnityNewtonsoftJsonSerializer.Serialize(obj);
UnityNewtonsoftJsonSerializer.Deserialize<T>(json);
```

---

## SQLite 操作

**文件**: `Assets/FrameWork/Scripts/BaseSystem/Sqlite/`

### SQliteHandle 核心方法

```csharp
// 初始化数据库
SQliteHandle handle = new SQliteHandle(dbPath);

// 执行查询
List<T> results = handle.Query<T>("SELECT * FROM TableName WHERE id = ?", id);

// 执行非查询
int affected = handle.Execute("INSERT INTO TableName VALUES (?, ?)", value1, value2);

// 批量操作
handle.BeginTransaction();
// ... 多次 Execute ...
handle.Commit();
```

### 使用 SQLiteHelper

```csharp
// 创建表
SQLiteHelper.CreateTable<T>(dbPath);

// 插入数据
SQLiteHelper.Insert(dbPath, myObject);

// 批量插入
SQLiteHelper.InsertAll(dbPath, listObjects);

// 更新数据
SQLiteHelper.Update(dbPath, myObject);

// 查询
List<T> items = SQLiteHelper.Query<T>(dbPath, "WHERE column = ?", value);
```

---

## Excel 配置处理 (ExcelUtil)

**文件**: `Assets/FrameWork/Scripts/Utils/ExcelUtil.cs`

### 读取 Excel 配置

```csharp
// 从 Excel 读取配置数据（Editor 环境）
var items = ExcelUtil.GetExcelDataList<ItemsInfoBean>(
    "Assets/Data/Excel/items.xlsx", 
    "ItemsInfo"
);

// 导出 Excel 数据为 JSON（使用编辑器窗口）
// 菜单: Custom/工具弹窗/Excel编辑器
```

### Excel 配置表结构

```
Assets/Data/Excel/
├── items.xlsx               # 道具配置
├── creatures.xlsx            # 生物配置
├── buffs.xlsx                # BUFF 配置
├── attacks.xlsx              # 攻击模式配置
└── ...
```

### 配置表字段命名规范

```
Excel列名 -> Bean字段名
- id       -> id (long)
- name     -> name (long, 文本表ID)
- remark   -> remark (string)
- ... 其他业务字段
```

---

## 常用代码模板

### 新增持久化数据模块

```csharp
// 1. 定义 Bean
[Serializable]
public class MySaveData
{
    public int version = 1;
    public long lastSaveTime;
    public List<MyRecord> records = new List<MyRecord>();
}

[Serializable]
public class MyRecord
{
    public string id;
    public long timestamp;
    public string data;
}

// 2. 创建 Service
public class MySaveDataService : BaseDataService<MySaveData>
{
    public MySaveDataService()
    {
        InitData("MySaveData", Application.persistentDataPath);
    }
}

// 3. 在 GameDataManager 中集成
// GameDataManager 中添加:
private MySaveDataService mySaveDataService;

public MySaveData GetMySaveData()
{
    if (mySaveDataService == null)
        mySaveDataService = new MySaveDataService();
    return mySaveDataService.GetData();
}

public void SaveMySaveData()
{
    mySaveDataService?.SaveData();
}
```

### 数据读写完整示例

```csharp
public class DataExample : BaseMonoBehaviour
{
    private void LoadData()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        
        // 读取数据
        long crystal = userData.crystal;
        int level = userData.userLevel;
        List<CreatureBean> creatures = userData.listCreature;
        
        // 修改数据
        userData.AddCrystal(100);
        userData.AddBackpackCreature(newCreature);
        
        // 保存数据
        GameDataHandler.Instance.manager.SaveUserData();
    }
}
```

### 使用 PlayerPrefs（简单设置）

```csharp
// 简单键值对的存储
PlayerPrefs.SetInt("TutorialComplete", 1);
PlayerPrefs.SetFloat("MusicVolume", 0.8f);
PlayerPrefs.SetString("LastLoginTime", DateTime.Now.ToString());

// 读取（带默认值）
int tutorialComplete = PlayerPrefs.GetInt("TutorialComplete", 0);
float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

// 保存到磁盘
PlayerPrefs.Save();
```

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 数据服务基类 | `Assets/FrameWork/Scripts/MVC/BaseDataService.cs` |
| 用户数据服务(含解锁/成就拆分存档读写) | `Assets/Scripts/MVC/Service/UserDataService.cs` |
| 游戏数据管理器 | `Assets/FrameWork/Scripts/Component/Manager/GameDataManager.cs` |
| 游戏数据处理器 | `Assets/FrameWork/Scripts/Component/Handler/GameDataHandler.cs` |
| 用户数据Bean | `Assets/Scripts/Bean/MVC/UserDataBean.cs` |
| 数据读取基类 | `Assets/FrameWork/Scripts/DataStorage/BaseDataRead.cs` |
| 数据存储基类 | `Assets/FrameWork/Scripts/DataStorage/BaseDataStorage.cs` |
| JSON工具 | `Assets/FrameWork/Scripts/Utils/JsonUtil.cs` |
| Excel工具 | `Assets/FrameWork/Scripts/Utils/ExcelUtil.cs` |
| SQLite操作 | `Assets/FrameWork/Scripts/BaseSystem/Sqlite/SQliteHandle.cs` |
| SQLite辅助 | `Assets/FrameWork/Scripts/BaseSystem/Sqlite/SQLiteHelper.cs` |
| SQLite初始化 | `Assets/FrameWork/Scripts/BaseSystem/Sqlite/SQliteInit.cs` |
| Excel编辑器窗口 | `Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs` |
| MVC编辑器窗口 | `Assets/FrameWork/Editor/Base/Window/MVCEditorWindow.cs` |

---

## 注意事项

1. **线程安全**: JSON 文件读写使用了 `async/await` 支持异步操作，非主线程读写时注意线程安全。
2. **自动备份**: UserDataService 自动维护备份文件，但极端情况（磁盘满、权限不足）会导致保存失败。
3. **数据版本**: Bean 中的 `version` 字段可用于数据迁移，升级时处理旧格式兼容。
4. **文件路径**: 编辑器环境使用 Application.dataPath 同级目录，打包后使用 Application.persistentDataPath。
5. **序列化**: 使用 Newtonsoft.Json 而非 Unity 内置 JsonUtility，支持更丰富的序列化特性（字典、私有字段等）。
