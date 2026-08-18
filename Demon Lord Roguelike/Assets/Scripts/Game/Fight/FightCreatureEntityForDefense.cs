using UnityEngine;

/// <summary>
/// 战斗生物实体-防守生物专属逻辑
/// <para>防守生物：玩家放置在道路上抵御进攻的魔物（CreatureFightTypeEnum.FightDefense）。</para>
/// </summary>
public partial class FightCreatureEntity
{
    #region 防守生物-死亡相关
    /// <summary>
    /// 死亡意图切换-防守生物（由 SetCreatureDead 统一分发 非防守生物自动跳过）
    /// <para>冲锋自爆型生物（charge_attack=1）死亡即引爆：先在死亡位置创建爆炸攻击模块，再切死亡意图；
    /// 遇敌/到路尽头（冲锋意图主动调用 SetCreatureDead）与被打死（CheckDead）都汇聚到本入口，配合 SetCreatureDead 的 IsDead 守卫只爆一次。</para>
    /// </summary>
    public void SetCreatureDeadForDefense()
    {
        if (aiEntity is AIDefenseCreatureEntity)
        {
            //冲锋自爆型：死亡即引爆（此时本生物已标记死亡，不能走 StartAttack(attacker,…) 路径——其对已死攻击者直接销毁不爆）
            if (fightCreatureData.creatureData.creatureInfo.IsChargeAttack())
            {
                CreateAttackModeForDeadExplosion();
            }
            aiEntity.ChangeIntent(AIIntentEnum.DefenseCreatureDead);
        }
    }

    /// <summary>
    /// 死亡引爆：在死亡位置创建本生物 attack_mode 配置的爆炸攻击模块（纯数据无参路径，照冲击波/矿车先例手动注入攻击者快照），
    /// 伤害 = ATK × 攻击模式伤害倍率（与正常攻击同公式），仅冲锋自爆型生物（charge_attack=1）使用
    /// </summary>
    public void CreateAttackModeForDeadExplosion()
    {
        var creatureData = fightCreatureData.creatureData;
        var creatureInfo = creatureData.creatureInfo;
        long attackModeId = creatureInfo.attack_mode;
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo == null)
            return;
        //爆炸点 = 生物位置 + 生物攻击起始位置 + 攻击模块自身偏移（与正常攻击起点同口径）
        Vector3 explosionPos = creatureObj.transform.position + creatureInfo.GetAttackStartPosition() + attackModeInfo.GetStartPosOffset();
        //纯数据发射路径（照冲击波/矿车先例）：注入攻击者快照与爆炸点
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerCreatureId = creatureInfo.id;
        var weaponItemData = creatureData.GetEquip(ItemTypeEnum.Weapon);
        attackModeData.attackerWeaponItemId = weaponItemData != null ? weaponItemData.itemId : creatureInfo.GetEquipBaseWeaponId();
        attackModeData.attackerId = creatureData.creatureUUId;
        attackModeData.attackerDamage = (int)(fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK) * attackModeInfo.GetDamageAddRate());
        attackModeData.attackerCRT = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.CRT);
        attackModeData.attackerCDMG = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.CDMG);
        attackModeData.startPos = explosionPos;
        attackModeData.targetPos = explosionPos;
        attackModeData.attackedId = "";
        attackModeData.attackedLayerTarget = fightCreatureData.GetCreatureLayer(true);
        //默认受击朝向（爆炸为自身范围检测，方向不参与命中判定）
        attackModeData.attackDirection = Vector3.right;
        FightHandler.Instance.StartCreateAttackMode(attackModeData);
    }
    #endregion
}
