---
name: game-creature
description: 生物系统开发：生物创建/管理/献祭、生物属性、生物卡片、生物培养、阵容管理。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: creature-system
watched_files:
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Manager/CreatureManager.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/MVC/Game/CreatureInfoBeanPartial.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Utils/FightCreatureSearchUtil.cs
---

# 生物系统 (Creature System) 开发代理

你负责 [Scripts/Component/](Assets/Scripts/Component/) 中与生物相关的代码开发。

## 职责范围

### 生物管理
- **CreatureHandler** / **CreatureManager** - 生物逻辑处理与资源管理
- **CreateDefenseCreatureEntity 末尾推送新事件（事件驱动，不再直接重算）** - 加完新防守魔物 BUFF 后 `EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_DefenseCreatureCreate, fightCreatureEntity)`；由 `GameFightLogic.EventForDefenseCreatureCreate` 监听后按守卫 `BuffHandler.Instance.HasDynamicRateAbyssalBlessing()` 重算全体防守属性，供随魔物数缩放类动态率馈赠（加成率随场上魔物数 N 变化；曾用于「都是兄弟」，现役无配置、机制留存）在放置/增殖新魔物时即时生效。CreatureHandler 只负责生成、推事件，重算职责归 GameFightLogic；守卫仅当馈赠池含指定类型/子类 BUFF 才广播，普通对局无开销。详见 abyssal-blessing-system / buff-system SKILL
- **CreatureBean** / **CreatureBeanPartial** - 生物数据模型
- **CreatureAttributeBean** - 生物属性（HP/DR/ATK/ASPD/MSPD/CRT/EVA/RCD 等）
- **CreatureCardItemBean** - 生物卡片数据
- **CreatureNpcBean** - NPC 生物数据
- **体型缩放** - 两条来源同一套解析规则(空/"0"=1倍、"min,max"如"0.9,1.1"=区间随机、"1.1"=固定倍数)：NPC 读 `NpcInfo.body_size` + `NpcInfoBean.GetBodySizeRandomScale()`(见 `SetData(NpcInfoBean)`)；按 creatureId 创建的生物(扭蛋/建号等)读 `CreatureInfo.body_size` + `CreatureInfoBean.GetBodySizeRandomScale()`(见 `SetData(long creatureId)`)。均在创建时随机一次并缓存到 `CreatureBean.bodySizeScale`(默认1)，`CreatureHandler.SetCreatureData` 以 `size_spine × GetBodySizeScale()` 应用到 localScale（未配置 body_size 的生物倍率恒为1）。CreatureInfo 侧另有区间解析 `CreatureInfoBean.GetBodySizeRange(out min, out max)`（供创建界面身高滑条取上下限，`GetBodySizeRandomScale` 已改为基于它随机）
- **NPC 稀有度** - NPC 创建(`SetData(NpcInfoBean)`)稀有度取自 `NpcInfo.rarity` 列(int, 空/0=N, 按 RarityEnum 值 1=N~6=L)；未配置维持旧行为全 N。仅写入 `CreatureBean.rarity`(详情显示/CMP 倍率等走 `GetRarityValue()` 的链路自动生效)，不授予稀有度 BUFF(稀有度 BUFF 仅孕育扭蛋 `GashaponItemBean`/测试添加 `UITestBase` 经 `RandomRarityBuffForCreate` 发放)
- **无实体 NPC（creature_id=0）** - `CreatureBean.AddSkinForBase` 开头守卫：`CreatureInfoCfg.GetItemData(creatureId)==null` 直接跳过基础皮肤（直查 Cfg 无日志），支持无生物资源的纯对话 NPC（如监视之塔 id=10001，详见 game-conversation/conversation-system「头像图片模式」）。注意此类生物 `creatureInfo`/`creatureModel` 为 null，`GetAttribute` 任意属性均会经 `GetListBuffData` 解引用 creatureInfo 触发 NRE——仅供对话展示，禁止用于战斗/详情/榨汁等属性链路
- **场上魔物描边高亮** - `CreatureHandler.ShowCreatureOutlinePreview(FightCreatureEntity)` / `HideCreatureOutlinePreview()` → `CreatureManager`(只负责懒加载预览预制+取组件)。**显示/材质/逐帧跟随逻辑都在 `CreatureSpineOutlineFollow` 组件**(`Assets/Scripts/Game/Fight/`，`Show`/`Hide`)。共享单例描边预览预制 `FightCreature_OutlinePreview.prefab`(由 `FightCreature_SelectPreview` 复制，Spine MeshRenderer 挂亮蓝 OutlineOnly 描边材质 `MatSpriteCreatureOutline.mat`；颜色由材质决定不写死)，悬停已上场魔物卡牌时移动到目标生物处。描边经 `SkeletonAnimation.CustomMaterialOverride` 把目标图集材质替换为描边材质(`_MainTex` 填目标图集纹理)，平面 Spine 精灵的 Rim 边缘光因固定法线不可见故改用 OutlineOnly。**逐帧跟随动画**：组件订阅自身 `SkeletonAnimation.UpdateLocal` 逐根复制目标骨骼本地 SRT，`LateUpdate` 同步位置/`localScale`(含翻转)，使描边跟上目标正在播放的动画(非定格首帧)。由战斗卡牌 `UIViewCreatureCardItemForFight.OnPointerEnter/OnPointerExit` 触发。
- **creature_layer 分层（搜索隐身+显示前置，勿当 bug 修）** - `excel_creature_info.creature_layer` 配 `CreatureDef_Front`/`CreatureAtt_Front` 时，`CreatureHandler.GetFightCreatureObj` 改根 GameObject.layer（BoxCollider 在根节点随根改层）+ Spine Z 前移 0.1。索敌/攻击物理搜索 mask 写死 `1 << CreatureDef`/`CreatureAtt`（FightCreatureSearchUtil、BaseAttackMode），Front 层不在 mask 内→**敌人搜不到/不攻击它，这是故意设计**：典型使用者烂泥史莱姆(id=3003)——敌人走过被减速但不把它当目标。**禁止以"修复索敌失效"为由扩搜索 mask 或改回 layer**。只要"显示在前"不想影响索敌时勿复用 Front 层，走渲染侧方案(sortingOrder)。`creature_layer_find` 字段未接线。详见 [creature-system](../skills/creature-system/SKILL.md)「CreatureInfoBean - 生物配置」
- **冲锋自爆型生物（creature_info 新列 `charge_attack`）** - int 配置列（0=默认站桩，1=冲锋自爆），`CreatureInfoBean.charge_attack`（自动生成列）+ `CreatureInfoBeanPartial.IsChargeAttack()` 解析。当前唯一配置生物：**6003 哥布林敢死队**（attack_mode=300002 爆炸、attack_search_range=0.5 触发距离、attack_search_time=0.1、RCD=60）。冲锋自爆语义：放卡后立即向+X冲锋并释放原占位格（`FightCreatureBean.isPositionReleased` 置位），前方0.5遇敌/冲到路尽头/被打死时在死亡位置原地自爆（AoE 500），死亡后卡片60秒CD回手。死亡引爆实现见 game-fight-core agent，重生联动见 buff-system SKILL「死亡重生」
- **CreateDefenseCreatureEntity 同UUID已死实体预清理** - Add 进主列表前，若存在同 UUID 且 `IsDead()` 的旧实体（DeadRebirth 重生替换场景），先按实例移出——防 `DictionaryList.Add` 同 key 静默失败导致新实体成幽灵（不可命中/清场泄漏）
- **RemoveFightCreatureEntity 防守分支改动** - ①移除改为**按实例**（新 `FightBean.RemoveDefenseCreature(FightCreatureEntity)`，`DictionaryList.RemoveByValue`）；`FightBean.RemoveDefenseCreatureByPos` **已删除**（按 positionCreate 首匹配会误删同格生物）。②卡片进 Rest(CD) 的条件改为「场上已无该 UUID 存活实体」——DeadRebirth 重生替换场景新实体在场，卡片保持 Fighting 不进 CD（详见 buff-system SKILL「死亡重生」）

### 生物 UI
- **UICreatureManager** - 生物管理界面
- **UICreatureChange** - 生物转换界面
- **UICreatureVat** - 生物培养舱界面
- **UILineupManager** - 阵容管理界面。阵容重命名：研究 `UnlockEnum.LineupRename`(201000001,前置=解锁多阵容)解锁后显示 RenameBtn（悬停气泡 UIText 30008），点击开 `UIDialogRename` 给当前选中阵容改名（`OnClickForRename`，限 10 字，提交空名=恢复默认）；自定义名存 `UserDataBean.dicLineupName`(阵容序号→名字，`GetLineupName`/`SetLineupName` 读写)，显示名 `UserDataBean.GetLineupShowName`(自定义名优先、未改名回退默认「阵容 {序号}」UIText 30005，本界面与 UIDialogPortalDetails 出战阵容选择区共用此单一真实源)
- **UIViewCreatureCardItem** - 生物卡片组件
- **UIViewCreatureCardList** - 生物卡片列表
- **UIViewCreatureCardDetails** - 生物卡片详情

### 生物属性枚举
```csharp
CreatureAttributeTypeEnum
├── HP, MP(魔力), DR, ATK
├── ASPD (攻击速度), MSPD (移动速度)
├── CRT (暴击率), EVA (闪避率)
├── RCD (复活CD), MPR (魔法回复%), MPF (魔法回复)
├── CMP (召唤魔力消耗, 基础值=CreatureInfo.CMP; GetAttribute(CMP)=基础CMP×(1+等级/稀有度增加倍率)再经BUFF修正; 倍率求和见 CreatureBean.GetCreateMPAddRate)
└── CDMG (暴击伤害倍率, 基础1.5=暴击伤害+50%, BUFF rate Flat累加可调)
⚠️枚举值即 excel_creature_attribute_type_info 表 id（属性中文名/颜色映射），新增属性只能追加到末尾
```

### 关键文件

| 文件 | 路径 |
|------|------|
| CreatureHandler | Assets/Scripts/Component/Handler/CreatureHandler.cs |
| CreatureManager | Assets/Scripts/Component/Manager/CreatureManager.cs |
| CreatureBean | Assets/Scripts/Bean/Game/CreatureBean.cs |
| CreatureUtil | Assets/Scripts/Utils/CreatureUtil.cs |
| FightCreatureSearchUtil | Assets/Scripts/Utils/FightCreatureSearchUtil.cs |

## 约束

- 生物属性和 BUFF 加成计算需正确叠加
- 生物创建通过 CreatureHandler 统一入口
- 生物卡片 UI 使用 UIView 前缀命名

## 关联 Skill

详细开发指南请参考: [creature-system](../skills/creature-system/SKILL.md)
