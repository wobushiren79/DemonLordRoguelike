/// <summary>
/// BUFF事件绑定接口
/// 把"事件名 -> 回调方法 + 回调参数类型"的映射从 BuffBaseEntity 的硬编码 switch 中抽出
/// 新增事件类型只需在 BuffEventDispatcher.dicBindings 注册一行，不需要改动基类
/// </summary>
public interface IBuffEventBinding
{
    /// <summary>
    /// 注册：把 entity 对应的事件处理方法订阅到全局 EventHandler
    /// </summary>
    void Register(BuffBaseEntity entity, string eventName);

    /// <summary>
    /// 注销：把先前订阅的处理方法从 EventHandler 移除
    /// </summary>
    void Unregister(BuffBaseEntity entity, string eventName);
}
