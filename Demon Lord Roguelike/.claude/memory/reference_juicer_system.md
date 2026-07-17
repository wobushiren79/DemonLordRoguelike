---
name: reference_juicer_system
description: 魔汁机(魔物回收/Juicer)系统术语与骨架：UI驱动+轻量Logic,榨汁流程/奖励留桩待接入
metadata:
  type: reference
---

「魔汁机(Juicer)」= 魔物回收设施。**术语统一**：面向玩家叫「魔汁机」，代码用 `Juicer`/`CreatureJuicer`；ScenePrefab 旧「榨汁机」注释已改为「魔汁机」，二者同义。

**架构=UI驱动+轻量Logic(仿容器 UICreatureVat,非献祭全流程)**：基地魔汁机建筑 `ScenePrefabForBase.objBuildingJuicer`(门控 `UnlockEnum.Juicer=100600001`,新设施块1006) → 走近按E(`ControlInteractionEnum.JuicerInteraction=9`,交互碰撞体命名固定 `JuicerInteraction`,提示 textId=2009) → 直接 `OpenUIAndCloseOther<UICreatureJuicer>`(退出回 `UIBaseMain`) → **多选**投入魔物(`listSelectCreature`,复用 `CardUseStateEnum.CreatureAscendTarget`+`CardStateEnum.CreatureAscendSelect/NoSelect`,因预制体卡片变体是 `UIViewCreatureCardItemForCreatureAscend`) → Start → `GameHandler.StartCreatureJuicer(List<CreatureBean>)` → `CreatureJuicerLogic.StartJuice(List)`。

**镜头**：OpenUI 切 `CameraHandler.SetJuicerCamera(int.MaxValue,true)`(CV_Juicer 固定机位,无需 Follow,同扭蛋机) + `VolumeHandler.SetDepthOfFieldActive(false)`;CloseUI 还原 DoF=true,基地镜头由返回 `UIBaseMain` 时统一还原。CV_Juicer 的 GameObject 由用户在基地场景 CV_List 手动建好。

**投入数量(多选上限)**：`GetJuicerMax()`=`UserUnlockBean.GetUnlockJuicerCreatureMax()`=`UserLimmitBean.juicerCreatureMax(基础5)` + `UnlockEnum.JuicerNum(100600002)` 研究等级(level_max=10,满级15,同献祭/进阶口径)。达上限再点弹 Toast `61012`(最多只能投入{0}只魔物)。常驻计数文本 `ui_LimmitText`(TMP,AutoLinkUI 按名绑定,预制需有同名子物体)显示「已选/上限」,`ColorUtil.WrapLimitFull` 达上限转红。**默认排序**:`InitCreatureData` 内 `listCreatureData.Sort((a,b)=>b.level.CompareTo(a.level))` 等级降序;已排除上阵魔物(`CheckIsInAnyLineup`)与非 Idle 态。

⚠️ **榨汁流程(建筑动画/消耗魔物)与奖励结算目前仍是留桩**，全部收在 `CreatureJuicerLogic.StartJuice(List)`(仅 LogUtil 打点+TODO)，后续需求接入时优先在此补,别散到UI。

研究/解锁配置：`excel_research_info`+`ResearchInfo.txt` 两个节点——开启节点(research_type=1,id=100600001,前置成就100500001,icon_res=ui_research_65,level_max=1) + 投入数量节点(id=100600002=JuicerNum,前置=100600001,level_max=10,icon_res=ui_research_65 占位待替换,position 需在研究图编辑器微调)、`excel_unlock_info`+`UnlockInfo.txt`(100600001/100600002)、多语言 `Language_ResearchInfo_cn/en`(100600001开启/100600002投入数量+1)与 `Language_UIText_cn/en`(2009魔汁机/61010未投入提示/61011开始榨汁=ui_BtnStartText/61012超上限提示),真实源 excel_language 同名工作表。

预制体(objBuildingJuicer 交互碰撞体、UICreatureJuicer 接线)本次由用户手动接好。完整开发指南见 `juicer-system` skill 与 `game-juicer` agent。相关：[[reference_equip_reward_race_gated]] 无关；与献祭 `sacrifice-system`、容器进阶(UICreatureVat)链路可对照。
