using System;
using System.Collections.Generic;
public partial class LevelInfoBean
{
    /// <summary>
    /// 升级获得的属性加点数(献祭升级成功后由玩家手动分配)
    /// <para>临时字段: Excel(excel_level_info) 已有 attribute_point 列, 在 Unity 运行 ExcelEditorWindow
    /// 重新生成 LevelInfoBean.cs 后, 该字段会写入自动生成文件, 届时必须删除此处临时定义以避免重复定义。</para>
    /// </summary>
    public int attribute_point;
}
public partial class LevelInfoCfg
{
}
