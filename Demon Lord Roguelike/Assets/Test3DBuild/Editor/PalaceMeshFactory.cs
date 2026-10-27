using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为 Test3DBuild 宫殿场景创建原创程序化网格，不依赖项目现有模型。
/// </summary>
public static class PalaceMeshFactory
{
    #region 基础网格

    /// <summary>创建以原点为中心、边长为 1 的盒体网格。</summary>
    public static Mesh CreateBox()
    {
        MeshBuilder builder = new MeshBuilder();
        const float h = 0.5f;
        builder.AddQuad(new Vector3(-h, h, -h), new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), Vector3.up);
        builder.AddQuad(new Vector3(-h, -h, h), new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), Vector3.down);
        builder.AddQuad(new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h), Vector3.forward);
        builder.AddQuad(new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h), Vector3.back);
        builder.AddQuad(new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h), Vector3.right);
        builder.AddQuad(new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h), Vector3.left);
        return builder.Build("Palace_Box");
    }

    /// <summary>创建沿 Y 轴、直径和高度均为 1 的圆柱网格。</summary>
    public static Mesh CreateCylinder(int segments)
    {
        MeshBuilder builder = new MeshBuilder();
        for (int i = 0; i < segments; i++)
        {
            float u0 = i / (float)segments;
            float u1 = (i + 1) / (float)segments;
            float a0 = u0 * Mathf.PI * 2f;
            float a1 = u1 * Mathf.PI * 2f;
            Vector3 r0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            Vector3 r1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Vector3 b0 = r0 * 0.5f + Vector3.down * 0.5f;
            Vector3 b1 = r1 * 0.5f + Vector3.down * 0.5f;
            Vector3 t0 = r0 * 0.5f + Vector3.up * 0.5f;
            Vector3 t1 = r1 * 0.5f + Vector3.up * 0.5f;
            Vector3 normal = (r0 + r1).normalized;
            builder.AddQuad(b0, t0, t1, b1, normal, new Vector2(u0, 0), new Vector2(u0, 1), new Vector2(u1, 1), new Vector2(u1, 0));

            Vector3 topCenter = Vector3.up * 0.5f;
            Vector3 bottomCenter = Vector3.down * 0.5f;
            builder.AddTriangle(topCenter, t1, t0, Vector3.up, new Vector2(0.5f, 0.5f), ToCapUv(r1), ToCapUv(r0));
            builder.AddTriangle(bottomCenter, b0, b1, Vector3.down, new Vector2(0.5f, 0.5f), ToCapUv(r0), ToCapUv(r1));
        }
        return builder.Build("Palace_Cylinder");
    }

    /// <summary>创建沿 Y 轴、直径和高度均为 1 的圆锥网格。</summary>
    public static Mesh CreateCone(int segments)
    {
        MeshBuilder builder = new MeshBuilder();
        Vector3 tip = Vector3.up * 0.5f;
        Vector3 bottomCenter = Vector3.down * 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float u0 = i / (float)segments;
            float u1 = (i + 1) / (float)segments;
            float a0 = u0 * Mathf.PI * 2f;
            float a1 = u1 * Mathf.PI * 2f;
            Vector3 r0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            Vector3 r1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Vector3 b0 = r0 * 0.5f + Vector3.down * 0.5f;
            Vector3 b1 = r1 * 0.5f + Vector3.down * 0.5f;
            builder.AddTriangle(b0, tip, b1, FaceNormal(b0, tip, b1), new Vector2(u0, 0), new Vector2((u0 + u1) * 0.5f, 1), new Vector2(u1, 0));
            builder.AddTriangle(bottomCenter, b0, b1, Vector3.down, new Vector2(0.5f, 0.5f), ToCapUv(r0), ToCapUv(r1));
        }
        return builder.Build("Palace_Cone");
    }

    /// <summary>创建直径为 1 的低多边形球体网格。</summary>
    public static Mesh CreateSphere(int segments, int rings)
    {
        MeshBuilder builder = new MeshBuilder();
        for (int ring = 0; ring < rings; ring++)
        {
            float v0 = ring / (float)rings;
            float v1 = (ring + 1) / (float)rings;
            float phi0 = v0 * Mathf.PI;
            float phi1 = v1 * Mathf.PI;
            for (int segment = 0; segment < segments; segment++)
            {
                float u0 = segment / (float)segments;
                float u1 = (segment + 1) / (float)segments;
                Vector3 p00 = SpherePoint(phi0, u0 * Mathf.PI * 2f);
                Vector3 p01 = SpherePoint(phi0, u1 * Mathf.PI * 2f);
                Vector3 p11 = SpherePoint(phi1, u1 * Mathf.PI * 2f);
                Vector3 p10 = SpherePoint(phi1, u0 * Mathf.PI * 2f);
                builder.AddQuad(p00, p01, p11, p10, FaceNormal(p00, p01, p11), new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1));
            }
        }
        return builder.Build("Palace_Sphere");
    }

    /// <summary>创建位于 XY 平面、朝向负 Z 的四边形网格。</summary>
    public static Mesh CreateQuad()
    {
        MeshBuilder builder = new MeshBuilder();
        builder.AddQuad(new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, -0.5f, 0), Vector3.back,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0));
        return builder.Build("Palace_Quad");
    }

    #endregion

    #region 造型网格

    /// <summary>创建顶部位于 Y=0、底部向下且带 V 形缺口和轻微布纹起伏的旗帜网格。</summary>
    public static Mesh CreateBanner(int columns, int rows)
    {
        MeshBuilder builder = new MeshBuilder();
        for (int row = 0; row < rows; row++)
        {
            float v0 = row / (float)rows;
            float v1 = (row + 1) / (float)rows;
            for (int column = 0; column < columns; column++)
            {
                float u0 = column / (float)columns;
                float u1 = (column + 1) / (float)columns;
                Vector3 p00 = BannerPoint(u0, v0);
                Vector3 p01 = BannerPoint(u1, v0);
                Vector3 p11 = BannerPoint(u1, v1);
                Vector3 p10 = BannerPoint(u0, v1);
                builder.AddQuad(p00, p01, p11, p10, Vector3.back, new Vector2(u0, 1 - v0), new Vector2(u1, 1 - v0), new Vector2(u1, 1 - v1), new Vector2(u0, 1 - v1));
            }
        }
        return builder.Build("Palace_Banner");
    }

    /// <summary>根据半径和高度剖面创建旋转体花盆网格。</summary>
    public static Mesh CreateLathe(Vector2[] profile, int segments)
    {
        MeshBuilder builder = new MeshBuilder();
        for (int ring = 0; ring < profile.Length - 1; ring++)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                float u0 = segment / (float)segments;
                float u1 = (segment + 1) / (float)segments;
                float a0 = u0 * Mathf.PI * 2f;
                float a1 = u1 * Mathf.PI * 2f;
                Vector3 p00 = LathePoint(profile[ring], a0);
                Vector3 p01 = LathePoint(profile[ring], a1);
                Vector3 p11 = LathePoint(profile[ring + 1], a1);
                Vector3 p10 = LathePoint(profile[ring + 1], a0);
                builder.AddQuad(p00, p10, p11, p01, FaceNormal(p00, p10, p11), new Vector2(u0, 0), new Vector2(u0, 1), new Vector2(u1, 1), new Vector2(u1, 0));
            }
        }

        Vector2 bottom = profile[0];
        for (int segment = 0; segment < segments; segment++)
        {
            float a0 = segment / (float)segments * Mathf.PI * 2f;
            float a1 = (segment + 1) / (float)segments * Mathf.PI * 2f;
            Vector3 center = new Vector3(0, bottom.y, 0);
            Vector3 b0 = LathePoint(bottom, a0);
            Vector3 b1 = LathePoint(bottom, a1);
            builder.AddTriangle(center, b0, b1, Vector3.down, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1, 0));
        }
        return builder.Build("Palace_FlowerPot");
    }

    /// <summary>创建风格化石像龙翼网格。</summary>
    public static Mesh CreateWing()
    {
        MeshBuilder builder = new MeshBuilder();
        Vector3 root = new Vector3(0, 0, 0);
        Vector3 top = new Vector3(0.35f, 1.1f, 0.04f);
        Vector3 tip = new Vector3(1.25f, 1.45f, 0);
        Vector3 middle = new Vector3(0.85f, 0.62f, 0.08f);
        Vector3 lower = new Vector3(1.15f, 0.05f, 0);
        Vector3 inner = new Vector3(0.25f, 0.18f, 0.03f);
        builder.AddTriangle(root, top, middle, Vector3.back, new Vector2(0, 0), new Vector2(0.2f, 1), new Vector2(0.7f, 0.5f));
        builder.AddTriangle(top, tip, middle, Vector3.back, new Vector2(0.2f, 1), new Vector2(1, 1), new Vector2(0.7f, 0.5f));
        builder.AddTriangle(root, middle, lower, Vector3.back, new Vector2(0, 0), new Vector2(0.7f, 0.5f), new Vector2(1, 0));
        builder.AddTriangle(root, lower, inner, Vector3.back, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.2f, 0.1f));
        return builder.Build("Palace_DragonWing");
    }

    #endregion

    #region 网格计算工具

    /// <summary>计算球面上的单位顶点位置。</summary>
    private static Vector3 SpherePoint(float phi, float theta)
    {
        return new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * 0.5f;
    }

    /// <summary>计算圆柱或圆锥顶盖 UV。</summary>
    private static Vector2 ToCapUv(Vector3 radial)
    {
        return new Vector2(0.5f + radial.x * 0.5f, 0.5f + radial.z * 0.5f);
    }

    /// <summary>计算旗帜网格上的单个顶点。</summary>
    private static Vector3 BannerPoint(float u, float v)
    {
        float x = u - 0.5f;
        float notch = 0.2f * (1f - Mathf.Abs(u - 0.5f) * 2f);
        float y = -v + notch * v * v;
        float z = Mathf.Sin(u * Mathf.PI * 3f) * 0.035f * v;
        return new Vector3(x, y, z);
    }

    /// <summary>计算旋转体剖面上的单个顶点。</summary>
    private static Vector3 LathePoint(Vector2 profile, float angle)
    {
        return new Vector3(Mathf.Cos(angle) * profile.x, profile.y, Mathf.Sin(angle) * profile.x);
    }

    /// <summary>计算三角形面法线。</summary>
    private static Vector3 FaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        return normal.sqrMagnitude < 0.000001f ? Vector3.up : normal.normalized;
    }

    #endregion

    #region 内部网格构建器

    /// <summary>收集顶点、法线、UV 和三角形并生成最终网格。</summary>
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        /// <summary>添加使用默认 UV 的四边形面。</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            AddQuad(a, b, c, d, normal, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
        }

        /// <summary>添加指定 UV 的四边形面。</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            normals.Add(normal); normals.Add(normal); normals.Add(normal); normals.Add(normal);
            uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC); uvs.Add(uvD);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
        }

        /// <summary>添加指定法线和 UV 的三角形面。</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            normals.Add(normal); normals.Add(normal); normals.Add(normal);
            uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
        }

        /// <summary>生成带边界和切线的 Unity 网格。</summary>
        public Mesh Build(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }

    #endregion
}
