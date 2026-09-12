using System.Collections.Generic;

/// <summary>
/// 周期型BUFF-援护护盾施放（大盾战士BOSS技能，经 CreatureInfo.creature_buff 出生自带，applier=target=BOSS自己）
/// <para>每 trigger_time(10) 秒触发一次：搜索全场最前排的 N 个同阵营存活生物（排除自己；进攻方前排=x最小=最靠近敌方阵地，防守方前排=x最大），
/// 为每个目标挂上 class_entity_data[1] 指定的护盾BUFF（伤害转移护盾）。</para>
/// <para>计时由 BuffHandler.UpdateData 每帧驱动，与AI意图/出手点无关（走路/硬直中也计时），随暂停/倍速同步。</para>
/// <para>class_entity_data 格式："目标数量,护盾BUFF的ID"（如 "3,2000700001"）。</para>
/// </summary>
public class BuffEntityPeriodicShieldCast : BuffEntityPeriodic
{
    #region 字段
    /// <summary>单次套盾目标数量（class_entity_data[0]）</summary>
    protected int shieldCount;
    /// <summary>护盾BUFF的ID（class_entity_data[1]）</summary>
    protected long shieldBuffId;
    /// <summary>参数是否解析成功（class_entity_data 只解析一次）</summary>
    protected bool isDataParsed;
    #endregion

    #region 数据相关
    /// <summary>
    /// 设置数据：解析 class_entity_data（"目标数量,护盾BUFF的ID"）
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"援护护盾施放BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"目标数量,护盾BUFF的ID\"：{buffInfo.class_entity_data}");
            return;
        }
        shieldCount = int.Parse(arrEntityData[0]);
        shieldBuffId = long.Parse(arrEntityData[1]);
        isDataParsed = true;
    }

    /// <summary>
    /// 清理数据（对象池复用前清空参数，防残留）
    /// </summary>
    public override void ClearData()
    {
        shieldCount = 0;
        shieldBuffId = 0;
        isDataParsed = false;
        base.ClearData();
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制：选取最前排 N 个同阵营存活生物（排除自己）并逐个套上护盾BUFF；无存活友军则本轮不触发
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (!isTriggerSuccess) return false;
        if (!isDataParsed) return false;
        //出生自带BUFF：持有者即BOSS自己（applier==target）
        var selfCreature = GetFightCreatureEntityForTarget();
        if (selfCreature == null || selfCreature.fightCreatureData == null || selfCreature.IsDead()) return false;
        var fightType = selfCreature.fightCreatureData.creatureFightType;
        //只支援进攻/防守阵营（魔王核心等不触发）
        if (fightType != CreatureFightTypeEnum.FightAttack && fightType != CreatureFightTypeEnum.FightDefense) return false;
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        var listAlly = fightType == CreatureFightTypeEnum.FightAttack
            ? gameFightLogic?.fightData?.dlAttackCreatureEntity?.List
            : gameFightLogic?.fightData?.dlDefenseCreatureEntity?.List;
        //选取最前排（前排方向由阵营决定），排除自己
        var listFrontRow = FightCreatureSearchUtil.FindFrontRowCreatures(listAlly, shieldCount, selfCreature, fightType);
        if (listFrontRow.IsNull()) return false;
        string selfUUId = selfCreature.fightCreatureData.creatureData.creatureUUId;
        for (int i = 0; i < listFrontRow.Count; i++)
        {
            var itemTarget = listFrontRow[i];
            BuffHandler.Instance.AddFightCreatureBuff(new List<BuffBean>() { new BuffBean(shieldBuffId) },
                selfUUId, itemTarget.fightCreatureData.creatureData.creatureUUId);
        }
        return true;
    }
    #endregion
}
