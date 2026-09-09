using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// GameResourceEditor 图集板块（partial）：SpriteAtlas 图集刷新
/// </summary>
public partial class GameResourceEditor
{
    #region 图集板块UI

    /// <summary>
    /// 绘制图集板块页签
    /// </summary>
    private void DrawAtlasTab()
    {
        DrawSectionBox("图集刷新", () =>
        {
            GUIStyle hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11
            };
            GUILayout.Label("重新打包 Assets/LoadResources/Textures/SpriteAtlas 下的所有图集", hintStyle);
            GUILayout.Space(8);
            DrawButton("刷新所有图集", _colorRefresh, 32, RefreshAllAtlases);
        });
    }

    #endregion

    #region 图集刷新逻辑

    /// <summary>
    /// 重新打包 SpriteAtlas 目录下的所有图集
    /// </summary>
    public static void RefreshAllAtlases()
    {
        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas";
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { targetPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

            if (atlas != null)
            {
                SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
                LogUtil.Log($"已重新生成图集: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #endregion
}
