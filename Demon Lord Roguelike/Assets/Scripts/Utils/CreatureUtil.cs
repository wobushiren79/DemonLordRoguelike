using System;
using System.Collections.Generic;
using UnityEngine;

public static class CreatureUtil
{
    #region 生物献祭
    /// <summary>
    /// 计算一批祭品对目标生物的"献祭成功率(祭品部分,不含保底)"。
    /// <para>规则: 单个相同生物且相同稀有度的祭品基础成功率 = 1 / sacrificeNum;</para>
    /// <para>1. 祭品与目标生物 id 不同: 单个成功率再 ×1/10;</para>
    /// <para>2. 祭品稀有度低于目标: 每低一级再 ×1/10(差2级即 ×1/100),与 id 惩罚可叠加;</para>
    /// <para>3. 全部祭品成功率累加;相同 id 且相同稀有度时,祭品数量达到 sacrificeNum 即可累加到 100%。</para>
    /// <para>注: 返回值不在此截顶,保底叠加后再统一 Clamp01。</para>
    /// </summary>
    /// <param name="targetCreature">接受献祭(升级)的目标生物</param>
    /// <param name="listFodder">作为祭品的生物列表</param>
    /// <param name="sacrificeNum">该等级所需祭品基础数量(来自 LevelInfo.sacrifice_num)</param>
    /// <returns>祭品部分累加成功率(未截顶,未含保底)</returns>
    public static float GetSacrificeFoddersRate(CreatureBean targetCreature, List<CreatureBean> listFodder, int sacrificeNum)
    {
        if (targetCreature == null || listFodder == null || listFodder.Count == 0)
            return 0f;
        if (sacrificeNum <= 0)
            sacrificeNum = 5;
        //单个相同生物、相同稀有度祭品的基础成功率
        float baseSingleRate = 1f / sacrificeNum;
        float totalRate = 0f;
        for (int i = 0; i < listFodder.Count; i++)
        {
            var fodder = listFodder[i];
            if (fodder == null)
                continue;
            float rate = baseSingleRate;
            //生物 id 不同: 再降一个数量级
            if (fodder.creatureId != targetCreature.creatureId)
                rate *= 0.1f;
            //稀有度低于目标: 每低一级再降一个数量级(可与 id 惩罚叠加)
            int rarityDiff = targetCreature.rarity - fodder.rarity;
            if (rarityDiff > 0)
                rate *= Mathf.Pow(0.1f, rarityDiff);
            totalRate += rate;
        }
        return totalRate;
    }

    /// <summary>
    /// 计算目标生物本次献祭的最终成功率(保底 + 祭品,统一截顶到 100%)。
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
        //祭品成功率 + 保底成功率
        float foddersRate = GetSacrificeFoddersRate(targetCreature, listFodder, sacrificeNum);
        float totalRate = targetCreature.sacrificePityRate + foddersRate;
        return Mathf.Clamp01(totalRate);
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
