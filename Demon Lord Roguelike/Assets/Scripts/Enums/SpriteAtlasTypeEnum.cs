/// <summary>
/// 图集类型枚举（游戏层）
/// </summary>
/// <remarks>
/// 框架层 IconHandler / UIHandlerCursor 仅认 string atlasTag，本枚举为游戏层封装的类型安全外壳。
/// 通过扩展方法 <see cref="SpriteAtlasTypeEnumExtension.ToAtlasTag"/> 转换为字符串再传入框架 API。
/// 新增图集类型只需在此枚举追加，无需修改框架层。
/// </remarks>
public enum SpriteAtlasTypeEnum
{
    UI,//ui
    Items,//道具
    Sky,//天空
    Skins,//皮肤
    AbyssalBlessing,//深渊馈赠
}

/// <summary>
/// 图集类型扩展
/// </summary>
public static class SpriteAtlasTypeEnumExtension
{
    /// <summary>
    /// 转为框架层 IconHandler.GetIconSprite 接受的 atlasTag 字符串
    /// </summary>
    public static string ToAtlasTag(this SpriteAtlasTypeEnum self)
    {
        return self.GetEnumName();
    }
}
