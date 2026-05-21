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
    public List<CreatureBean> listCreature;          // 所有生物
    public Dictionary<string, List<string>> dicLineup; // 阵容（阵容ID -> 生物UUID列表）
    
    // 道具数据
    public List<ItemBean> listBackpackItems;          // 背包道具
    
    // 解锁数据
    public UserUnlockBean userUnlockData;             // 解锁内容
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
```

### 存档备份机制

```csharp
// UserDataService 自动处理
// 保存流程:
// 1. 将当前数据写入 UserData.json
// 2. 复制上一份到 UserData_backup.json
// 3. 如果写入失败，尝试从备份恢复
```

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
| 用户数据服务 | `Assets/Scripts/MVC/Service/UserDataService.cs` |
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
