using UnityEngine;

/// <summary>
/// 条件触发-伤害转移护盾（大盾战士BOSS「援护护盾」套给友军的护盾）
/// <para>生效期间（trigger_value=9 秒）：目标按配置的转移比例承受伤害——其受到的 UnderAttack 伤害在结算前按比例拆分，
/// 转移部分改道给施加者（BOSS），剩余部分由目标自己继续结算（比例=1 时等同零承伤，比例=0.5 时目标自己吃一半）。
/// 拦截走 BuffBaseEntity.InterceptUnderAttack 通用钩子：FightCreatureEntity.UnderAttack 链式遍历目标身上的活跃BUFF逐个询问，
/// 本类状态（代受者/比例）只存自己身上，BUFF在=生效、BUFF移除=失效，无需在生物数据上另存标记。</para>
/// <para>视觉：Effect_ShieldBubble_1（罩体+能量连线子功能合一，连线为子节点 Line、由 EffectShieldBubble 统一驱动）走「战斗杂项预制」通道
/// （FightManager.GetFightPrefabCommon + dicPoolFightObj 按路径分桶缓存池），挂为目标生物子节点（罩体跟随目标，连线两端点由本 BUFF 每帧喂给 bubble.SetLinkEndpoints：目标胸口→施加者胸口，脉冲流向代受者）；
/// 存续期前 4/5 罩体保持完整，最后 1/5 才随已流逝时间渐进溶解（SetLifetimePercent，上限 EffectShieldBubble.AgingDissolveMax 不裁没），受击罩体连线同闪（PlayHitFlash），
/// 到期/施加者死亡时 PlayBreak 罩体溶解推满+连线淡出后回池。</para>
/// <para>施加者（BOSS）死亡/离场 → 护盾立即破碎、目标当帧恢复全额承伤；目标死亡/战斗结束走 ClearData 立即回池（不播溶解）。</para>
/// <para>堆叠策略须配 Ignore（重复施加直接忽略）：同一目标不需要两层同源护盾，多实例会让同一份伤害被重复分摊。</para>
/// <para>class_entity_data 格式："泡泡特效ID(EffectInfo),基础半径[,伤害转移比例]"（如 "500004,0.6,0.5"；实际半径=基础半径×目标体型缩放；
/// 转移比例 0~1 可省略，省略=1 全额改道；0.5=护盾只承受一半伤害，目标仍吃另一半）。</para>
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
    /// <summary>伤害转移比例（class_entity_data[2]，0~1，省略=1 全额改道；0.5=护盾只承受一半伤害）</summary>
    protected float damageTransferRate = 1f;
    /// <summary>代受者UUID（施加者，拦截时改道目标；SetData 从 buffEntityData.applierCreatureUUId 取，ClearData 清空）</summary>
    protected string transferApplierId;
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
    /// 设置数据：解析参数 → 记录代受者 → 生成泡泡
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        ParseEntityData();
        //代受者=施加者（拦截时改道目标）；BUFF移除即失效，不在生物数据上另存标记
        transferApplierId = buffEntityData.applierCreatureUUId;
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null)
            return;
        SpawnBubble(targetCreature);
    }

    /// <summary>
    /// 清理数据：泡泡（含连线）立即回池（外部移除路径：目标死亡清BUFF/战斗结束等，不播溶解）
    /// </summary>
    public override void ClearData()
    {
        //先回收表现层再调 base（base.ClearData 会把 buffEntityData 置 null，届时将找不到目标）
        RecycleBubble();
        bubbleEffectId = 0;
        bubbleBaseRadius = 0;
        damageTransferRate = 1f;
        transferApplierId = null;
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
        //破碎溶解中：播完溶解才真正移除并回池（破碎开始后 InterceptUnderAttack 即停止拦截，目标已恢复承伤）
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

    #region 受击拦截
    /// <summary>
    /// 受击拦截：按转移比例把剩余伤害的一部分改道给施加者（向下取整，零头留给目标），并播罩体+连线闪白
    /// <para>施加者已死/离场时返回 0 不拦截（也不闪白），护盾的破碎由 UpdateBuffTime 在下一帧主动处理。</para>
    /// </summary>
    public override int InterceptUnderAttack(int incomingDamage, out string transferApplierId)
    {
        transferApplierId = null;
        if (incomingDamage <= 0 || isBreaking || this.transferApplierId.IsNull()) return 0;
        //施加者判活：已死/离场则本帧不转移（调用方另有同样判空，双保险）
        var applierCreature = GetFightCreatureEntityForApplier();
        if (applierCreature == null || applierCreature.fightCreatureData == null || applierCreature.IsDead()) return 0;
        //按比例拆分（向下取整：零头归目标，避免两端四舍五入凭空多/少伤害）
        int transferDamage = (int)(incomingDamage * damageTransferRate);
        if (transferDamage <= 0) return 0;
        transferApplierId = this.transferApplierId;
        PlayHitFlash();
        return transferDamage;
    }
    #endregion

    #region 护盾表现
    /// <summary>
    /// 受击闪白（伤害被拦截转移时由本类 InterceptUnderAttack 调用；罩体与连线同闪强化转移反馈）
    /// </summary>
    protected void PlayHitFlash()
    {
        if (bubble != null && !isBreaking)
            bubble.PlayHitFlash();
    }

    /// <summary>
    /// 开始破碎：罩体从当前溶解度推满 + 连线同步淡出（组件内部处理）→ 进入破碎态（溶解播完才置失效并回池）
    /// <para>破碎当帧起 InterceptUnderAttack 不再拦截（isBreaking），目标当帧恢复剩余伤害结算。</para>
    /// </summary>
    protected void StartBreak()
    {
        if (isBreaking) return;
        isBreaking = true;
        breakTimer = 0;
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
        //防御：gameObject 已彻底销毁则不再回池；同帧「已排队 Destroy 但尚未生效」的条目仍会入池，由 GetFightPrefabCommon 出池校验丢弃
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
    /// 解析 class_entity_data（"泡泡特效ID,基础半径[,伤害转移比例]"）
    /// </summary>
    protected void ParseEntityData()
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"伤害转移护盾BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"泡泡特效ID,基础半径[,伤害转移比例]\"：{buffInfo.class_entity_data}");
            return;
        }
        bubbleEffectId = long.Parse(arrEntityData[0]);
        bubbleBaseRadius = float.Parse(arrEntityData[1]);
        //第三项可选：伤害转移比例（省略=1 全额改道；越界按 0~1 夹取）
        damageTransferRate = 1f;
        if (arrEntityData.Length >= 3 && float.TryParse(arrEntityData[2], out float transferRate))
        {
            damageTransferRate = Mathf.Clamp01(transferRate);
        }
        isDataParsed = true;
    }
    #endregion
}
