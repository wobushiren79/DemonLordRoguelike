---
name: reference_world_quick_attack_research
description: 世界「加快进攻节奏(Quick)」研究——战斗进度条 Quick 按钮 +10% 推进 + 世界绑定的 id 块约定 nn=30
metadata:
  type: reference
---

战斗界面进攻进度条(`UIViewFightMainAttCreateProgress`, 仅征服模式显示)上的 **Quick 按钮**：点一次立即向前推进「总进攻时长10%」的进度、并把这段时间本应生成的进攻波次全部立即生成；无消耗无冷却，到100%封顶点击无效；进度条用 0.3s DOTween 平滑过渡不瞬跳。

**核心链路**：
- `GameFightLogic.QuickAdvanceAttackCreate(advanceRate=0.1f)`——用与逐帧刷怪 `UpdateGameForAttackCreate` 同一套「累加达标即出下一波」步进语义逐波消费 `timeAttackTotal*0.1` 的时间(含 BOSS 特写 `ShowBossDialog`)，队列耗尽即停、进度封顶，返回最新进度。
- `UIViewFightMainAttCreateProgress.OnClickForQuick`(BaseUIView.OnClickForButton 派发 `ui_Quick`)→ `GetAttackProgress()>=1` 则 return，否则推进后 `SetProgress(newProgress, animTime:0.3f)`。
- 显隐：`UIFightMain.RefreshQuickButtonShow`——仅征服模式且 `UserUnlockBean.CheckIsUnlockWorldQuickAttack(worldId)`(当前世界的 Quick 研究已解锁)才显示；worldId 取 `FightBeanForConquer.gameWorldInfoRandomData.worldId`。

**世界绑定 id 块约定(关键、非显然)**：每个世界的专属解锁 id 都落在 `1003_10_W_nn` 块内(W=世界id, 每世界独占100个id)。块内偏移 01=世界解锁 / 02=无尽 / **12~20=征服难度(难度研究 level_max=9 占满该段)** / **30=Quick**。Quick 偏移取 nn=30 刻意避开 12~20。推导：`unlock_id = 100310000 + worldId*100 + 30`(world1→100310130, world2→100310230…)，**新增世界无需给 excel_game_world_info 加列**。常量集中在 `UserUnlockBean.WORLD_UNLOCK_BLOCK_BASE / WORLD_QUICK_ATTACK_UNLOCK_OFFSET`。

**落表(world1)**：`excel_research_info`(id=100310130, research_type=4 世界分类, icon ui_research_30, level_max=1, pre_unlock_ids 空, pay_crystal=2000) + `excel_unlock_info`(id=100310130, unlock_type=0) + `excel_language` ResearchInfo(cn加快进攻节奏/en Faster Assault)。目前只有剑与魔法(world1)加了这一个节点。

详见 [[research-system]] 「世界分支」与 game-fight-system SKILL。研究门控模式同 [[reference_portal_reward_pregen_research_gate]]。
