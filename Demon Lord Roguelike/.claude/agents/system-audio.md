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

### 连续音效 (LoopSound) — 多路并发循环（走路/下雨）
- 框架层通用能力，**非新 `AuidoTypeEnum`/资源目录**：传任意音频 id，按其 `audio_type` 走现有加载路由取 clip，在**循环 AudioSource 池**（`DequeueLoopSource`/`RecycleLoopSource`，`MaxLoopSource=16`）上播放；活跃 `Dictionary<long,LoopSoundEntry>` 按 id 去重、独立起停、多路并发。
- API：`PlayLoopSound`/`StopLoopSound`/`StopAllLoopSound`/`PauseAllLoopSound`/`RestoreAllLoopSound`/`IsLoopSoundPlaying`（框架层 long + 游戏层 `AudioEnum` 重载）。
- **音量跟随 `soundVolume`**（不新增 loopVolume），`InitAudio` 用 `soundVolume × entry.volumeScale` 刷新活跃源。
- **异步竞态防护**：`PlayLoopSound` 先登记 pending entry(token) 占位去重，加载回调校验 token/canceled 才起播；`StopLoopSound` 可取消加载中的 id。防"停不掉的孤儿音源"。
- **生命周期**：音源常驻 DontDestroyOnLoad，`WorldHandler.ClearWorldData` 已挂 `StopAllLoopSound()` 兜底。
- **走路声消费方**：基地自控魔王 [ControlForGameBase.cs](Assets/Scripts/Component/Game/Control/ControlForGameBase.cs) 移动/静止/禁用三处起停，复用一次性音效 `sound_walk_1`。

### 音量管理
- 音量持久化在 **GameConfigBean**（`musicVolume`/`soundVolume`/`environmentVolume`，默认 0.5）
- 设置界面 **UIGameSettingForAudio** 滑条改音量，变更后调 `AudioHandler.Instance.InitAudio()` 刷新
- 注意：项目里的 `VolumeHandler`/`VolumeManager` 是 URP 后处理（景深）控制器，**与音频音量无关**

### 通用 UI 音效（游戏层 AudioHandler.Awake 自动订阅）
- **点击音效**：`UIHandler.AddOnClickAction(ActionForUIOnClick)`，由 `UIHandler.Update()` 的物理鼠标点击 + 射线驱动；sprite 名在 `listCommonUIClick` 集合播 `sound_btn_1`，image 名为 `ViewExit` 播退出音 `sound_btn_31`。**只认鼠标点击，覆盖不到 ESC**。
- **弹窗背景点击关闭音效**：`ActionForUIOnClick` 内判断点击对象 `GetComponentInParent<DialogView>()` 命中的 `ui_Background` 且 `dialogData.isDestroyBG==true` 时播退出音 `sound_btn_31`（判断在 image sprite 空判之前，因背景常无 sprite）；所有弹窗继承 `DialogView` 故一处覆盖全部，`isDestroyBG==false` 的弹窗不误播。游戏层实现，无框架改动。
- **ESC 退出音效**：`AudioHandler.Awake` 订阅 `InputActionUIEnum.ESC` 的 `InputAction.started` → `ActionForUIEscExit`，一处全局补播退出音 `sound_btn_31`，**无条件判断，任意 ESC 按下均播放**。所有 UI 零改动自动覆盖，不要在各 UI 的 ESC/退出分支里手动加播放。

### 音频 UI 组件
- **AudioView** - 音频视图控制 [FrameWork/Scripts/Component/UI/AudioView.cs](Assets/FrameWork/Scripts/Component/UI/AudioView.cs)
- **ButtonAudio** - 按钮音效 [FrameWork/Scripts/Component/UI/ButtonAudio.cs](Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs)

### 音频数据
- **AudioBean** - 音频资源数据
- **AudioInfoBean / AudioInfoBeanPartial** - 音频配置信息。字段：`id`/`name_res`/`remark`/`audio_type`/`volume_scale`
- **音效音量缩放 `volume_scale`**（float 列）：0 或空 = 1（不缩放），填值后框架层 `PlaySound` 核心方法自动 `volumeScale *= volume_scale`。固定缩放某个音效优先用此配置列，不要在调用处写死倍率；运行时临时缩放才用 `PlaySoundForVolumeScale`
- **AudioEnum** - 音频枚举 [Scripts/Enums/AudioEnum.cs](Assets/Scripts/Enums/AudioEnum.cs)：枚举值 = `AudioInfo` 配置表 id，枚举名 = `name_res`（去扩展名），底层类型 `long`。业务调用音频的首选方式，替代裸 id

## 约束

- 音频播放通过 AudioHandler 统一调用
- **调用统一用 `AudioEnum` 枚举**，禁止裸 id（如 `PlaySound(15)`）。游戏层 `AudioHandler` partial 已为 `PlaySound`/`PlayMusicForLoop`/`PlayMusicListForLoop`/`PlayEnvironment` 提供枚举重载，内部 `(long)` 转发框架层 long 接口；框架层 long 接口保留给配置驱动的动态 id（如 `soundHitId`/`soundMissId`，均为 `long`）。另有 `PlaySoundRandom(params AudioEnum[])` 从多个候选音效中等概率随机取一个播放（同类多备选避免重复，如清扫 `sound_clean_1`/`sound_clean_2`）
- **新增/删除音频时必须同步维护 `AudioEnum`**（与 `AudioInfo` 配置表一一对应），否则枚举与 id 错位
- 音量设置通过 VolumeHandler 管理
- 按钮音效使用 ButtonAudio 组件
- 音频资源通过 Manager 缓存避免重复加载
