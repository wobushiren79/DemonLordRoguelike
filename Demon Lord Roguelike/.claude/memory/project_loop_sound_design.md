---
name: project_loop_sound_design
description: 连续音效(LoopSound)已实现——框架层通用"循环播放能力"，复用任意音频clip，非新audio_type
metadata:
  type: project
---

音频系统第 4 类能力 **「连续音效」(代码 `LoopSound`)** 已实现（2026-07-01）。用于走路/下雨这类"持续循环、按需起停、可多路并发"的音效。参见 audio-system skill。

**最终采用"通用循环播放能力"而非"新增资源类型"**（因用户两条约束：①走路声复用现有 `sound_walk_1`；②改动全在框架层供其他项目复用）：
- **不新增** `AuidoTypeEnum.Loop`、不建 `Audio/Loop/` 目录、不加 Addressables 分组、不加 Excel 行、不加 `AudioEnum` 项、**不动 Environment**。
- `PlayLoopSound(任意音频id)` → 读该 id 的 `AudioInfoBean.audio_type` → 用现有 `manager.LoadClipDataByAddressbles((AuidoTypeEnum)audio_type, name_res, cb)` 取 clip → 在**循环 AudioSource 池**上 `loop=true` 播放。任何现有音效都能被当连续音效循环播。

**框架层改动**：
- `AudioManager`(框架)：加连续音效音源池——`loopSoundRoot` 容器(挂 audioListener GO 下,DontDestroyOnLoad)、`Queue<AudioSource> queueLoopSourceIdle`、`DequeueLoopSource()`(池空则新建,超 `MaxLoopSource=16` 返 null 告警)、`RecycleLoopSource()`(先 Stop 再清 clip 再 SetActive(false) 入队)、`spatialBlend=0`(第一期2D全局单路)。
- `AudioHandler`(框架)：`#region 连续音效`——嵌套类 `LoopSoundEntry{source,volumeScale,token,canceled}`、`Dictionary<long,LoopSoundEntry> dicLoopActive`、`PlayLoopSound`(×2)/`StopLoopSound`/`StopAllLoopSound`/`PauseAllLoopSound`/`RestoreAllLoopSound`/`IsLoopSoundPlaying`；`InitAudio` 末尾遍历刷新 `source.volume=soundVolume*entry.volumeScale`。

**关键实现点**：
- **音量跟随 soundVolume**(不加 loopVolume/滑条)；最终音量=soundVolume×配置volume_scale。
- **异步竞态防护(核心)**：PlayLoopSound 先登记 pending entry(source=null,token=++seed) 占位→ContainsKey 即去重；加载回调校验 `dicLoopActive[id]==entry && !canceled && token 未变` 才 DequeueLoopSource 起播；StopLoopSound 对加载中 id 置 canceled→回调丢弃。防"停请求先于加载回调→孤儿音源停不掉"。
- **暂停不误启**：PauseAllLoopSound 只暂停当前 isPlaying 的源并记入 `listLoopPaused`，Restore 只 UnPause 这批。
- 不碰一次性音效防抖变量(lastPlaySoundId/timeUpdateForRepeatPlay)。

**游戏层改动**：
- `AudioHandler`(游戏 partial)：`AudioEnum` 重载 PlayLoopSound(×2)/StopLoopSound/IsLoopSoundPlaying。
- `ControlForGameBase.HandleForMoveUpdate`：移动分支(:159 Walk 后)`PlayLoopSound(sound_walk_1, pitch:1.5f)`(加快脚步,1.5 倍速)、静止分支(:131 Idle 后)`StopLoopSound`、`EnabledControl(false)` 也 StopLoopSound。**只处理基地自控魔王,单实体,靠幂等去重免计数。**
- `WorldHandler.ClearWorldData`：EnableAllControl(false) 后加 `StopAllLoopSound()` 兜底(音源常驻 DontDestroyOnLoad 不随场景销毁)。

**2026-07-05 变速**：`PlayLoopSound` 合并为 `PlayLoopSound(id, float volumeScale=-1f, float pitch=1f)`(volumeScale<0 取 soundVolume)；新增 `LoopSoundEntry.pitch`，起播 `source.pitch=pitch`，`RecycleLoopSource` 回收复位 `pitch=1` 防污染池；走路声传 `pitch:1.5f` 加快(1.5 倍速; Unity 音源变速必同时升调,无法只变速)。游戏层枚举重载同步收敛为 3 参带默认。

**未做/技术债**：独立 loopVolume 音量条；按句柄多路+3D 位置跟随(每生物脚步声)；同 id 平滑换 clip(雨强度切换,当前按 id 去重会忽略)；同 id 变更 pitch 也被去重忽略(需先 Stop 再 Play)；dicLoopData 同其它音频缓存从不 Addressables.Release(既有债,池仅解组件泄漏)。已同步 audio-system SKILL + system-audio agent + control-system SKILL + game-control agent 文档。
