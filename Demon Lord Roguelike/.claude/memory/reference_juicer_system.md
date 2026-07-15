---
name: reference_juicer_system
description: 魔汁机(魔物回收/Juicer)系统术语与骨架：UI驱动+轻量Logic,榨汁流程/奖励留桩待接入
metadata:
  type: reference
---

「魔汁机(Juicer)」= 魔物回收设施。**术语统一**：面向玩家叫「魔汁机」，代码用 `Juicer`/`CreatureJuicer`；ScenePrefab 旧「榨汁机」注释已改为「魔汁机」，二者同义。

**架构=UI驱动+轻量Logic(仿容器 UICreatureVat,非献祭全流程)**：基地魔汁机建筑 `ScenePrefabForBase.objBuildingJuicer`(门控 `UnlockEnum.Juicer=100600001`,新设施块1006) → 走近按E(`ControlInteractionEnum.JuicerInteraction=9`,交互碰撞体命名固定 `JuicerInteraction`,提示 textId=2009) → 直接 `OpenUIAndCloseOther<UICreatureJuicer>`(退出回 `UIBaseMain`) → 选**单个**目标魔物(复用 `CardUseStateEnum.CreatureAscendTarget`+`CardStateEnum.CreatureAscendSelect/NoSelect`,因预制体卡片变体是 `UIViewCreatureCardItemForCreatureAscend`) → Start → `GameHandler.StartCreatureJuicer(target)` → `CreatureJuicerLogic.StartJuice`。

⚠️ **榨汁流程(建筑动画/消耗魔物)与奖励结算目前是留桩**，全部收在 `CreatureJuicerLogic.StartJuice`(仅 LogUtil 打点+TODO)，后续需求接入时优先在此补,别散到UI。

研究/解锁配置：`excel_research_info`+`ResearchInfo.txt`(research_type=1设施,unlock_id=name=id=100600001,前置成就100500001,icon_res 暂用孕育占位 ui_research_57 待替换)、`excel_unlock_info`+`UnlockInfo.txt`、多语言 `Language_ResearchInfo_cn/en`(100600001)与 `Language_UIText_cn/en`(2009魔汁机/61010未选目标提示/61011开始榨汁=Start按钮 ui_BtnStartText textId,预制上直接配无代码赋值),真实源 excel_language 同名工作表。

预制体(objBuildingJuicer 交互碰撞体、UICreatureJuicer 接线)本次由用户手动接好。完整开发指南见 `juicer-system` skill 与 `game-juicer` agent。相关：[[reference_equip_reward_race_gated]] 无关；与献祭 `sacrifice-system`、容器进阶(UICreatureVat)链路可对照。
