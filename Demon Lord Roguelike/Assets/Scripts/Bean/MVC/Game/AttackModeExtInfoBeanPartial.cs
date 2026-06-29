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
}
public partial class AttackModeExtInfoCfg
{
}
