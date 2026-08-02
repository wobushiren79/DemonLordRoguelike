using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-环绕书本攻击（深渊馈赠「知识的力量」）
/// <para>常驻效果：随机选取一只最前排（x 最大）的己方存活魔物作为宿主，其周围 XZ 水平面环绕 N 本书（半径 0.75，
/// 高度取魔王位置+攻击起始偏移高度，与回旋镖发射高度同一惯例），
/// 书触碰到的敌人受到魔王实时攻击力×trigger_value 的伤害（每只书对同一敌人 0.5 秒冷却，由 AttackModeOrbit 自身处理）。</para>
/// <para>宿主规则：宿主死亡 → 自动改选当前最前排魔物；新放置的魔物位置更前排(x 更大) → 改选到更前排魔物；
/// 同一最前排 x 并列多只时随机选 1 只（并列不换宿主防抖动）；场上无己方魔物 → 销毁全部书本，重新有魔物时再生成。</para>
/// <para>书本是常驻攻击模块 AttackModeOrbit（不自毁），由本 BUFF 负责生成/销毁/宿主同步；
/// 关卡切换致书本被回收时下一帧自动整套重建（保证角度均分）；BUFF 清理（升级替换/馈赠清空）时全部销毁。</para>
/// <para>class_entity_data 格式："书本数,攻击模块ID,旋转速度(弧度/秒)"（如 "3,300071,3"）。
/// trigger_time 配极大值使周期触发永不发生（本 BUFF 只靠 UpdateBuffTime 每帧驱动环绕管理，不做周期触发）。</para>
/// </summary>
public class BuffEntityPeriodicAttackOrbit : BuffEntityPeriodic
{
    #region 字段
    /// <summary>环绕书本数量（class_entity_data[0]）</summary>
    protected int bookCount;
    /// <summary>书本攻击模块ID（class_entity_data[1]）</summary>
    protected long attackModeId;
    /// <summary>旋转角速度（弧度/秒，class_entity_data[2]）</summary>
    protected float rotateSpeed;
    /// <summary>当前环绕中的书本（常驻攻击模块，由本 BUFF 管理生命周期）</summary>
    protected readonly List<AttackModeOrbit> listOrbitBook = new List<AttackModeOrbit>();
    /// <summary>当前宿主（最前排己方魔物）</summary>
    protected FightCreatureEntity hostCreature;
    /// <summary>参数是否解析成功（class_entity_data 只解析一次）</summary>
    protected bool isDataParsed;
    /// <summary>宿主 x 并列判定容差（世界单位）</summary>
    protected const float HostXEpsilon = 0.001f;
    #endregion

    #region 数据相关
    /// <summary>
    /// 设置数据：解析 class_entity_data（"书本数,攻击模块ID,旋转速度"）
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 3)
        {
            LogUtil.LogError($"环绕书本BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"书本数,攻击模块ID,旋转速度\"：{buffInfo.class_entity_data}");
            return;
        }
        bookCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);
        rotateSpeed = float.Parse(arrEntityData[2]);
        isDataParsed = true;
    }

    /// <summary>
    /// 清理数据：销毁全部环绕书本（对象池复用/升级替换/馈赠清空时走这里）
    /// </summary>
    public override void ClearData()
    {
        DestroyAllBooks();
        hostCreature = null;
        bookCount = 0;
        attackModeId = 0;
        rotateSpeed = 0;
        isDataParsed = false;
        base.ClearData();
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧驱动：刷新宿主（最前排变更/死亡重选/无魔物清空）→ 书本整套重建校验 → 同步宿主给书本
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (!isDataParsed)
            return;
        RefreshHost();
        if (hostCreature == null)
        {
            // 场上无己方魔物：销毁全部书本
            if (listOrbitBook.Count > 0)
                DestroyAllBooks();
            return;
        }
        // 书本缺失/失效（首次生成、关卡切换被回收等）则整套重建，保证环绕角度均分
        if (CheckAnyBookInvalid())
        {
            DestroyAllBooks();
            SpawnBooks();
        }
        // 同步宿主给所有书本：宿主变更时书本环绕中心随之切换，书本实体不变
        SyncHostToBooks();
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期触发禁用：本 BUFF 是常驻环绕管理，不做周期触发（trigger_time 配极大值本就到不了这里，兜底拦截防误配）
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        return false;
    }
    #endregion

    #region 宿主选择
    /// <summary>
    /// 刷新宿主：宿主死亡或出现更前排(x 更大)魔物时重选（最前排并列者中随机 1 只）；无存活魔物则宿主置空
    /// </summary>
    protected void RefreshHost()
    {
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        var listDefenseEntity = gameFightLogic?.fightData?.dlDefenseCreatureEntity?.List;
        if (listDefenseEntity == null)
        {
            hostCreature = null;
            return;
        }
        // 找存活魔物的最前排 x，并确认宿主是否还存活
        float maxX = float.MinValue;
        bool isHostAlive = false;
        for (int i = 0; i < listDefenseEntity.Count; i++)
        {
            var itemEntity = listDefenseEntity[i];
            if (itemEntity == null || itemEntity.creatureObj == null || itemEntity.IsDead())
                continue;
            float x = itemEntity.creatureObj.transform.position.x;
            if (x > maxX)
                maxX = x;
            if (itemEntity == hostCreature)
                isHostAlive = true;
        }
        // 无存活魔物
        if (maxX == float.MinValue)
        {
            hostCreature = null;
            return;
        }
        // 宿主仍存活且没有更前排的魔物 → 保持原宿主（同 x 并列不换，避免宿主抖动）
        if (isHostAlive && hostCreature != null && maxX <= hostCreature.creatureObj.transform.position.x + HostXEpsilon)
            return;
        // 重选：从最前排 x 并列者中随机选 1 只
        FightCreatureEntity pickCreature = null;
        int candidateCount = 0;
        for (int i = 0; i < listDefenseEntity.Count; i++)
        {
            var itemEntity = listDefenseEntity[i];
            if (itemEntity == null || itemEntity.creatureObj == null || itemEntity.IsDead())
                continue;
            float x = itemEntity.creatureObj.transform.position.x;
            if (x < maxX - HostXEpsilon)
                continue;
            // 蓄水池抽样：遍历一遍等概率随机取 1 只，无需分配候选列表
            candidateCount++;
            if (UnityEngine.Random.Range(0, candidateCount) == 0)
                pickCreature = itemEntity;
        }
        hostCreature = pickCreature;
    }
    #endregion

    #region 书本管理
    /// <summary>
    /// 检测是否有书本缺失或失效（数量不符/已回收），是则整套重建
    /// </summary>
    protected bool CheckAnyBookInvalid()
    {
        if (listOrbitBook.Count != bookCount)
            return true;
        for (int i = 0; i < listOrbitBook.Count; i++)
        {
            if (listOrbitBook[i] == null || !listOrbitBook[i].isValid)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 生成整套环绕书本（角度按 2π/N 均分）：纯数据发射路径，宿主/转速/伤害倍率逐本注入
    /// </summary>
    protected void SpawnBooks()
    {
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null)
            return;
        if (hostCreature == null || hostCreature.creatureObj == null)
            return;
        // 环绕高度：魔王位置 + CreatureInfo 攻击起始位置 + 攻击模块自身偏移（与回旋镖发射高度同一惯例）
        Vector3 startPosOffset = coreCreature.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo != null)
            startPosOffset += attackModeInfo.GetStartPosOffset();
        float orbitHeight = coreCreature.creatureObj.transform.position.y + startPosOffset.y;
        var fightManager = FightHandler.Instance.manager;
        float damageRate = buffEntityData.GetTriggerValue();
        for (int i = 0; i < bookCount; i++)
        {
            AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
            attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
            // 伤害命中瞬间实时结算（AttackModeOrbit.HandleForHitTarget），这里先置 0
            attackModeData.attackerDamage = 0;
            attackModeData.attackerCRT = 0;
            attackModeData.startPos = hostCreature.creatureObj.transform.position;
            attackModeData.targetPos = attackModeData.startPos;
            attackModeData.attackedId = "";
            attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
            attackModeData.attackDirection = Vector3.right;
            float orbitAngle = Mathf.PI * 2f * i / bookCount;
            fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
            {
                attackMode.StartAttackInit(attackModeData);
                if (attackMode is AttackModeOrbit orbitBook)
                {
                    orbitBook.orbitCenterEntity = hostCreature;
                    orbitBook.orbitAngle = orbitAngle;
                    orbitBook.rotateSpeed = rotateSpeed;
                    orbitBook.damageRate = damageRate;
                    orbitBook.orbitHeight = orbitHeight;
                    listOrbitBook.Add(orbitBook);
                }
                attackMode.StartAttack();
            });
        }
    }

    /// <summary>
    /// 销毁全部环绕书本（回对象池）并清空列表
    /// </summary>
    protected void DestroyAllBooks()
    {
        for (int i = 0; i < listOrbitBook.Count; i++)
        {
            if (listOrbitBook[i] != null && listOrbitBook[i].isValid)
                listOrbitBook[i].Destroy();
        }
        listOrbitBook.Clear();
    }

    /// <summary>
    /// 把当前宿主同步给所有书本（宿主变更时书本环绕中心随之切换）
    /// </summary>
    protected void SyncHostToBooks()
    {
        for (int i = 0; i < listOrbitBook.Count; i++)
        {
            if (listOrbitBook[i] != null)
                listOrbitBook[i].orbitCenterEntity = hostCreature;
        }
    }
    #endregion
}
