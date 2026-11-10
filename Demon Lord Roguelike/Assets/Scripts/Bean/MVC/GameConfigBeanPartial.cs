public partial class GameConfigBean
{
    #region 游戏专用配置
    //是否显示按键提示(UIViewPressCommon 等快捷按键提示)，默认开启
    public bool pressKeyTipShow = true;
    //弹道阴影（战斗中子弹弹道是否投/收阴影：开启=所有弹道投阴影，配置表 visual_data 可显式 cast:0 单独关闭某种弹道；接收阴影仍按 receive:1 配置），默认开启
    public bool bulletShadow = true;
    //物品阴影（战斗中魔晶掉落物是否投阴影），默认开启
    public bool itemShadow = true;
    //场景杂物阴影（花海等装饰实例是否投阴影），默认关闭
    public bool sceneClutterShadow = false;
    #endregion
}
