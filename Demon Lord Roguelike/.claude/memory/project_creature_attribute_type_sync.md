---
name: project-creature-attribute-type-sync
description: 装备属性池新增枚举时必须同步 CreatureAttributeTypeInfo 配置表，否则详情显示???
metadata:
  type: project
---

装备/道具属性显示依赖两张表的一致性，新增 `CreatureAttributeTypeEnum` 枚举值时必须同步 `excel_creature_attribute_type_info[生物属性信息].xlsx`，否则对应属性在道具详情弹窗显示"???"+黑色。

**Why**：`CreatureAttributeTypeInfoCfg.GetAttributeTypeNameByEnum`/`GetAttributeTypeColorByEnum`（[CreatureAttributeTypeInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/CreatureAttributeTypeInfoBeanPartial.cs)）按枚举值查配置表，查不到(`GetItemData==null`)回退 `"???"`/`Color.black`。而装备属性随机池 [ItemBean.InitRandomAttributeForCreate](Assets/Scripts/Bean/Game/ItemBean.cs)（userType=1 魔王专属池含 MSPD/MP/**MPF**/ATK，普通池 HP/DR/ATK/ASPD）并不校验配置表，一旦抽到未配置枚举即触发。2026-08 实例：魔王专属装备"破烂腰带"随机到 MPF(枚举值11) 而配置表缺 id=11 行 → 存档1"???:+5"黑色。

**How to apply**：
- 修改装备属性池或 `CreatureAttributeTypeEnum` 时，核对配置表 id 覆盖 1~13 全部入池枚举（目前缺行风险位：MPF=11 已于 2026-08-30 补齐，CMP=12 仅作 BUFF 标签不入装备池未补）。
- 属性面板支持 id 1~10、11、13 的显示与颜色（color_text 解析）。语言表 `Language_CreatureAttributeTypeInfo_*` 12 语种 id=11 均已存在。
- 排查同类症状（"???"+黑）：查 `dicAttribute` 的 key → 枚举值 → 配置表 `CreatureAttributeTypeInfo` 是否存在。
