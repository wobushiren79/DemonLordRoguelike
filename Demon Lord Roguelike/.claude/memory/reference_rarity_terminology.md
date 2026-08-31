---
name: reference_rarity_terminology
description: 稀有度术语约定:统一叫「稀有度」(rarity),N/R/SR/SSR/UR/L 六档,禁用品质/阶级/品阶等叫法
metadata:
  type: reference
---

# 稀有度(Rarity)术语约定

**全项目统一称呼:「稀有度」(rarity),禁止用「品质/阶级/品阶/品级」等词指代同一概念。**

- **代码**:`RarityEnum`(N=1/R=2/SR=3/SSR=4/UR=5/L=6,定义于 `Assets/Scripts/Enums/GameStateEnum.cs`)、`RarityInfo` 配置表、Bean 字段统一 `rarity`。
- **魔王专属档**:`RarityEnum.DemonLord=999`(自 `Assets/Scripts/Enums/GameStateEnum.cs`)。**非真实档位、仅魔王标记**:由 `IsDemonLord()` 判定,只在图鉴/卡面按 999 取 `RarityInfo` 的深黑+暗紫红配色;**不参与稀有度排序/进阶/BUFF/CMP 等数值比较**(那些仍按 `rarity<=0` 归 N)。`RarityInfo` 表新增 id=999 行(语言 name 引用 id=7)。
- **中文档名**:普通/稀有/特稀/极稀/绝稀/传奇;英文档名:N/R/SR/SSR/UR/L(见 `Language_RarityInfo_*.txt`)。
- **配色**:`RarityInfo.ui_board_color`(UI底板)/`RarityInfo.buff_color`(BUFF配色,UIViewBuffShowItem 与进阶预览同口径)。
- **注意区分**:「稀有」(R 档档名)是「稀有度」的一个档位,两者语义不冲突;「进阶/升阶」是提升稀有度的**流程名**,不要因此把稀有度本身叫「阶级」。
- **魔王特例**:`CreatureBean.rarity=0`(IsDemonLord),代码里 `rarity≤0` 一律按 N(1) 处理(如 `RarityInfoCfg.GetAscendTimeByRarity`)。
- 相关 UI 文本:UIText 2000018「稀有度」、80013「进阶成功,稀有度提升!」、4000020「随机生成一条该稀有度增益」(进阶 BUFF 预览兜底文案)。
