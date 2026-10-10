using UnityEngine;

/// <summary>
/// 终焉议会编辑工具 - 议员评级页签
/// 用于可视化编辑 excel_doom_council_ratings_info[终焉议会议员等级信息] 表（左侧评级列表 + 右侧参数编辑）
/// 宿主窗口见 DoomCouncilEditorWindow（菜单：游戏/终焉议会编辑）
/// </summary>
public class DoomCouncilEditorTabRatings : DoomCouncilEditorTabBase<DoomCouncilRatingsInfoBean>
{
    #region 基类抽象实现

    /// <summary>Excel 文件名</summary>
    protected override string ExcelFileName => "excel_doom_council_ratings_info[终焉议会议员等级信息].xlsx";

    /// <summary>工作表名称</summary>
    protected override string SheetName => "DoomCouncilRatingsInfo";

    /// <summary>多语言 JSON 文件名</summary>
    protected override string LanguageFileName => "Language_DoomCouncilRatingsInfo_cn.txt";

    /// <summary>
    /// 列表项显示名：优先取评级名文本ID对应中文，取不到回退备注
    /// </summary>
    protected override string GetItemDisplayName(DoomCouncilRatingsInfoBean bean)
    {
        if (languageMap.TryGetValue(bean.name, out LanguageJsonItem item) && !string.IsNullOrEmpty(item.content))
            return item.content;
        return string.IsNullOrEmpty(bean.remark) ? "(未命名)" : bean.remark;
    }

    #endregion

    #region UI 绘制 - 字段编辑区

    /// <summary>
    /// 绘制议员评级字段编辑区（基础信息/文本配置/备注）
    /// </summary>
    protected override void DrawEditFields()
    {
        // 基础信息
        DrawSectionTitle("基础信息");
        DrawIdField();
        currentBean.icon_res = DrawStringField(new GUIContent("图标名字", "icon_res：图标资源名"), currentBean.icon_res, "icon_res");
        currentBean.vote = DrawIntField(new GUIContent("投票票数", "vote：该评级议员投票时的票权"), currentBean.vote, "vote");

        // 文本配置
        DrawSectionTitle("文本配置");
        currentBean.name = DrawLanguageIdField(new GUIContent("名字文本ID", "name：多语言文本ID，预览为中文评级名"), currentBean.name, "name", false);

        // 备注
        DrawSectionTitle("备注");
        currentBean.remark = DrawStringField(new GUIContent("备注", "remark"), currentBean.remark, "remark");
    }

    #endregion
}
