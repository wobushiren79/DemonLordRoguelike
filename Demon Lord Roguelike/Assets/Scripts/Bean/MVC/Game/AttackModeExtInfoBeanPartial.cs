using System;
using System.Collections.Generic;
public partial class AttackModeExtInfoBean
{
    #region 类型
    /// <summary>
    /// 获取扩展类型（ext_type 转枚举）
    /// </summary>
    public AttackModeExtTypeEnum GetExtType()
    {
        return (AttackModeExtTypeEnum)ext_type;
    }
    #endregion

    #region 触发场景
    /// <summary>
    /// 获取触发场景（trigger_scene 转枚举；0=攻击意图内释放[默认]，1=释放技能意图[由 NPC ai_param 的 skill_update 事件全局周期驱动]）
    /// </summary>
    public AttackModeExtTriggerSceneEnum GetTriggerScene()
    {
        return (AttackModeExtTriggerSceneEnum)trigger_scene;
    }
    #endregion
}
public partial class AttackModeExtInfoCfg
{
}
