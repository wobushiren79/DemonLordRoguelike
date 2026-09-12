using UnityEngine;

/// <summary>
/// 条件触发-伤害转移护盾（大盾战士BOSS「援护护盾」套给最前排友军的护盾）
/// <para>生效期间（trigger_value=5 秒）：目标零承伤——其受到的 UnderAttack 伤害在结算前拦截并原样改道给施加者（BOSS）
/// （拦截分支在 FightCreatureEntity.UnderAttack 方法头，靠 FightCreatureBean.damageTransferApplierId/damageTransferBuff 标记驱动）。</para>
/// <para>视觉：Effect_ShieldBubble_1（罩体+能量连线子功能合一，连线为子节点 Line、由 EffectShieldBubble 统一驱动）走「战斗杂项预制」通道
/// （FightManager.GetFightPrefabCommon + dicPoolFightObj 按路径分桶缓存池），挂为目标生物子节点（罩体跟随目标，连线两端点由本 BUFF 每帧喂给 bubble.SetLinkEndpoints：目标胸口→施加者胸口，脉冲流向代受者）；
/// 存续期前 4/5 罩体保持完整，最后 1/5 才随已流逝时间渐进溶解（SetLifetimePercent，上限 EffectShieldBubble.AgingDissolveMax 不裁没），受击罩体连线同闪（PlayHitFlash），
/// 到期/施加者死亡时 PlayBreak 罩体溶解推满+连线淡出后回池。</para>
/// <para>施加者（BOSS）死亡/离场 → 护盾立即破碎、目标当帧恢复承伤；目标死亡/战斗结束走 ClearData 立即回池（不播溶解）。</para>
/// <para>堆叠策略须配 Ignore（重复施加直接忽略）：标记字段是单值的，多实例会互相覆盖。</para>
/// <para>class_entity_data 格式："泡泡特效ID(EffectInfo),基础半径"（如 "500004,0.6"；实际半径=基础半径×目标体型缩放）。</para>
/// </summary>
public class BuffEntityConditionalShieldTransfer : BuffEntityConditional
{
    #region 常量
    /// <summary>连线端点高度（生物胸口，世界单位）</summary>
    protected const float LinkEndpointHeight = 0.5f;
    /// <summary>连线宽度（世界单位）</summary>
    protected const float LinkWidth = 0.12f;
    #endregion

    #region 字段
    /// <summary>泡泡特效ID（class_entity_data[0]，EffectInfo 表）</summary>
    protected long bubbleEffectId;
    /// <summary>泡泡基础半径（class_entity_data[1]，世界单位）</summary>
    protected float bubbleBaseRadius;
    /// <summary>参数是否解析成功（class_entity_data 只解析一次）</summary>
    protected bool isDataParsed;
    /// <summary>泡泡杂项预制实体（战斗杂项缓存池复用，gameObject 挂为目标子节点跟随；含连线子节点 Line）</summary>
    protected FightPrefabEntity bubblePrefabEntity;
    /// <summary>泡泡驱动组件（含连线子功能驱动）</summary>
    protected EffectShieldBubble bubble;
    /// <summary>是否处于破碎溶解中（溶解播完才置 isValid=false 并回池）</summary>
    protected bool isBreaking;
    /// <summary>破碎溶解计时（对齐 EffectShieldBubble.DissolveTime，连线淡出 LinkFadeOutTime 更短由组件内部并行处理）</summary>
    protected float breakTimer;
    #endregion

    #region 数据相关
    /// <summary>
    /// 设置数据：解析参数 → 给目标写入伤害转移标记 → 生成泡泡
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        ParseEntityData();
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null)
            return;
        //写入伤害转移标记（拦截分支凭此改道；damageTransferBuff 回指本实例供受击闪白，避免受击时扫描BUFF列表）
        targetCreature.fightCreatureData.damageTransferApplierId = buffEntityData.applierCreatureUUId;
        targetCreature.fightCreatureData.damageTransferBuff = this;
        SpawnBubble(targetCreature);
    }

    /// <summary>
    /// 清理数据：兜底清标记+泡泡（含连线）立即回池（外部移除路径：目标死亡清BUFF/战斗结束等，不播溶解）
    /// </summary>
    public override void ClearData()
    {
        //先处理自身状态再调 base（base.ClearData 会把 buffEntityData 置 null，届时将找不到目标）
        ClearTransferMark();
        RecycleBubble();
        bubbleEffectId = 0;
        bubbleBaseRadius = 0;
        isDataParsed = false;
        isBreaking = false;
        breakTimer = 0;
        base.ClearData();
    }
    #endregion

    #region Update
    /// <summary>
    /// buff持续时间增加：驱动泡泡（含连线）动画与老化消融、连线端点跟随；施加者死亡提前破碎；到期破碎；溶解播完才失效
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (buffEntityData == null || buffEntityData.isValid == false) return;
        if (bubble != null)
        {
            bubble.UpdateView(buffTime);
            UpdateLinkEndpoints();
        }
        //破碎溶解中：播完溶解才真正移除并回池（标记在破碎开始时已清，目标已恢复承伤）
        if (isBreaking)
        {
            breakTimer += buffTime;
            if (breakTimer >= EffectShieldBubble.DissolveTime)
            {
                RecycleBubble();
                buffEntityData.isValid = false;
            }
            return;
        }
        //施加者（BOSS）死亡/离场：护盾立即破碎
        var applierCreature = GetFightCreatureEntityForApplier();
        if (applierCreature == null || applierCreature.fightCreatureData == null || applierCreature.IsDead())
        {
            StartBreak();
            return;
        }
        //存续期老化消融：随已流逝时间渐进溶解（上限 AgingDissolveMax 不裁没，到期由 PlayBreak 推满）
        if (bubble != null)
        {
            float duration = buffEntityData.GetTriggerValue();
            if (duration > 0)
                bubble.SetLifetimePercent(buffEntityData.timeUpdateTotal / duration);
        }
        //到期破碎
        if (buffEntityData.timeUpdateTotal >= buffEntityData.GetTriggerValue())
        {
            StartBreak();
        }
    }
    #endregion

    #region 护盾表现
    /// <summary>
    /// 受击闪白（伤害被拦截转移时由 FightCreatureEntity.UnderAttack 拦截分支调用；罩体与连线同闪强化转移反馈）
    /// </summary>
    public void PlayHitFlash()
    {
        if (bubble != null && !isBreaking)
            bubble.PlayHitFlash();
    }

    /// <summary>
    /// 开始破碎：清标记（目标当帧恢复承伤）→ 罩体从当前溶解度推满 + 连线同步淡出（组件内部处理）→ 进入破碎态（溶解播完才置失效并回池）
    /// </summary>
    protected void StartBreak()
    {
        if (isBreaking) return;
        isBreaking = true;
        breakTimer = 0;
        ClearTransferMark();
        if (bubble != null)
            bubble.PlayBreak();
        else
            buffEntityData.isValid = false;//无泡泡可播时直接失效
    }

    /// <summary>
    /// 生成泡泡：走战斗杂项预制通道（dicPoolFightObj 按路径分桶缓存池），挂为目标子节点自动跟随，按目标体型缩放半径+初始化连线
    /// </summary>
    protected void SpawnBubble(FightCreatureEntity targetCreature)
    {
        if (!isDataParsed) return;
        if (targetCreature.creatureObj == null) return;
        var effectInfo = EffectInfoCfg.GetItemData(bubbleEffectId);
        if (effectInfo == null)
        {
            LogUtil.LogError($"伤害转移护盾BUFF[{buffEntityData.buffId}]找不到特效配置：{bubbleEffectId}");
            return;
        }
        string bubblePath = $"Assets/LoadResources/Effects/{effectInfo.res_name}.prefab";
        //GetFightPrefabCommon 内部同步加载并回调，无跨帧风险；杂项实体每帧由 FightHandler 驱动 Update，战斗结束 FightManager.Clear 统一销毁
        FightHandler.Instance.manager.GetFightPrefabCommon(bubblePath, (prefabEntity) =>
        {
            //pathAsstes 必须赋值才能正确回池分桶（RemoveFightPrefabCommon 按它入队）
            prefabEntity.pathAsstes = bubblePath;
            bubblePrefabEntity = prefabEntity;
            //挂为目标子节点自动跟随（取出时 SetActive(true) 会触发 EffectShieldBubble.OnEnable 自动重置视觉，池化安全）
            var bubbleTransform = prefabEntity.gameObject.transform;
            bubbleTransform.SetParent(targetCreature.creatureObj.transform, false);
            bubbleTransform.localPosition = Vector3.zero;
            bubble = prefabEntity.gameObject.GetComponent<EffectShieldBubble>();
            if (bubble == null)
            {
                LogUtil.LogError($"伤害转移护盾BUFF[{buffEntityData.buffId}]特效 {effectInfo.res_name} 没有 EffectShieldBubble 组件");
                RecycleBubble();
                return;
            }
            float radius = bubbleBaseRadius * targetCreature.fightCreatureData.creatureData.GetBodySizeScale();
            bubble.SetRadius(radius);
            //球心抬升半径高度使罩体底部贴地（正球半高=半径）
            if (bubble.bubbleRoot != null)
                bubble.bubbleRoot.localPosition = new Vector3(0, radius, 0);
            //连线子功能：宽度 + 初始端点
            bubble.SetLinkWidth(LinkWidth);
            UpdateLinkEndpoints();
        });
    }

    /// <summary>
    /// 刷新连线两端点（目标胸口 → 施加者胸口；任一端失效则跳过，等回收路径处理）
    /// </summary>
    protected void UpdateLinkEndpoints()
    {
        if (bubble == null) return;
        var targetCreature = GetFightCreatureEntityForTarget();
        var applierCreature = GetFightCreatureEntityForApplier();
        if (targetCreature == null || targetCreature.creatureObj == null || applierCreature == null || applierCreature.creatureObj == null)
            return;
        Vector3 startPos = targetCreature.creatureObj.transform.position + new Vector3(0, LinkEndpointHeight, 0);
        Vector3 endPos = applierCreature.creatureObj.transform.position + new Vector3(0, LinkEndpointHeight, 0);
        bubble.SetLinkEndpoints(startPos, endPos);
    }

    /// <summary>
    /// 泡泡回池（战斗杂项缓存池复用，不销毁；连线作为其子节点一并回池）
    /// </summary>
    protected void RecycleBubble()
    {
        if (bubblePrefabEntity == null) return;
        //防御：gameObject 已被外部销毁（战斗结束 FightManager.Clear 先跑）则不再回池
        if (bubblePrefabEntity.gameObject != null)
        {
            //回池前摘出目标层级并隐藏（RemoveFightPrefabCommon 不处理显隐/父级，取出时才 SetActive(true)）
            bubblePrefabEntity.gameObject.transform.SetParent(FightHandler.Instance.manager.gameObject.transform, false);
            bubblePrefabEntity.gameObject.SetActive(false);
            //回池（SetState(None)+按 pathAsstes 分桶入队）
            bubblePrefabEntity.Destroy();
        }
        bubblePrefabEntity = null;
        bubble = null;
    }

    /// <summary>
    /// 清除目标身上的伤害转移标记（防御性校验：仅当标记回指本实例时才清，防误清新护盾的标记）
    /// </summary>
    protected void ClearTransferMark()
    {
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null) return;
        if (targetCreature.fightCreatureData.damageTransferBuff == this)
        {
            targetCreature.fightCreatureData.damageTransferApplierId = null;
            targetCreature.fightCreatureData.damageTransferBuff = null;
        }
    }

    /// <summary>
    /// 解析 class_entity_data（"泡泡特效ID,基础半径"）
    /// </summary>
    protected void ParseEntityData()
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"伤害转移护盾BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"泡泡特效ID,基础半径\"：{buffInfo.class_entity_data}");
            return;
        }
        bubbleEffectId = long.Parse(arrEntityData[0]);
        bubbleBaseRadius = float.Parse(arrEntityData[1]);
        isDataParsed = true;
    }
    #endregion
}
