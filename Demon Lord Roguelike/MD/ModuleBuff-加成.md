# 加成模块 (Buff Module) 分析文档

## 一、模块概述

加成模块负责游戏中所有增益/减益效果（Buff）的定义、施加、持续、叠加、移除以及效果计算。涵盖属性加成、状态效果、触发型 Buff 等多种类型。

---

## 二、核心数据结构

### 2.1 BuffBean（Buff 实例）

**文件**: `Bean/Game/BuffBean.cs` + `BuffBeanPartial.cs`

Buff 的运行时实例，代表施加在生物身上的一个具体增益/减益效果。

| 字段 | 类型 | 说明 |
|------|------|------|
| `buffId` | `long` | Buff 配置 ID，对应 BuffInfoBean.id |
| `stackNum` | `int` | Buff 叠加层数 |
| `duration` | `float` | 剩余持续时间（秒），-1 表示永久 |
| `sourceId` | `long` | 施加者 ID（生物或技能 ID） |
| `dicAttribute` | `Dictionary<CreatureAttributeTypeEnum, float>` | 属性加成值（HP/DR/ATK/ASPD 等） |

**关键方法**:
- `AddStack(int num)` — 增加层数
- `RemoveStack(int num)` — 减少层数
- `AddDuration(float seconds)` — 增加持续时间
- `UpdateDuration(float deltaTime)` — 更新持续时间，返回是否过期
- `GetAttribute(type)` — 获取指定属性加成值
- `GetTotalAttribute(type)` — 获取考虑层数后的总属性加成

**Partial 扩展** (`BuffBeanPartial.cs`):
- 懒加载 `buffInfo` 属性，通过 `BuffInfoCfg.GetBuffData(buffId)` 从配置表获取静态信息
- 标记 `[JsonIgnore]` 避免序列化

### 2.2 BuffInfoBean（Buff 配置表）

**文件**: `Bean/MVC/Game/BuffInfoBean.cs` + `BuffInfoBeanPartial.cs`

JSON 配置表数据，继承 `BaseBean`，由 `BuffInfoCfg` 管理。

| 字段 | 类型 | 说明 |
|------|------|------|
| `buff_name` | `long` | 多语言文本 ID |
| `buff_type` | `int` | Buff 类型（1 属性加成 2 状态效果 3 触发型 4 特殊） |
| `duration` | `float` | 基础持续时间（秒），-1 表示永久 |
| `max_stack` | `int` | 最大叠加层数，1 表示不可叠加 |
| `icon_res` | `string` | 图标资源名 |
| `icon_rotate_z` | `float` | 图标旋转角度 |
| `attribute_data` | `string` | 属性加成参数字符串 |
| `effect_res` | `string` | 特效资源路径 |
| `priority` | `int` | Buff 优先级（用于冲突处理） |
| `remark` | `string` | 备注 |

**Partial 扩展** (`BuffInfoBeanPartial.cs`):
- `GetBuffType()` — 返回 `BuffTypeEnum` 枚举
- `ParseAttributeData()` — 解析 `attribute_data` 字符串，返回属性类型和加成值的字典
- `IsPermanent()` — 判断是否为永久 Buff
- `CanStack()` — 判断是否可叠加

**配置管理器** `BuffInfoCfg`:
- 继承 `BaseCfg<long, BuffInfoBean>`，从 `"BuffInfo"` JSON 文件加载
- `GetBuffData(long key)` — 按 ID 查询
- `GetAllData()` / `GetAllArrayData()` — 全量查询
- `GetDataByType(BuffTypeEnum)` — 按类型查询

### 2.3 BuffEffectBean（Buff 效果配置表）

**文件**: `Bean/MVC/Game/BuffEffectBean.cs`

| 字段 | 类型 | 说明 |
|------|------|------|
| `effect_type` | `int` | 效果类型（1 持续伤害 2 治疗 3 护盾 4 免疫） |
| `trigger_interval` | `float` | 触发间隔（秒） |
| `trigger_times` | `int` | 触发次数，-1 表示持续到结束 |
| `effect_value` | `float` | 效果值（伤害/治疗量等） |
| `effect_data` | `string` | 额外效果参数 |

**配置管理器** `BuffEffectCfg`:
- 支持 `GetEffectData(long buffId)` 按 BuffID 查询关联效果

---

## 三、枚举定义

**文件**: `Enums/BuffEnum.cs` + `Enums/GameStateEnum.cs`

### BuffTypeEnum（Buff 类型）
```csharp
Attribute = 1,      // 属性加成（ATK/DEF/HP 等）
Status = 2,         // 状态效果（眩晕/减速/中毒等）
Trigger = 3,        // 触发型（反击/闪避/暴击等）
Special = 4         // 特殊效果（无敌/隐身/霸体等）
```

### BuffSourceEnum（Buff 来源）
```csharp
Skill = 1,          // 技能施加
Item = 2,           // 道具施加
Creature = 3,       // 生物自带
Environment = 4,    // 环境效果
System = 5          // 系统效果
```

### BuffConflictEnum（Buff 冲突类型）
```csharp
None = 0,           // 无冲突
SameType = 1,       // 同类型冲突
Exclusive = 2,      // 互斥冲突
Priority = 3        // 优先级覆盖
```

### CreatureAttributeTypeEnum（生物属性类型）
```csharp
HP = 1,             // 生命值
ATK = 2,            // 攻击力
DEF = 3,            // 防御力
ASPD = 4,           // 攻击速度
CRIT = 5,           // 暴击率
MOVE = 6,           // 移动速度
// ... 更多属性
```

---

## 四、Buff 管理系统

### 4.1 数据存储

**文件**: `Bean/Game/CreatureBean.cs`

每个生物通过 `List<BuffBean> listBuffs` 存储当前身上的所有 Buff。

### 4.2 Buff 操作接口（CreatureBean）

| 方法 | 说明 |
|------|------|
| `AddBuff(long buffId, float duration, long sourceId)` | 施加 Buff，返回 Buff 实例 |
| `RemoveBuff(BuffBean buff)` | 移除指定 Buff |
| `RemoveBuffByType(BuffTypeEnum type)` | 移除指定类型的所有 Buff |
| `RemoveBuffBySource(long sourceId)` | 移除指定来源的所有 Buff |
| `GetBuff(long buffId)` | 获取指定 Buff 实例（可叠加时返回第一层） |
| `GetBuffStack(long buffId)` | 获取指定 Buff 的层数 |
| `HasBuff(long buffId)` | 判断是否拥有指定 Buff |
| `HasBuffType(BuffTypeEnum type)` | 判断是否拥有指定类型的 Buff |
| `GetAttribute(CreatureAttributeTypeEnum type)` | 获取包含 Buff 加成的总属性值 |
| `UpdateBuffs(float deltaTime)` | 更新所有 Buff 持续时间 |
| `ClearBuffs()` | 清空所有 Buff |

### 4.3 Buff 施加流程

```
1. 检查 Buff 配置是否存在
2. 检查是否已有同名 Buff
   ├── 有且可叠加 → 增加层数/刷新持续时间
   ├── 有但不可叠加 → 检查冲突规则
   │       ├── 优先级低 → 忽略
   │       └── 优先级高 → 移除旧 Buff，添加新 Buff
   └── 无 → 创建新 Buff 实例
3. 应用 Buff 效果（属性加成/状态改变）
4. 触发 Buff_Add 事件
5. 返回 Buff 实例
```

### 4.4 Buff 移除流程

```
1. 移除 Buff 实例
2. 清除 Buff 效果（属性还原/状态清除）
3. 触发 Buff_Remove 事件
4. 如有特效，销毁特效
```

### 4.5 Buff 更新流程

在 `CreatureBean.Update()` 或独立管理器中：

```csharp
foreach (var buff in listBuffs)
{
    if (!buff.IsPermanent())
    {
        buff.UpdateDuration(deltaTime);
        if (buff.IsExpired())
        {
            RemoveBuff(buff);
        }
    }
}
```

### 4.6 事件通知

| 事件名 | 触发时机 |
|--------|----------|
| `Buff_Add` | 施加 Buff 时触发 |
| `Buff_Remove` | 移除 Buff 时触发 |
| `Buff_Stack_Change` | Buff 层数变化时触发 |
| `Buff_Duration_Change` | Buff 持续时间变化时触发 |
| `Buff_Effect_Trigger` | Buff 效果触发时触发 |

---

## 五、属性计算系统

### 5.1 属性汇总

**文件**: `Bean/Game/CreatureBean.cs`

生物的最终属性 = 基础属性 + 装备加成 + Buff 加成 + 其他修正

```csharp
public float GetAttribute(CreatureAttributeTypeEnum type)
{
    float baseValue = baseAttributes[type];
    float equipBonus = GetEquipAttribute(type);      // 装备加成
    float buffBonus = GetBuffAttribute(type);        // Buff 加成
    float otherBonus = GetOtherAttribute(type);      // 其他加成
    
    return baseValue + equipBonus + buffBonus + otherBonus;
}
```

### 5.2 Buff 属性计算

```csharp
public float GetBuffAttribute(CreatureAttributeTypeEnum type)
{
    float total = 0;
    foreach (var buff in listBuffs)
    {
        if (buff.dicAttribute.ContainsKey(type))
        {
            // 考虑层数
            total += buff.GetAttribute(type) * buff.stackNum;
        }
    }
    return total;
}
```

### 5.3 属性类型

| 属性类型 | 计算方式 | 说明 |
|----------|----------|------|
| `HP` | 固定值 | 生命值上限加成 |
| `ATK` | 固定值 | 攻击力加成 |
| `DEF` | 固定值 | 防御力加成 |
| `ASPD` | 固定值/百分比 | 攻击速度加成 |
| `CRIT` | 百分比 | 暴击率加成 |
| `MOVE` | 固定值/百分比 | 移动速度加成 |

---

## 六、UI 层结构

### 6.1 Buff 展示

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIViewBuffSlot` | `UI/Common/Buff/UIViewBuffSlot.cs` | 单个 Buff 格子：展示图标、层数、持续时间进度条 |
| `UIViewBuffList` | `UI/Common/Buff/UIViewBuffList.cs` | Buff 列表：横向/纵向排列，支持排序 |

### 6.2 Buff 详情弹窗

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIPopupBuffInfo` | `UI/Popup/BuffInfo/UIPopupBuffInfo.cs` | Buff 详情弹窗：展示名字、图标、描述、剩余时间 |

### 6.3 战斗 Buff 栏

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIBattleBuffBar` | `UI/Game/Battle/UIBattleBuffBar.cs` | 战斗时玩家 Buff 栏：实时显示当前所有 Buff |

---

## 七、图标与特效系统

### 7.1 图标加载

**文件**: `Component/Handler/IconHandler.cs`

| 方法 | 说明 |
|------|------|
| `SetBuffIcon(long buffId, Image)` | 根据 BuffID 查配置表，加载图标到 Image |
| `SetBuffIcon(long buffId, SpriteRenderer)` | 同上，目标为 SpriteRenderer |
| `SetBuffIcon(string iconName, float rotateZ, Image)` | 从 SpriteAtlas 加载并设置旋转 |

图标资源通过 `SpriteAtlasTypeEnum.Buff` 图集管理。

### 7.2 特效管理

**文件**: `Component/Effect/BuffEffectManager.cs`

| 方法 | 说明 |
|------|------|
| `PlayBuffEffect(string effectRes, Transform parent)` | 播放 Buff 特效 |
| `StopBuffEffect(string effectRes)` | 停止 Buff 特效 |
| `SetBuffEffectVisible(BuffBean buff, bool visible)` | 设置 Buff 特效可见性 |

---

## 八、特殊 Buff 类型

### 8.1 持续伤害 Buff (DoT)

- 每隔固定时间造成伤害
- 可叠加（层数增加伤害或刷新持续时间）
- 示例：中毒、燃烧、流血

### 8.2 持续治疗 Buff (HoT)

- 每隔固定时间恢复生命值
- 可叠加
- 示例：再生、祝福

### 8.3 护盾 Buff

- 吸收伤害
- 有耐久值，吸收足够伤害后消失
- 示例：魔法护盾、神圣护甲

### 8.4 免疫 Buff

- 免疫特定类型的伤害或效果
- 有持续时间
- 示例：无敌、魔法免疫

### 8.5 触发型 Buff

- 满足条件时触发效果
- 可能有内置冷却时间 (ICD)
- 示例：反击、闪避、暴击加成

---

## 九、关联系统

### 9.1 技能系统关联

**文件**: `Bean/Game/SkillBean.cs`

技能是 Buff 的主要来源之一：
- 技能命中时施加 Buff
- 技能效果可能包含 Buff 效果
- 技能等级影响 Buff 强度/持续时间

### 9.2 道具系统关联

**文件**: `Bean/Game/ItemBean.cs`

道具可以施加 Buff：
- 消耗品道具使用后施加 Buff
- 装备道具穿戴时提供持续性 Buff
- 装备词条可能触发 Buff

### 9.3 成就系统关联

**文件**: `Bean/Game/AchievementBean.cs`

某些成就需要 Buff 相关条件：
- 累计施加 Buff 次数
- 同时拥有 Buff 数量
- Buff 持续时间统计

### 9.4 任务系统关联

**文件**: `Bean/Game/TaskBean.cs`

任务可能要求：
- 使用特定 Buff
- 在 Buff 状态下完成目标
- 收集 Buff 相关道具

---

## 十、数据流总结

```
[JSON 配置文件]
    │
    ▼
BuffInfoCfg (静态配置缓存) ◄─── BuffInfoBean (Buff 定义)
BuffEffectCfg (效果配置缓存) ◄─── BuffEffectBean (效果定义)
    │
    ▼
BuffBean (Buff 运行时实例)
    │  ├── buffInfo → 懒加载关联 BuffInfoBean
    │  ├── stackNum → 层数
    │  ├── duration → 持续时间
    │  └── dicAttribute → 属性加成
    │
    └──► CreatureBean.listBuffs (生物 Buff 存储)
            ├── AddBuff() → 施加/叠加
            ├── RemoveBuff() → 移除
            ├── UpdateBuffs() → 更新持续时间
            └── GetBuffAttribute() → 属性汇总

[UI 层]
UIViewBuffList → UIViewBuffSlot → Buff 展示
UIPopupBuffInfo → Buff 详情弹窗
UIBattleBuffBar → 战斗 Buff 栏

[关联系统]
SkillBean → 技能施加 Buff
ItemBean → 道具施加 Buff
AchievementBean → Buff 相关成就
TaskBean → Buff 相关任务
```

---

## 十一、扩展建议

1. **Buff 链系统**: 支持 Buff 组合效果，特定 Buff 组合触发额外效果
2. **Buff 抗性系统**: 生物对特定类型 Buff 有抗性/免疫
3. **Buff 优先级细化**: 支持更复杂的 Buff 冲突处理规则
4. **Buff 可视化编辑器**: 可视化配置 Buff 效果和参数
5. **Buff 历史记录**: 记录战斗中的 Buff 施加/移除历史，用于回放和分析
