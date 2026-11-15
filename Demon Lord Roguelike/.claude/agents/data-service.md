---
name: data-service
description: 数据服务层开发：BaseDataService<T> 泛型数据服务、UserDataService、JSON/SQLite 数据读写、自动备份。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: data-service-system
watched_files:
  - Assets/FrameWork/Scripts/MVC/
  - Assets/Scripts/MVC/
  - Assets/FrameWork/Scripts/DataStorage/BaseDataStorage.cs
  - Assets/FrameWork/Scripts/BaseSystem/Sqlite/SQliteHandle.cs
---

# 数据服务 (Data Service) 开发代理

你负责 [FrameWork/Scripts/MVC/](Assets/FrameWork/Scripts/MVC/)、[Scripts/MVC/](Assets/Scripts/MVC/) 中的数据服务层开发。

## 职责范围

### 数据服务体系
```
BaseDataService<T>                    # 泛型数据服务基类（JSON 文件读写）
├── BaseDataService<GameConfigBean>   # 游戏配置读写
├── BaseDataService<ModIdMapBean>     # ModID 映射读写
└── UserDataService                   # 用户存档读写（含自动备份）

保留的框架基类（不再使用）：
    BaseMVC / BaseMVCModel / BaseMVCController<M,V>
    BaseMVCService（SQLite）
```

### 数据存储
- **BaseDataRead** - 数据读取基类
- **BaseDataStorage** - 数据存储基类
- **SQliteHandle / SQLiteHelper / SQliteInit** - SQLite 操作

### 持久化方式
| 方式 | 适用场景 |
|------|---------|
| JSON (Newtonsoft.Json) | 复杂数据结构（存档、配置） |
| PlayerPrefs | 简单键值对 |
| SQLite | 大量结构化数据 |
| Excel (EPPlus) | 配置表导入 |

### 关键文件

| 文件 | 路径 |
|------|------|
| BaseDataService | Assets/FrameWork/Scripts/MVC/BaseDataService.cs |
| UserDataService | Assets/Scripts/MVC/Service/UserDataService.cs |
| BaseDataStorage | Assets/FrameWork/Scripts/DataStorage/BaseDataStorage.cs |
| SQliteHandle | Assets/FrameWork/Scripts/BaseSystem/Sqlite/SQliteHandle.cs |

### 用户存档拆分（UserDataService）

孕育整批生成后由 `GameDataManager.TryCommitGashaponBatch` 提交魔晶、抽取计数与背包魔物。`UserDataService.TrySaveGashaponBatch` 原子写入同槽位 `GashaponPending_{slot}` 日志作为提交点，快照包括主档、背包生物和成就。读档或再次保存前幂等回放，三份文件完成后清除日志。日志写入前失败回滚本批内存，保存成功后才通知扣款并进入演出。

主档 `UserData_{slot}` + 同槽目录下独立拆分档（`UserUnlock`/`UserAchievement`/`UserBackpackItem`/`UserBackpackCreature`/`UserRelationship`/`UserStory` 各 `_{slot}`），`Save` 一并写、`Load` 后注入、`Delete` 一并删；拆分档无备份。
`GameDataManager.LoadUserData` 以实际打开的槽位设置 `saveIndex`。日志恢复失败时返回带同槽号的 `isErrorData`，读档列表展示错误且屏蔽创建、继续和删除。已提交的发奖批次在音效或余额刷新异常时只记录日志，仍进入演出。
故事拆分档提供轻量接口：`LoadStoryData()`（只读）/`DeleteStoryData()`（删整个档）/`RemoveStoryData(storyId)`（只移除单个故事已播记录后写回）——供编辑器测试工具单独操作故事已播记录。详见 data-service-system skill。

## 约束

- Manager 直接操作 Service 进行数据读写
- UserDataService 需实现自动备份机制
- JSON 序列化使用 UnityNewtonsoftJsonSerializer
- 数据读写必须考虑线程安全（非 Unity 主线程场景）

## 关联 Skill

详细开发指南请参考: [data-service-system](../skills/data-service-system/SKILL.md)
