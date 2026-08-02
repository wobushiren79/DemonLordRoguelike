using UnityEngine;

/// <summary>
/// 周期型BUFF-冲击波发射（深渊馈赠「第六次冲击」）
/// <para>每 trigger_time(10) 秒触发一轮：从防守核心（魔王）位置+攻击起始偏移处发出一道圆环冲击波（AttackModeShockwaveRing），
/// 半径扩张至覆盖整条道路，扫到的敌人受到伤害并击退（扩张/命中/击退由攻击模块处理）。</para>
/// <para>伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（发射时实时取，保底1点），不暴击；
/// 场上无存活敌人时本轮不触发。</para>
/// <para>class_entity_data 格式："攻击模块ID"（如 "300091"；5 级共用同一攻击模块，等级差异在 trigger_value 伤害倍率）。</para>
/// </summary>
public class BuffEntityPeriodicAttackShockwave : BuffEntityPeriodic
{
    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制：检测存活敌人（无则跳过本轮）→ 从魔王位置发射一道冲击波
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (!isTriggerSuccess) return false;

        // 场上无存活敌人则本轮不触发（矿车先例）
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listEnemy = gameFightLogic.fightData.dlAttackCreatureEntity.List;
        if (listEnemy.IsNull()) return false;
        bool hasAliveEnemy = false;
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var enemy = listEnemy[i];
            if (enemy != null && !enemy.IsDead())
            {
                hasAliveEnemy = true;
                break;
            }
        }
        if (!hasAliveEnemy) return false;

        // 解析参数 "攻击模块ID"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 1 || !long.TryParse(arrEntityData[0], out long attackModeId) || attackModeId == 0)
        {
            LogUtil.LogError($"冲击波BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"攻击模块ID\"：{buffInfo.class_entity_data}");
            return false;
        }

        // 伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（发射时实时取，保底1点）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null) return false;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = Mathf.Max(1, (int)(coreATK * buffEntityData.GetTriggerValue()));

        // 圆心 = 魔王生物位置 + CreatureInfo 攻击起始位置 + 攻击模块自身偏移（与回旋镖发射点对齐，粒子从此处发出）
        Vector3 startPosOffset = coreCreature.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo != null)
            startPosOffset += attackModeInfo.GetStartPosOffset();
        Vector3 centerPos = coreCreature.creatureObj.transform.position + startPosOffset;

        // 纯数据发射路径（照矿车/闪电/回旋镖先例）：注入攻击者快照与圆心
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        // 不暴击（保持闪电/矿车/回旋镖的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = centerPos;
        attackModeData.targetPos = centerPos;
        attackModeData.attackedId = "";
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        //默认受击朝向（命中时攻击模块会按「圆心→敌人」逐目标刷新）
        attackModeData.attackDirection = Vector3.right;
        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
        });
        return true;
    }
    #endregion
}
