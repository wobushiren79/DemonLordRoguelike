---
name: system-audio
description: 音频系统开发：AudioHandler/AudioManager、音效播放、背景音乐、音量管理、ButtonAudio。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/AudioHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/AudioManager.cs
  - Assets/Scripts/Component/Handler/VolumeHandler.cs
  - Assets/FrameWork/Scripts/Component/UI/AudioView.cs
  - Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs
---

# 音频系统 (Audio System) 开发代理

你负责音频系统的开发。

## 职责范围

### 音频管理
- **AudioHandler** - 音频逻辑处理单例 [FrameWork/Scripts/Component/Handler/AudioHandler.cs](Assets/FrameWork/Scripts/Component/Handler/AudioHandler.cs)
- **AudioManager** - 音频资源与播放管理 [FrameWork/Scripts/Component/Manager/AudioManager.cs](Assets/FrameWork/Scripts/Component/Manager/AudioManager.cs)

### 音量管理
- **VolumeHandler** / **VolumeManager** - 音量设置 [Scripts/Component/Handler/VolumeHandler.cs](Assets/Scripts/Component/Handler/VolumeHandler.cs)

### 音频 UI 组件
- **AudioView** - 音频视图控制 [FrameWork/Scripts/Component/UI/AudioView.cs](Assets/FrameWork/Scripts/Component/UI/AudioView.cs)
- **ButtonAudio** - 按钮音效 [FrameWork/Scripts/Component/UI/ButtonAudio.cs](Assets/FrameWork/Scripts/Component/UI/ButtonAudio.cs)

### 音频数据
- **AudioBean** - 音频资源数据
- **AudioInfoBean / AudioInfoBeanPartial** - 音频配置信息

## 约束

- 音频播放通过 AudioHandler 统一调用
- 音量设置通过 VolumeHandler 管理
- 按钮音效使用 ButtonAudio 组件
- 音频资源通过 Manager 缓存避免重复加载
