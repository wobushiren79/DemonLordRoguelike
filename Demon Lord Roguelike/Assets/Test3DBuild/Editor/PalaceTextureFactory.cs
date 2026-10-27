using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 为 Test3DBuild 宫殿场景生成原创低分辨率纹理，所有文件仅写入指定目录。
/// </summary>
public static class PalaceTextureFactory
{
    #region 公共生成入口

    /// <summary>生成紫色石砖纹理。</summary>
    public static Texture2D CreatePurpleStone(string path)
    {
        Color[] palette =
        {
            Rgb(74, 48, 92), Rgb(83, 54, 103), Rgb(66, 43, 85), Rgb(91, 59, 111)
        };
        return CreateBrickTexture(path, palette, Rgb(38, 27, 52), Rgb(117, 79, 133), 64, 32, 16, 101, true);
    }

    /// <summary>生成灰色石砖纹理。</summary>
    public static Texture2D CreateWallStone(string path)
    {
        Color[] palette =
        {
            Rgb(102, 99, 105), Rgb(116, 111, 113), Rgb(91, 91, 99), Rgb(126, 119, 117)
        };
        return CreateBrickTexture(path, palette, Rgb(54, 52, 61), Rgb(151, 143, 137), 64, 32, 16, 202, true);
    }

    /// <summary>生成浅色台阶石材纹理。</summary>
    public static Texture2D CreateDaisStone(string path)
    {
        Color[] palette =
        {
            Rgb(157, 145, 132), Rgb(174, 158, 137), Rgb(144, 137, 133), Rgb(184, 169, 147)
        };
        return CreateBrickTexture(path, palette, Rgb(87, 78, 77), Rgb(202, 184, 157), 64, 32, 32, 303, true);
    }

    /// <summary>生成红金主地毯纹理。</summary>
    public static Texture2D CreateCarpet(string path)
    {
        const int size = 128;
        Color[,] pixels = new Color[size, size];
        Color red = Rgb(135, 25, 38);
        Color gold = Rgb(202, 147, 55);
        Color darkGold = Rgb(126, 78, 34);
        Color lightGold = Rgb(231, 187, 84);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = (Hash01(x, y, 404) - 0.5f) * 0.12f;
                Color color = Shift(red, noise);
                int edge = Math.Min(x, size - 1 - x);

                if (edge < 3) color = darkGold;
                else if (edge < 6) color = gold;
                else if (edge == 8 || edge == 10) color = darkGold;
                else if (edge == 11) color = lightGold;

                int patternY = y % 32;
                int centerDistance = Math.Abs(x - size / 2) + Math.Abs(patternY - 16);
                if (edge > 14 && centerDistance == 13) color = darkGold;
                if (edge > 14 && centerDistance == 12 && patternY % 4 == 0) color = gold;
                if (edge > 14 && Math.Abs(x - size / 2) < 2 && Math.Abs(patternY - 16) < 5) color = gold;
                if (edge > 14 && Math.Abs(patternY - 16) < 2 && Math.Abs(x - size / 2) < 6) color = gold;

                int sideMotif = Math.Min(Math.Abs(x - 20), Math.Abs(x - (size - 21)));
                if (sideMotif + Math.Abs(patternY - 16) == 7) color = darkGold;
                if (sideMotif < 2 && patternY % 8 == 4) color = gold;

                pixels[x, y] = color;
            }
        }
        return WriteTexture(path, size, size, pixels, true);
    }

    /// <summary>生成红色天鹅绒纹理。</summary>
    public static Texture2D CreateVelvet(string path)
    {
        const int size = 32;
        Color[,] pixels = new Color[size, size];
        Color red = Rgb(128, 18, 39);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = (Hash01(x, y, 505) - 0.5f) * 0.16f;
                pixels[x, y] = Shift(red, noise);
            }
        }
        return WriteTexture(path, size, size, pixels, true);
    }

    /// <summary>生成带金色纹章的红旗纹理。</summary>
    public static Texture2D CreateBanner(string path)
    {
        const int width = 64;
        const int height = 128;
        Color[,] pixels = new Color[width, height];
        Color red = Rgb(138, 22, 39);
        Color gold = Rgb(220, 172, 70);
        Color darkGold = Rgb(128, 77, 32);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int topY = height - 1 - y;
                float shade = 0.82f + 0.18f * Mathf.Sin(x * Mathf.PI * 2f / width);
                Color color = Shift(red, shade - 1f);
                int edge = Math.Min(x, width - 1 - x);
                if (edge < 3 || topY < 3 || topY >= height - 4) color = darkGold;
                else if (edge < 5 || topY < 5 || topY >= height - 7) color = gold;

                int dx = Math.Abs(x - width / 2);
                int dy = topY - 50;
                if (dy >= -15 && dy <= 12 && dx + Math.Abs(dy) == 20) color = gold;
                if (dy >= -11 && dy <= 8 && dx + Math.Abs(dy) == 15) color = darkGold;
                if (topY >= 39 && topY <= 43 && dx <= 17) color = gold;
                if (topY >= 32 && topY < 43 && (dx == 15 || dx == 7 || dx == 0)) color = gold;
                if (topY >= 30 && topY < 36 && dx <= 18) color = gold;
                if (topY >= 76 && topY <= 79 && dx <= 12) color = darkGold;
                if (topY >= 80 && topY <= 82 && dx <= 8) color = gold;

                pixels[x, y] = color;
            }
        }
        return WriteTexture(path, width, height, pixels, false);
    }

    /// <summary>生成带石框和彩色玻璃的拱窗纹理。</summary>
    public static Texture2D CreateWindow(string path)
    {
        const int width = 64;
        const int height = 128;
        Color[,] pixels = new Color[width, height];
        Color frame = Rgb(105, 101, 104);
        Color frameLight = Rgb(157, 146, 132);
        Color lead = Rgb(39, 37, 50);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int topY = height - 1 - y;
                bool outer = InsideArch(x, topY, width, height, 29, 39, 43);
                bool inner = InsideArch(x, topY, width, height, 23, 33, 48);
                Color color = new Color(0, 0, 0, 0);
                if (outer && !inner)
                {
                    color = (x + topY) % 7 == 0 ? frameLight : frame;
                }
                else if (inner)
                {
                    bool leading = x % 12 == 0 || topY % 18 == 0 || topY > 119;
                    if (leading) color = lead;
                    else
                    {
                        int pane = (x / 12 + topY / 18) % 3;
                        color = pane == 0 ? Rgb(48, 85, 111) : pane == 1 ? Rgb(66, 55, 105) : Rgb(45, 105, 100);
                        color = Shift(color, (Hash01(x, y, 606) - 0.5f) * 0.14f);
                    }
                }
                pixels[x, y] = color;
            }
        }
        return WriteTexture(path, width, height, pixels, false);
    }

    /// <summary>生成透明背景像素火焰纹理。</summary>
    public static Texture2D CreateFlame(string path)
    {
        const int width = 16;
        const int height = 32;
        Color[,] pixels = new Color[width, height];
        for (int y = 0; y < height; y++)
        {
            float fy = y / (float)(height - 1);
            float halfWidth = fy < 0.25f ? 2f + fy * 16f : 6f * (1f - (fy - 0.25f) / 0.75f);
            float center = width * 0.5f + Mathf.Sin(fy * 5.2f) * 1.1f;
            for (int x = 0; x < width; x++)
            {
                float distance = Math.Abs(x - center);
                if (distance > halfWidth || halfWidth < 0.2f)
                {
                    pixels[x, y] = new Color(0, 0, 0, 0);
                    continue;
                }

                Color color = fy < 0.22f ? Rgb(237, 91, 25) : Rgb(255, 166, 37);
                if (distance < halfWidth * 0.48f && fy < 0.82f) color = Rgb(255, 239, 144);
                pixels[x, y] = color;
            }
        }
        return WriteTexture(path, width, height, pixels, false);
    }

    #endregion

    #region 纹理绘制工具

    /// <summary>生成通用的错缝砖墙纹理。</summary>
    private static Texture2D CreateBrickTexture(string path, Color[] palette, Color mortar, Color highlight, int size, int brickWidth, int brickHeight, int seed, bool repeat)
    {
        Color[,] pixels = new Color[size, size];
        for (int y = 0; y < size; y++)
        {
            int row = y / brickHeight;
            int offset = row % 2 == 0 ? 0 : brickWidth / 2;
            for (int x = 0; x < size; x++)
            {
                int shiftedX = (x + offset) % size;
                int column = shiftedX / brickWidth;
                int localX = shiftedX % brickWidth;
                int localY = y % brickHeight;
                Color color;
                if (localX < 2 || localY < 2)
                {
                    color = mortar;
                }
                else
                {
                    float pick = Hash01(column + row * 7, row, seed);
                    int index = Mathf.Clamp(Mathf.FloorToInt(pick * palette.Length), 0, palette.Length - 1);
                    color = palette[index];
                    if (localY == 2 && localX > 2) color = Color.Lerp(color, highlight, 0.35f);
                    if (localY == brickHeight - 1 || localX == brickWidth - 1) color = Shift(color, -0.12f);
                    color = Shift(color, (Hash01(x, y, seed + 17) - 0.5f) * 0.12f);
                }
                pixels[x, y] = color;
            }
        }
        return WriteTexture(path, size, size, pixels, repeat);
    }

    /// <summary>判断像素是否位于拱窗轮廓内。</summary>
    private static bool InsideArch(int x, int topY, int width, int height, int halfWidth, int archHeight, int archTop)
    {
        int centerX = width / 2;
        if (topY >= archTop && topY < height - 4 && Math.Abs(x - centerX) <= halfWidth) return true;
        if (topY >= archTop) return false;
        float nx = (x - centerX) / (float)halfWidth;
        float ny = (topY - archTop) / (float)archHeight;
        return nx * nx + ny * ny <= 1f && topY >= archTop - archHeight;
    }

    /// <summary>将像素数组写成 PNG 并按 HD-2D 规则导入。</summary>
    private static Texture2D WriteTexture(string path, int width, int height, Color[,] pixels, bool repeat)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, pixels[x, y]);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>将 0-255 颜色转换为 Unity 颜色。</summary>
    private static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    /// <summary>按比例调整颜色亮度。</summary>
    private static Color Shift(Color color, float amount)
    {
        return new Color(Mathf.Clamp01(color.r + amount), Mathf.Clamp01(color.g + amount), Mathf.Clamp01(color.b + amount), color.a);
    }

    /// <summary>生成确定性的 0-1 哈希噪声。</summary>
    private static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            int hash = x * 374761393 + y * 668265263 + seed * 69069;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            return ((hash ^ (hash >> 16)) & 0x7fffffff) / 2147483647f;
        }
    }

    #endregion
}
