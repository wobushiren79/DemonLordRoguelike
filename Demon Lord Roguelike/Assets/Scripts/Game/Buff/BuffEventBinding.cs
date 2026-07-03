using System;
using System.Collections.Generic;

/// <summary>
/// 强类型 BUFF 事件绑定
/// </summary>
/// <typeparam name="TArg">事件参数类型（与 EventHandler.RegisterEvent&lt;T&gt; 一致）</typeparam>
public class BuffEventBinding<TArg> : IBuffEventBinding
{
    private readonly Func<BuffBaseEntity, Action<TArg>> handlerSelector;

    public BuffEventBinding(Func<BuffBaseEntity, Action<TArg>> handlerSelector)
    {
        this.handlerSelector = handlerSelector;
    }

    public void Register(BuffBaseEntity entity, string eventName)
    {
        //仅在 Register 时构造一次 Action<TArg> 并缓存到 entity，
        //避免 Unregister 时再造一份（虽然结构相等也能移除，但少一次GC更稳）
        Action<TArg> handler = handlerSelector(entity);
        entity.cachedEventDelegate = handler;
        EventHandler.Instance.RegisterEvent<TArg>(eventName, handler);
    }

    public void Unregister(BuffBaseEntity entity, string eventName)
    {
        if (entity.cachedEventDelegate is Action<TArg> handler)
        {
            EventHandler.Instance.UnRegisterEvent<TArg>(eventName, handler);
        }
        else
        {
            //极端兜底：缓存丢失/类型错配，回退到结构相等的delegate反注册
            EventHandler.Instance.UnRegisterEvent<TArg>(eventName, handlerSelector(entity));
        }
        entity.cachedEventDelegate = null;
    }
}

/// <summary>
/// BUFF事件全局调度器
/// 字典驱动事件名 -> Binding 映射。新增事件只动这里一行。
/// </summary>
public static class BuffEventDispatcher
{
    /// <summary>
    /// 事件名 -> Binding 映射表
    /// 新增 BUFF 触发事件时在此追加一行即可，不需要改 BuffBaseEntity
    /// </summary>
    private static readonly Dictionary<string, IBuffEventBinding> dicBindings = new Dictionary<string, IBuffEventBinding>
    {
        { EventsInfo.GameFightLogic_UnderAttack_Dead,
            new BuffEventBinding<FightUnderAttackBean>(e => e.EventForUnderAttackDead) },
        { EventsInfo.GameFightLogic_UnderAttack,
            new BuffEventBinding<FightUnderAttackBean>(e => e.EventForUnderAttack) },
        { EventsInfo.GameFightLogic_RegainHP,
            new BuffEventBinding<FightUnderAttackBean>(e => e.EventForRegainHP) },
        { EventsInfo.GameFightLogic_CreatureDeadDropCrystal,
            new BuffEventBinding<FightDropCrystalBean>(e => e.EventForCreatureDeadDropCrystal) },
        { EventsInfo.GameFightLogic_CreatureDeadStart,
            new BuffEventBinding<FightCreatureEntity>(e => e.EventForCreatureDeadStart) },
        { EventsInfo.GameFightLogic_CreatureDeadEnd,
            new BuffEventBinding<FightCreatureEntity>(e => e.EventForCreatureDeadEnd) },
    };

    /// <summary>
    /// 为指定 BUFF 实体注册事件
    /// </summary>
    public static void Register(BuffBaseEntity entity, string eventName)
    {
        if (entity == null || eventName.IsNull()) return;
        if (dicBindings.TryGetValue(eventName, out var binding))
        {
            binding.Register(entity, eventName);
        }
    }

    /// <summary>
    /// 为指定 BUFF 实体注销事件
    /// </summary>
    public static void Unregister(BuffBaseEntity entity, string eventName)
    {
        if (entity == null || eventName.IsNull()) return;
        if (dicBindings.TryGetValue(eventName, out var binding))
        {
            binding.Unregister(entity, eventName);
        }
    }
}
