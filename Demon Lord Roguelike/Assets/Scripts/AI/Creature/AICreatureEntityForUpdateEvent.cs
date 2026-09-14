using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生物 AI 基类 - 通用 Update 事件部分（partial，自主文件拆出：创建时读 NPC ai_param 配置注册事件，每帧统一推进）。
/// </summary>
public abstract partial class AICreatureEntity
{
    #region 通用 Update 事件（创建时读 NPC ai_param 配置注册；当前支持 skill_update 技能定时触发）
    /// <summary>
    /// Update 事件运行时数据（间隔+计时+触发动作）。
    /// <para>触发契约：计时到点调用 actionTrigger——返回 true=本轮已触发（计时由 tick 统一清零从头计）；返回 false=条件不满足，保持就绪下帧再试（不打断清零）。</para>
    /// </summary>
    public class AIUpdateEventRuntime
    {
        /// <summary>触发间隔(秒)</summary>
        public float interval;
        /// <summary>已累计时间(秒)</summary>
        public float timer;
        /// <summary>触发动作（true=已触发清零；false=保持就绪）</summary>
        public Func<AICreatureEntity, AIUpdateEventRuntime, bool> actionTrigger;
    }
    /// <summary>Update 事件列表（仅配置了 ai_param 的NPC生物非空，其余生物每帧仅一次 null 判断）</summary>
    protected List<AIUpdateEventRuntime> listUpdateEvent;

    /// <summary>
    /// 每帧更新：先驱动当前意图，再推进通用 Update 事件
    /// </summary>
    public override void Update()
    {
        base.Update();
        UpdateAIUpdateEvent();
    }

    /// <summary>
    /// 注册一个 Update 事件（间隔秒 + 触发动作）
    /// </summary>
    public void RegisterUpdateEvent(float interval, Func<AICreatureEntity, AIUpdateEventRuntime, bool> actionTrigger)
    {
        if (listUpdateEvent == null) listUpdateEvent = new List<AIUpdateEventRuntime>();
        listUpdateEvent.Add(new AIUpdateEventRuntime { interval = interval, timer = 0, actionTrigger = actionTrigger });
    }

    /// <summary>
    /// 创建时读取 NPC ai_param 配置注册 Update 事件（当前支持 skill_update=每 N 秒触发指定 attack_mode_ext 技能）。
    /// <para>调用时机：子类 InitData 写入 selfCreatureEntity 之后；无配置则不建列表（每帧零开销）。</para>
    /// </summary>
    protected void InitUpdateEvents()
    {
        listUpdateEvent = null;
        var npcInfo = selfCreatureEntity?.fightCreatureData?.creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null) return;
        var listEvent = npcInfo.GetListAIParamUpdateEvent();
        if (listEvent.IsNull()) return;
        for (int i = 0; i < listEvent.Count; i++)
        {
            var item = listEvent[i];
            switch (item.eventType)
            {
                case AIParamUpdateEventTypeEnum.SkillUpdate:
                    //listParam=[技能extId, 间隔秒]（NpcInfoBeanPartial 解析时已校验可解析性）
                    long extId = long.Parse(item.listParam[0]);
                    float interval = float.Parse(item.listParam[1]);
                    RegisterUpdateEvent(interval, (self, eventRuntime) => self.TryTriggerExtSkill(extId, eventRuntime));
                    break;
            }
        }
    }

    /// <summary>
    /// 每帧推进通用 Update 事件（计时走战斗时钟 GetFightDeltaTime，暂停/倍速同步）
    /// </summary>
    protected void UpdateAIUpdateEvent()
    {
        if (listUpdateEvent == null) return;
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        for (int i = 0; i < listUpdateEvent.Count; i++)
        {
            var eventRuntime = listUpdateEvent[i];
            eventRuntime.timer += deltaTime;
            if (eventRuntime.timer < eventRuntime.interval) continue;
            //到点：true=已触发清零重计；false=保持就绪下帧再试
            if (eventRuntime.actionTrigger != null && eventRuntime.actionTrigger(this, eventRuntime))
            {
                eventRuntime.timer = 0;
            }
        }
    }

    /// <summary>
    /// 尝试触发指定技能（skill_update 事件的动作）：校验技能配 trigger_scene=释放技能意图 → 当前意图可切入时先写后切「释放技能」意图；
    /// 不可切入返回 false 保持就绪（恢复后下帧补放）；配错时禁用本事件防日志刷屏
    /// </summary>
    protected virtual bool TryTriggerExtSkill(long extId, AIUpdateEventRuntime eventRuntime)
    {
        var extInfo = AttackModeExtInfoCfg.GetItemData(extId);
        if (extInfo == null || extInfo.GetTriggerScene() != AttackModeExtTriggerSceneEnum.CastSkillIntent)
        {
            LogUtil.LogError($"NPC技能定时触发配置错误：技能[{extId}]不存在或 trigger_scene 未配 1（释放技能意图）");
            eventRuntime.interval = float.MaxValue;
            return true;
        }
        if (!CanEnterCastSkillIntent()) return false;
        EnterCastSkillIntent(extInfo);
        return true;
    }

    /// <summary>
    /// 当前意图是否可切入「释放技能」：待机/移动立即；攻击意图仅在无在途攻击回调时（attackState!=2，攻击模块未发射）可打断，
    /// 否则在途攻击的结束回调会半路把生物扯出本意图；击退/魅惑/死亡/攻核心/释放技能中不可切入
    /// </summary>
    protected virtual bool CanEnterCastSkillIntent()
    {
        switch (currentIntentEnum)
        {
            case AIIntentEnum.AttackCreatureIdle:
            case AIIntentEnum.AttackCreatureMove:
                return true;
            case AIIntentEnum.AttackCreatureAttack:
                var attackIntent = currentIntent as AIIntentCreatureAttack;
                return attackIntent == null || attackIntent.attackState != 2;
            default:
                return false;
        }
    }

    /// <summary>
    /// 切入「释放技能」意图（先写入本次技能配置再切换，照击退意图 SetupKnockback 的先写后切先例）
    /// </summary>
    protected virtual void EnterCastSkillIntent(AttackModeExtInfoBean extInfo)
    {
        var castIntent = GetIntent<AIIntentAttackCreatureCastSkill>(AIIntentEnum.AttackCreatureCastSkill);
        if (castIntent == null) return;
        castIntent.SetupCastSkill(extInfo);
        ChangeIntent(AIIntentEnum.AttackCreatureCastSkill);
    }
    #endregion
}
