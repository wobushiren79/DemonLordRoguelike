---
name: system-steam
description: Steam平台集成开发：Steamworks.NET、Steam排行榜、Steam创意工坊、Steam用户统计。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/SteamHandler.cs
  - Assets/FrameWork/Scripts/BaseSystem/Steam/
  - Assets/FrameWork/Plugins/Steamworks.NET/
  - Assets/FrameWork/Editor/SteamPipe/
---

# Steam 平台集成 (Steam Integration) 开发代理

你负责 Steam 平台集成的开发。

## 职责范围

### SteamPipe 游戏本体发布

框架的 `Assets/FrameWork/Editor/SteamPipe/` 提供「游戏/SteamPipe」窗口，通过官方 SteamCMD 执行授权、Depot 预览及上传。Windows 启动器随框架分发，首次操作时自动部署到本机，也支持选择已有的独立安装。Steam Guard 交互走官方终端，应用默认分支由 Steamworks 后台设置上线。配置与日志的存放约定见 [SteamPipe 发布窗口](../skills/editor-extension-system/SKILL.md#steampipe-发布窗口)。

### Steam 管理
- **SteamHandler** - Steam 逻辑处理 [FrameWork/Scripts/Component/Handler/SteamHandler.cs](Assets/FrameWork/Scripts/Component/Handler/SteamHandler.cs)
- **SteamManager** - Steam 管理器 [FrameWork/Scripts/BaseSystem/Steam/SteamManager.cs](Assets/FrameWork/Scripts/BaseSystem/Steam/SteamManager.cs)

### Steam 功能模块
- **SteamUserStatsHandle** - 用户统计 [FrameWork/Scripts/BaseSystem/Steam/SteamUserStatsHandle.cs](Assets/FrameWork/Scripts/BaseSystem/Steam/SteamUserStatsHandle.cs)
- **SteamWorkshopHandle** - 创意工坊 [FrameWork/Scripts/BaseSystem/Steam/SteamWorkshopHandle.cs](Assets/FrameWork/Scripts/BaseSystem/Steam/SteamWorkshopHandle.cs)

### Steam 接口实现
```
Impl/
├── SteamLeaderboardImpl.cs      # 排行榜实现
├── SteamUserStatsImpl.cs        # 用户统计实现
├── SteamWebImpl.cs              # Web API 实现
├── SteamWorkshopQueryImpl.cs    # 创意工坊查询实现
└── SteamWorkshopUpdateImpl.cs   # 创意工坊更新实现
```

### Steam 接口定义
```
ISteam/
├── ISteamLeaderboard.cs         # 排行榜接口
├── ISteamUserStats.cs           # 用户统计接口
├── ISteamWorkshopQuery.cs       # 创意工坊查询接口
├── ISteamWorkshopUpdate.cs      # 创意工坊更新接口
└── *CallBack.cs                 # 各类回调接口
```

### Steam 数据 Bean
- SteamLeaderboardEntryBean
- SteamWebPlaySummariesBean
- SteamWorkshopQueryInstallInfoBean
- SteamWorkshopUpdateBean

### 跨模块消费点

| 消费方 | 调用 | 用途 |
|--------|------|------|
| 多语言初始化（[LanguageBeanPartial.cs](Assets/FrameWork/Scripts/Bean/MVC/LanguageBeanPartial.cs) `LanguageCfg.GetInitialLanguage()`） | `SteamManager.Initialized` + `SteamApps.GetCurrentGameLanguage()` | 新用户首次启动按 Steam 客户端语言推断默认语言（含 `chinese` → `cn`，否则 `en`），Steam 未连上回退 `cn` |

> 在 Steam 类调用点务必做 `SteamManager.Initialized` 判断 + `try/catch`，保证非 Steam 环境（编辑器、Steam 未启动）也能优雅降级。

### SDK 文件

- 框架子模块在 `Assets/FrameWork/Plugins/Steamworks.NET/` 内置 [Steamworks.NET 2025.165.0](https://github.com/rlabrecque/Steamworks.NET/releases/tag/2025.165.0)，配套 Valve Steamworks SDK 1.65。版本可在插件的 `package.json` 和 `Runtime/Version.cs` 核对。
- `Runtime/` 提供 C# 封装，`Plugins/` 提供各平台的原生库和 Unity 导入设置，`Editor/` 提供安装及构建辅助脚本。升级时从同一官方 Unity 发行包一起替换这三部分，保留框架自己的 `SteamManager`。项目使用这一份内置插件，勿另装同名 UPM 包或示例管理器。
- 项目通过 Unity 的 PluginImporter 限定原生库平台。Windows x86、x64 分别选 `steam_api.dll`、`steam_api64.dll`，Linux 桌面选 x86_64 的 `libsteam_api.so`，macOS 选通用 `steam_api.bundle`，Android 选 `androidarm64/libsteam_api.so`。`linuxarm64/libsteam_api.so` 保留在发行包中，当前目标均禁用。下次升级后按构建目标核对实际导入列表。
- `SteamManager` 通过 `SteamAPI.InitEx` 初始化，失败时将结果和原始原因写入 Console。Steam 客户端在游戏启动前同步当前玩家的成就及统计。只有需要显式请求用户统计时才调用 `SteamUserStatsHandle.UserStatsInit()`。
- 在编辑器调试时，上游安装脚本会在项目根目录缺少 `steam_appid.txt` 时写入测试 App ID `480`。测试本游戏的 Steam 后台数据时，将此本地文件设置为对应的测试 App ID，发布到 Steam 时从 depot 排除该文件。

## 约束

- Steam API 调用通过 Impl 实现，ISteam 定义接口
- 回调处理使用 Steam CallBack 机制
- Steam 功能在非 Steam 平台需有降级处理
- Steam 插件路径不可随意移动
