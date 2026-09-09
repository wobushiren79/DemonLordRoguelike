using UnityEditor;
using UnityEngine;

/// <summary>
/// GameResourceEditor 通用板块（partial）：跨板块的一键组合操作
/// </summary>
public partial class GameResourceEditor
{
    #region 通用板块UI

    /// <summary>
    /// 绘制通用板块页签
    /// </summary>
    private void DrawCommonTab()
    {
        DrawSectionBox("一键操作", () =>
        {
            GUIStyle hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11
            };
            GUILayout.Label("依次执行：生成所有 Spine 道具图标 → 生成所有 Spine 皮肤图标 → 刷新所有图集", hintStyle);
            GUILayout.Space(8);
            DrawButton("一键生成所有资源", _colorAll, 38, GenerateAllResources);
        });
    }

    #endregion

    #region 通用逻辑

    /// <summary>
    /// 一键生成所有资源：Spine 道具图标 + Spine 皮肤图标 + 刷新所有图集
    /// </summary>
    public static void GenerateAllResources()
    {
        LogUtil.Log("========== 开始生成所有资源 ==========");

        SpineAllItemInit();
        SpineAllSkinInit();
        RefreshAllAtlases();

        LogUtil.Log("========== 所有资源生成完成 ==========");
    }

    #endregion
}
