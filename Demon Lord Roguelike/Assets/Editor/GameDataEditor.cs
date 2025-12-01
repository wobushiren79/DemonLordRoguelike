using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
public class GameDataEditor
{
    [MenuItem("游戏/生成所有spine里的道具图标")]
    public static void SpineAllItemInit()
    {
        string inputPath = "Assets/LoadResources/Spine/Creature";
        string outputPath = "Assets/LoadResources/Textures/Items";
        string filterSkinName = "Clothes,Pants,Weapon,Shoes,Hat,Mask,NoseRing,Arrow";//筛选名字
        SpineWindow.ExtractSkinTextures(inputPath, outputPath, null, true, null, filterSkinName);


        // 指定路径（示例路径：Assets/Art/Atlases）
        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForItems.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
        if (atlas != null)
        {
            // 重新打包图集
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"已重新生成图集: {atlas.name}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("游戏/生成所有spine里的皮肤图标")]
    public static void SpineAllSkinInit()
    {
        string inputPath = "Assets/LoadResources/Spine/Creature";
        string outputPath = "Assets/LoadResources/Textures/Skins";
        string filterSkinName = "Eye,Head,Mouth,Body,Hair,Horn,Wing";//筛选名字
        SpineWindow.ExtractSkinTextures(inputPath, outputPath, null, true, null, filterSkinName);


        // 指定路径（示例路径：Assets/Art/Atlases）
        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForSkins.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
        if (atlas != null)
        {
            // 重新打包图集
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"已重新生成图集: {atlas.name}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("游戏/刷新所有图集")]
    public static void RefreshAllAtlases()
    {
        // 指定路径（示例路径：Assets/Art/Atlases）
        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas";
        
        // 查找所有SpriteAtlas资源
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { targetPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            
            if (atlas != null)
            {
                // 重新打包图集
                SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
                Debug.Log($"已重新生成图集: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
