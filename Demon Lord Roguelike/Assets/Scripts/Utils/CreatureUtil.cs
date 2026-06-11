using System;
using System.Collections.Generic;
using UnityEngine;

public static class CreatureUtil
{
    #region 生物献祭
    /// <summary>
    /// 计算一批祭品对目标生物的"献祭成功率(祭品部分,不含保底)"。
    /// <para>规则: 单个「相同生物id」祭品的基础成功率 = 1 / sacrificeNum;</para>
    /// <para>1. 祭品与目标生物 id 不同: 单个成功率 = differentIdRate(来自「不同生物id献祭成功率提升」研究,未解锁为0);</para>
    /// <para>2. 等级差修正(替代旧的稀有度惩罚,不再判定稀有度): 修正系数 = 2^(祭品等级 − 目标当前等级),即祭品每高目标 1 级 ×2(高2级 ×4…),每低 1 级 ×0.5(低2级 ×0.25…),同级不变;同id/不同id均叠加;</para>
    /// <para>3. 全部祭品成功率累加;相同 id 且相同等级时,祭品数量达到 sacrificeNum 即可累加到 100%。</para>
    /// <para>注: 返回值不在此截顶,保底叠加后再统一 Clamp01。</para>
    /// </summary>
    /// <param name="targetCreature">接受献祭(升级)的目标生物</param>
    /// <param name="listFodder">作为祭品的生物列表</param>
    /// <param name="sacrificeNum">该等级所需祭品基础数量(来自 LevelInfo.sacrifice_num)</param>
    /// <param name="differentIdRate">单个「不同生物id」祭品的成功率(来自研究 UnlockEnum.SacrificeDifferentIdRate,未解锁为0)</param>
    /// <returns>祭品部分累加成功率(未截顶,未含保底)</returns>
    public static float GetSacrificeFoddersRate(CreatureBean targetCreature, List<CreatureBean> listFodder, int sacrificeNum, float differentIdRate)
    {
        if (targetCreature == null || listFodder == null || listFodder.Count == 0)
            return 0f;
        if (sacrificeNum <= 0)
            sacrificeNum = 5;
        //单个相同生物id祭品的基础成功率
        float baseSingleRate = 1f / sacrificeNum;
        float totalRate = 0f;
        for (int i = 0; i < listFodder.Count; i++)
        {
            var fodder = listFodder[i];
            if (fodder == null)
                continue;
            //同id用基础成功率;不同id用研究加成(默认0)
            float rate = fodder.creatureId == targetCreature.creatureId ? baseSingleRate : differentIdRate;
            //等级差修正(替代稀有度判定): 修正系数=2^(祭品等级-目标当前等级),祭品每高1级×2、每低1级×0.5、同级不变
            int levelDiff = fodder.level - targetCreature.level;
            rate *= Mathf.Pow(2f, levelDiff);
            totalRate += rate;
        }
        return totalRate;
    }

    /// <summary>
    /// 计算目标生物本次献祭的最终成功率(保底 + 祭品,统一截顶到 100%)。
    /// 不同生物id祭品的成功率由研究「不同生物id献祭成功率提升」(UnlockEnum.SacrificeDifferentIdRate)决定,未解锁时为0。
    /// </summary>
    /// <param name="targetCreature">接受献祭(升级)的目标生物</param>
    /// <param name="listFodder">作为祭品的生物列表</param>
    /// <returns>0~1 的最终成功率</returns>
    public static float GetSacrificeSuccessRate(CreatureBean targetCreature, List<CreatureBean> listFodder)
    {
        if (targetCreature == null)
            return 0f;
        //取下一级配置里的祭品基础数量
        int sacrificeNum = 5;
        var nextLevelInfo = targetCreature.GetNextLevelInfo();
        if (nextLevelInfo != null && nextLevelInfo.sacrifice_num > 0)
            sacrificeNum = nextLevelInfo.sacrifice_num;
        //读取「不同生物id献祭成功率提升」研究等级换算出的单个不同id祭品成功率
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        float differentIdRate = userUnlock.GetUnlockSacrificeDifferentIdRate();
        //祭品成功率 + 保底成功率
        float foddersRate = GetSacrificeFoddersRate(targetCreature, listFodder, sacrificeNum, differentIdRate);
        float totalRate = targetCreature.sacrificePityRate + foddersRate;
        return Mathf.Clamp01(totalRate);
    }
    #endregion

    #region 生物加点
    /// <summary>
    /// 获取「单个属性加点」对应增加的属性数值。
    /// <para>升级加点时, 每点对不同属性的增量不同: HP/护甲(DR) 每点 +10, 攻击(ATK)/攻速(ASPD) 每点 +1, 其余默认每点 +1。</para>
    /// </summary>
    /// <param name="attributeType">属性类型</param>
    /// <returns>单点该属性增加的数值</returns>
    public static float GetAttributePointAddValue(CreatureAttributeTypeEnum attributeType)
    {
        switch (attributeType)
        {
            case CreatureAttributeTypeEnum.HP:
                return 10;
            case CreatureAttributeTypeEnum.DR:
                return 10;
            case CreatureAttributeTypeEnum.ATK:
                return 1;
            case CreatureAttributeTypeEnum.ASPD:
                return 1;
            default:
                return 1;
        }
    }
    #endregion

    #region 生物皮肤
    /// <summary>
    /// 获取生物皮肤类型的多语言显示名称
    /// </summary>
    /// <param name="creatureSkinType">生物皮肤类型枚举</param>
    /// <returns>多语言名称；未匹配返回 "???"，Base 返回空串</returns>
    public static string GetCreatureSkinTypeEnumName(CreatureSkinTypeEnum creatureSkinType)
    {
        switch (creatureSkinType)
        {
            case CreatureSkinTypeEnum.Base:
                return "";
            case CreatureSkinTypeEnum.Head:
                return TextHandler.Instance.GetTextById(1001);
            case CreatureSkinTypeEnum.Hat:
                return TextHandler.Instance.GetTextById(1002);
            case CreatureSkinTypeEnum.Hair:
                return TextHandler.Instance.GetTextById(1003);
            case CreatureSkinTypeEnum.Body:
                return TextHandler.Instance.GetTextById(1004);
            case CreatureSkinTypeEnum.Eye:
                return TextHandler.Instance.GetTextById(1005);
            case CreatureSkinTypeEnum.Mouth:
                return TextHandler.Instance.GetTextById(1006);
        }
        return "???";
    }
    #endregion
}
