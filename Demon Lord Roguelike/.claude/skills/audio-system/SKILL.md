---
name: audio-system
description: Demon Lord Roguelike 游戏的音频(Audio)系统开发指南。使用此SKILL当需要创建或修改音效播放、背景音乐(单曲/列表循环)、环境音、音量管理、按钮点击音效、音频资源加载缓存、音频配置表(excel_audio_info/AudioInfo)等，包括 AudioHandler/AudioManager(框架层+游戏层 partial 配对)、PlaySound/PlayMusicForLoop/PlayMusicListForLoop/PlayEnvironment、PauseMusic/RestoreMusic/StopMusic/StopEnvironment、AudioInfoBean(audio_type 0音效/1音乐/2环境音)、AuidoTypeEnum、三条 AudioSource(Music/Sound/Environment)、GameConfigBean 音量(musicVolume/soundVolume/environmentVolume)、UIGameSettingForAudio 音量设置、ButtonAudio/AudioView UI 音效组件、AddOnClickAction 通用 UI 点击音效等。
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/AudioHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/AudioManager.cs
  - Assets/Scripts/Component/Handler/AudioHandler.cs
  - Assets/Scripts/Component/Manager/AudioManager.cs
  - Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBean.cs
  - Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBeanPartial.cs
  - Assets/FrameWork/Scripts/Component/UI/AudioView.cs
  - Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs
  - Assets/Scripts/Component/UI/Game/GameSetting/UIGameSettingForAudio.cs
  - Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs
---

# 音频系统 (Audio System) 开发指南

## 核心概念

音频逻辑通过 `BaseHandler<AudioHandler, AudioManager>` 配对模式组织，框架层与游戏层各有一个 `partial` 文件。
所有音频按 **三类** 区分，每类对应一条常驻 `AudioSource` 与一个资源缓存字典：

```
AudioHandler   - 音频逻辑处理器（对外 API，单例 AudioHandler.Instance）
AudioManager   - 音频资源管理器（持有 3 条 AudioSource、AudioListener、3 个 AudioClip 缓存字典）

三类音频（AuidoTypeEnum）：
  Sound       = 0  音效   → audioSourceForSound        （PlayOneShot / PlayClipAtPoint，瞬时）
  Music       = 1  音乐   → audioSourceForMusic        （loop，单曲或列表轮播）
  Environment = 2  环境音 → audioSourceForEnvironment  （loop，常驻氛围音）
```

> `AudioListener` 每帧跟随 `Camera.main` 位置（`AudioHandler.Update`），所以 3D 定位音效（`PlayClipAtPoint`）以摄像机为听者。

### 分层文件速查表

| 文件 | 层 | 职责 |
| --- | --- | --- |
| [Assets/FrameWork/Scripts/Component/Handler/AudioHandler.cs](Assets/FrameWork/Scripts/Component/Handler/AudioHandler.cs) | 框架 | 通用播放 API：`InitAudio`、`PlayMusicForLoop`、`PlayMusicListForLoop`、`PlaySound`、`PlayEnvironment`、暂停/停止/恢复 |
| [Assets/FrameWork/Scripts/Component/Manager/AudioManager.cs](Assets/FrameWork/Scripts/Component/Manager/AudioManager.cs) | 框架 | 3 条 `AudioSource` 懒加载、`AudioListener`、按类型加载并缓存 `AudioClip`（`GetMusicClip`/`GetSoundClip`/`GetEnvironmentClip` → `LoadClipDataByAddressbles`） |
| [Assets/Scripts/Component/Handler/AudioHandler.cs](Assets/Scripts/Component/Handler/AudioHandler.cs) | 游戏 | 业务封装：`PlayMusicForMain`/`PlayMusicForGaming`/`PlayMusicForFight`、`ActionForUIOnClick` 通用 UI 点击音效 |
| [Assets/Scripts/Component/Manager/AudioManager.cs](Assets/Scripts/Component/Manager/AudioManager.cs) | 游戏 | `listCommonUIClick`（触发通用点击音效的 sprite 名集合） |
| [Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBean.cs](Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBean.cs) | 框架 | 自动生成的配置 Bean + `AudioInfoCfg`（`GetItemData(id)`） |
| [Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBeanPartial.cs](Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBeanPartial.cs) | 框架 | 配置 Bean 的扩展方法（手写代码写这里） |
| [Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs](Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs) | 框架 | 按钮自带点击音效组件（挂在 Button 上，直接放 AudioClip） |
| [Assets/FrameWork/Scripts/Component/UI/AudioView.cs](Assets/FrameWork/Scripts/Component/UI/AudioView.cs) | 框架 | 本地 AudioClip 列表按名播放（脱离配置表的就地播放） |
| [Assets/Scripts/Component/UI/Game/GameSetting/UIGameSettingForAudio.cs](Assets/Scripts/Component/UI/Game/GameSetting/UIGameSettingForAudio.cs) | 游戏 | 设置界面音乐/音效音量滑条 |
| [Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs](Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs) | 框架 | `AuidoTypeEnum` 枚举（注意官方拼写为 **Auido**） |

> 提示：`AudioHandler` / `AudioManager` 都是 `partial` 类，框架层放通用播放能力，游戏层放业务封装（按场景选曲、UI 点击音效）。修改时按职责选对应层。

## 音频配置表

- **真实源**：`Assets/Data/Excel/excel_audio_info*.xlsx`（工作表导出为 `Assets/Resources/JsonText/AudioInfo.txt`）。改配置必须改 Excel（见 CLAUDE.md「Excel 是唯一真实源」），仅改 JSON 下次导出会被覆盖。
- **字段**（`AudioInfoBean`）：`id`、`name_res`（资源文件名，不含扩展名）、`remark`（备注）、`audio_type`（0 音效 / 1 音乐 / 2 环境音）。
- **读取**：`AudioInfoCfg.GetItemData(id)` 拿到 Bean，`name_res` 即资源名。

## 音频资源路径

`AudioManager` 按类型从固定 Addressables 地址加载并缓存（同名只加载一次）：

```
音效   Sound       → Assets/LoadResources/Audio/Sound/{name_res}
音乐   Music       → Assets/LoadResources/Audio/Music/{name_res}
环境音 Environment → Assets/LoadResources/Audio/Environment/{name_res}
```

加载走 `LoadAddressablesUtil.LoadAssetAsync<AudioClip>`，结果存入对应 `dicMusicData/dicSoundData/dicEnvironmentData` 缓存。

## 初始化流程

```csharp
// 从存档读取音量，写入 3 条 AudioSource（音量变更后也调用此方法刷新）
AudioHandler.Instance.InitAudio();
```

音量持久化在 `GameConfigBean`（`musicVolume` / `soundVolume` / `environmentVolume`，默认 0.5）。

## 关键 API

### 1. 音效（瞬时，Sound）

```csharp
// 最简：用默认音量(soundVolume)在 Camera.main 位置播放
AudioHandler.Instance.PlaySound(soundId);

// 指定 3D 播放位置（PlayClipAtPoint）
AudioHandler.Instance.PlaySound(soundId, worldPosition);

// 完整：自定义音量、可指定 AudioSource（传 null 用 PlayClipAtPoint 在指定点播放）
AudioHandler.Instance.PlaySound(soundId, worldPosition, volumeScale, audioSource);
```
- `soundId == 0` 或 `volumeScale == 0` 直接跳过。
- **防抖**：同一 `soundId` 在 0.1s 内重复请求会被忽略（`timeUpdateForRepeatPlay` + `lastPlaySoundId`），避免密集命中时音效炸裂。

### 2. 音乐（循环，Music）

```csharp
// 单曲循环（loop=true）
AudioHandler.Instance.PlayMusicForLoop(musicId);
AudioHandler.Instance.PlayMusicForLoop(musicId, volumeScale);

// 列表随机轮播：每首播完用协程切下一首随机曲（非 loop，靠协程衔接）
AudioHandler.Instance.PlayMusicListForLoop(listMusicIds);
```
游戏层已封装按场景选曲（[Assets/Scripts/Component/Handler/AudioHandler.cs](Assets/Scripts/Component/Handler/AudioHandler.cs)）：
```csharp
AudioHandler.Instance.PlayMusicForMain();    // 主界面（单曲 1200001）
AudioHandler.Instance.PlayMusicForFight();   // 战斗（1000001~1000006 轮播）
AudioHandler.Instance.PlayMusicForGaming();  // 基地游戏中（当前留空）
```

### 3. 环境音（循环，Environment）

```csharp
AudioHandler.Instance.PlayEnvironment(environmentId);
AudioHandler.Instance.PlayEnvironment(environmentId, volumeScale);
```

### 4. 暂停 / 恢复 / 停止

```csharp
AudioHandler.Instance.PauseMusic();        // 暂停音乐（并停掉列表轮播协程）
AudioHandler.Instance.RestoreMusic();      // 恢复音乐
AudioHandler.Instance.StopMusic();         // 停止音乐并清 clip
AudioHandler.Instance.StopMusicListLoop(); // 仅停列表轮播协程

AudioHandler.Instance.PauseEnvironment();
AudioHandler.Instance.RestoreEnvironment();
AudioHandler.Instance.StopEnvironment();
```

### 5. UI 点击音效

两种机制并存：

- **通用点击音效**（自动）：`AudioHandler.Awake` 里 `UIHandler.Instance.AddOnClickAction(ActionForUIOnClick)`。点击带 `Button` 的 UI 时，若 image sprite 名在 `AudioManager.listCommonUIClick` 集合中则播 `PlaySound(3)`；image 名为 `ViewExit` 的退出按钮播 `PlaySound(4)`。新增一类通用音效：把对应 sprite 名加进游戏层 `AudioManager.listCommonUIClick`。
- **按钮独立音效**（手动挂载）：在 Button 上挂 [ButtonAudio](Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs) 组件，填 `clickClip` 列表（随机取一个），用 `soundVolume` 在按钮位置播放。**不读配置表**，直接用本地 AudioClip。

### 6. 音量设置

设置界面 [UIGameSettingForAudio](Assets/Scripts/Component/UI/Game/GameSetting/UIGameSettingForAudio.cs) 用滑条改 `gameConfig.musicVolume`/`soundVolume`，每次变更调用 `AudioHandler.Instance.InitAudio()` 即时生效（环境音音量同字段，UI 当前只暴露音乐/音效两条）。

## 常见任务流程

### 新增一个音效/音乐/环境音
1. 把音频文件放到对应目录 `Assets/LoadResources/Audio/{Sound|Music|Environment}/`，确保被 Addressables 标记，地址与路径一致。
2. 在 `excel_audio_info` Excel 新增一行（按 id 升序插入，见 [feedback_excel_id_sorted_insert]）：`id` / `name_res`（= 文件名）/ `remark` / `audio_type`。
3. 在 Unity 编辑器用配置导出工具重新生成 `AudioInfo.txt`（仅改 Excel 时须提醒用户导出）。
4. 代码里用对应 `Play*` API 按 id 播放。

### 在战斗/业务里触发音效
直接 `AudioHandler.Instance.PlaySound(id, worldPosition)`，例如命中/落空音效见 [FightCreatureEntity.cs](Assets/Scripts/Game/Fight/FightCreatureEntity.cs)（`soundHitId` / `soundMissId`）。

## 约束与注意事项

- **音频播放统一走 `AudioHandler`**，不要在业务代码里直接 new `AudioSource` 或散落调 `AudioSource.PlayClipAtPoint`（脱离配置/缓存的本地播放才用 `AudioView`/`ButtonAudio`）。
- **配置表是唯一真实源**：音频 id ↔ 资源名映射改在 Excel，别只改 `AudioInfo.txt`（下次导出会被覆盖）。
- **资源缓存**：clip 按 `{路径}/{name_res}` 缓存，重复播放不重复加载；新增音频确保 Addressables 地址与三类固定路径前缀一致，否则加载失败（日志 `没有名字为:xxx 的音效资源`）。
- **枚举拼写**：类型枚举为 `AuidoTypeEnum`（源码即写作 **Auido**，非 Audio），引用时勿"纠正"拼写。
- **音效防抖**：0.1s 内同 id 不重复播放是刻意设计，需要叠加同音效时注意此限制。
- **`VolumeHandler` 不是音量管理**：项目里的 `VolumeHandler`/`VolumeManager` 是 URP 后处理（景深 DepthOfField）控制器，与音频音量无关。音频音量在 `GameConfigBean` + `UIGameSettingForAudio`。
- **Bean 规则**：`AudioInfoBean.cs` 自动生成，扩展写在 [AudioInfoBeanPartial.cs](Assets/FrameWork/Scripts/Bean/MVC/AudioInfoBeanPartial.cs)。
- **代码规范**：所有方法/属性加 `/// <summary>` XML 注释，用 `#region`/`#endregion` 按「音乐播放/音效播放/环境音效/停止相关」分区（现有文件已分区）。
- **配套 agent**：复杂改动可委派 `system-audio` 子代理；改了本 skill `watched_files` 命中的代码须同步更新本文档（见 [feedback_agent_skill_sync]）。
