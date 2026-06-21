---
name: feedback_audio_use_enum
description: 音频播放统一用 AudioEnum 枚举调用，禁止裸 int id；新增音频须同步维护枚举
metadata:
  type: feedback
---

音效/音乐/环境音播放统一用 `AudioEnum` 枚举调用，**禁止传裸 int id**（如 `PlaySound(15)`）。

**Why:** 裸 id 不直观、易写错、无编译期校验，用户明确要求改成枚举调用。

**How to apply:**
- 枚举定义在 [Assets/Scripts/Enums/AudioEnum.cs](Assets/Scripts/Enums/AudioEnum.cs)：枚举值 = `AudioInfo` 配置表 id，枚举名 = `name_res`（去扩展名），由 `AudioInfo.txt` 一一对应生成。
- 游戏层 `AudioHandler` partial（[Assets/Scripts/Component/Handler/AudioHandler.cs](Assets/Scripts/Component/Handler/AudioHandler.cs)）为 `PlaySound`/`PlayMusicForLoop`/`PlayMusicListForLoop`/`PlayEnvironment` 提供 `AudioEnum` 重载，内部 `(int)枚举` 转发到框架层 int 接口。框架层 AudioHandler 保持 int-based 不动（layering）。
- **例外**：从配置 Bean 动态读出的 id（如 `FightUnderAttackData.soundHitId`/`soundMissId`）本就是 int，继续走 int 接口。
- **新增/删除音频时必须同步维护 AudioEnum**（手工维护，首次由配置表生成），否则枚举与 id 错位。
- 配套文档：[[feedback_agent_skill_sync]] —— 改 AudioHandler/AudioEnum 须同步 audio-system skill 与 system-audio agent（已加 AudioEnum.cs 到 watched_files）。
