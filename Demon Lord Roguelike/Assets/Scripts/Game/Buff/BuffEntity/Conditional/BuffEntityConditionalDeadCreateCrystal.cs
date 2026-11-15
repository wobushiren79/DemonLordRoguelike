using System.Globalization;
using UnityEngine;

public class BuffEntityConditionalDeadCreateCrystal : BuffEntityConditionalDead
{
    #region 死亡掉落
    /// <summary>
    /// 死亡后按当前配置生成魔晶，每次掉落独立随机价值。
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        float chance = buffInfo.trigger_chance;
        if (chance > 0f && chance < 1f && Random.value >= chance)
            return false;
        var fightCreatureEntity = GetFightCreatureEntityForTarget();
        if (fightCreatureEntity == null)
            return false;
        ShowBuffEffect();
        var (min, max) = GetCrystalAmountRange(buffInfo);
        int crystalValue = Random.Range(min, max + 1);
        var fightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(crystalValue, fightCreatureEntity.fightCreatureData.positionDead);
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        fightDropCrystal.lifeTime = FightDropCrystalBean.BASE_LIFE_TIME + userUnlock.GetUnlockDropCrystalAddLifeTime();
        FightHandler.Instance.CreateDropCrystal(fightDropCrystal);

        return true;
    }

    /// <summary>
    /// 读取掉落价值的整数闭区间，供战斗和词条文本共用。
    /// </summary>
    public static (int min, int max) GetCrystalAmountRange(BuffInfoBean buffInfo)
    {
        var parts = buffInfo.class_entity_data?.Split(',');
        if (parts?.Length == 2
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int min)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int max)
            && min > 0 && max >= min && max < int.MaxValue)
        {
            return (min, max);
        }
        return (1, 1);
    }
    #endregion
}
