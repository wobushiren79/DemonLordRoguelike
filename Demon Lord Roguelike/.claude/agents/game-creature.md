---
name: game-creature
description: 生物系统开发：生物创建/管理/献祭、生物属性、生物卡片、生物培养、阵容管理。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: creature-system
watched_files:
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Manager/CreatureManager.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Utils/FightCreatureSearchUtil.cs
---

# 生物系统 (Creature System) 开发代理

你负责 [Scripts/Component/](Assets/Scripts/Component/) 中与生物相关的代码开发。

## 职责范围

### 生物管理
- **CreatureHandler** / **CreatureManager** - 生物逻辑处理与资源管理
- **CreateDefenseCreatureEntity 末尾推送新事件（事件驱动，不再直接重算）** - 加完新防守魔物 BUFF 后 `EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_DefenseCreatureCreate, fightCreatureEntity)`；由 `GameFightLogic.EventForDefenseCreatureCreate` 监听后按守卫 `BuffHandler.Instance.HasDynamicRateAbyssalBlessing()` 重算全体防守属性，供动态数值馈赠「都是兄弟」（加成率随场上魔物数 N 变化）在放置/增殖新魔物时即时生效。CreatureHandler 只负责生成、推事件，重算职责归 GameFightLogic；守卫仅当馈赠池含指定类型/子类 BUFF 才广播，普通对局无开销。详见 abyssal-blessing-system / buff-system SKILL
- **CreatureBean** / **CreatureBeanPartial** - 生物数据模型
- **CreatureAttributeBean** - 生物属性（HP/DR/ATK/ASPD/MSPD/CRT/EVA/RCD 等）
- **CreatureCardItemBean** - 生物卡片数据
- **CreatureNpcBean** - NPC 生物数据
- **NPC体型缩放** - `NpcInfo.body_size`(string) 配置体型倍率：空/"0"=1倍、"min,max"(如"0.9,1.1")=区间随机、"1.1"=固定倍数；`NpcInfoBean.GetBodySizeRandomScale()` 解析，`CreatureBean.SetData(NpcInfoBean)` 创建时随机一次并缓存到 `CreatureBean.bodySizeScale`(默认1)，`CreatureHandler.SetCreatureData` 以 `size_spine × GetBodySizeScale()` 应用到 localScale（普通生物倍率恒为1）
- **场上魔物描边高亮** - `CreatureHandler.ShowCreatureOutlinePreview(FightCreatureEntity)` / `HideCreatureOutlinePreview()` → `CreatureManager`(只负责懒加载预览预制+取组件)。**显示/材质/逐帧跟随逻辑都在 `CreatureSpineOutlineFollow` 组件**(`Assets/Scripts/Game/Fight/`，`Show`/`Hide`)。共享单例描边预览预制 `FightCreature_OutlinePreview.prefab`(由 `FightCreature_SelectPreview` 复制，Spine MeshRenderer 挂亮蓝 OutlineOnly 描边材质 `MatSpriteCreatureOutline.mat`；颜色由材质决定不写死)，悬停已上场魔物卡牌时移动到目标生物处。描边经 `SkeletonAnimation.CustomMaterialOverride` 把目标图集材质替换为描边材质(`_MainTex` 填目标图集纹理)，平面 Spine 精灵的 Rim 边缘光因固定法线不可见故改用 OutlineOnly。**逐帧跟随动画**：组件订阅自身 `SkeletonAnimation.UpdateLocal` 逐根复制目标骨骼本地 SRT，`LateUpdate` 同步位置/`localScale`(含翻转)，使描边跟上目标正在播放的动画(非定格首帧)。由战斗卡牌 `UIViewCreatureCardItemForFight.OnPointerEnter/OnPointerExit` 触发。

### 生物 UI
- **UICreatureManager** - 生物管理界面
- **UICreatureChange** - 生物转换界面
- **UICreatureVat** - 生物培养舱界面
- **UILineupManager** - 阵容管理界面
- **UIViewCreatureCardItem** - 生物卡片组件
- **UIViewCreatureCardList** - 生物卡片列表
- **UIViewCreatureCardDetails** - 生物卡片详情

### 生物属性枚举
```csharp
CreatureAttributeTypeEnum
├── HP, DR, ATK
├── ASPD (攻击速度), MSPD (移动速度)
├── CRT (暴击率), EVA (闪避率)
├── RCD (复活CD)
├── HPRegeneration
└── CMP (召唤魔力消耗, 基础值=CreatureInfo.CMP; GetAttribute(CMP)=基础CMP×(1+等级/稀有度增加倍率)再经BUFF修正; 倍率求和见 CreatureBean.GetCreateMPAddRate)
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
