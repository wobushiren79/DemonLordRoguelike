---
name: system-audio
description: 音频系统开发：AudioHandler/AudioManager、音效播放、背景音乐、音量管理、ButtonAudio。
tools: Read, Write, Edit, Glob, Grep, Bash
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
  - Assets/Scripts/Enums/AudioEnum.cs
---

# 音频系统 (Audio System) 开发代理

你负责音频系统的开发。

## 职责范围

### 音频管理
- **AudioHandler** - 音频逻辑处理单例 [FrameWork/Scripts/Component/Handler/AudioHandler.cs](Assets/FrameWork/Scripts/Component/Handler/AudioHandler.cs)
- **AudioManager** - 音频资源与播放管理 [FrameWork/Scripts/Component/Manager/AudioManager.cs](Assets/FrameWork/Scripts/Component/Manager/AudioManager.cs)

### 音量管理
- 音量持久化在 **GameConfigBean**（`musicVolume`/`soundVolume`/`environmentVolume`，默认 0.5）
- 设置界面 **UIGameSettingForAudio** 滑条改音量，变更后调 `AudioHandler.Instance.InitAudio()` 刷新
- 注意：项目里的 `VolumeHandler`/`VolumeManager` 是 URP 后处理（景深）控制器，**与音频音量无关**

### 音频 UI 组件
- **AudioView** - 音频视图控制 [FrameWork/Scripts/Component/UI/AudioView.cs](Assets/FrameWork/Scripts/Component/UI/AudioView.cs)
- **ButtonAudio** - 按钮音效 [FrameWork/Scripts/Component/UI/ButtonAudio.cs](Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs)

### 音频数据
- **AudioBean** - 音频资源数据
- **AudioInfoBean / AudioInfoBeanPartial** - 音频配置信息。字段：`id`/`name_res`/`remark`/`audio_type`/`volume_scale`
- **音效音量缩放 `volume_scale`**（float 列）：0 或空 = 1（不缩放），填值后框架层 `PlaySound` 核心方法自动 `volumeScale *= volume_scale`。固定缩放某个音效优先用此配置列，不要在调用处写死倍率；运行时临时缩放才用 `PlaySoundForVolumeScale`
- **AudioEnum** - 音频枚举 [Scripts/Enums/AudioEnum.cs](Assets/Scripts/Enums/AudioEnum.cs)：枚举值 = `AudioInfo` 配置表 id，枚举名 = `name_res`（去扩展名）。业务调用音频的首选方式，替代裸 int id

## 约束

- 音频播放通过 AudioHandler 统一调用
- **调用统一用 `AudioEnum` 枚举**，禁止裸 int id（如 `PlaySound(15)`）。游戏层 `AudioHandler` partial 已为 `PlaySound`/`PlayMusicForLoop`/`PlayMusicListForLoop`/`PlayEnvironment` 提供枚举重载，内部 `(int)` 转发框架层 int 接口；框架层 int 接口保留给配置驱动的动态 id（如 `soundHitId`/`soundMissId`）
- **新增/删除音频时必须同步维护 `AudioEnum`**（与 `AudioInfo` 配置表一一对应），否则枚举与 id 错位
- 音量设置通过 VolumeHandler 管理
- 按钮音效使用 ButtonAudio 组件
- 音频资源通过 Manager 缓存避免重复加载
