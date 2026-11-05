using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Procedural procedural mesh generator on the XZ plane with +Y normals.</summary>
    public static class MeshBuilder
    {
        public static Mesh Disc(float r, int seg)
        {
            seg = Mathf.Max(3, seg);
            Mesh mesh = new Mesh { name = "Disc" };

            Vector3[] vertices = new Vector3[seg + 1];
            Vector3[] normals = new Vector3[seg + 1];
            Vector2[] uvs = new Vector2[seg + 1];
            int[] triangles = new int[seg * 3];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < seg; i++)
            {
                float angle = i * 2f * Mathf.PI / seg;
                float x = Mathf.Sin(angle) * r;
                float z = Mathf.Cos(angle) * r;

                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = Vector3.up;
                uvs[i + 1] = new Vector2(x / (2f * r) + 0.5f, z / (2f * r) + 0.5f);

                int next = (i + 1) % seg;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = next + 1;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Ring(float rIn, float rOut, int seg)
        {
            seg = Mathf.Max(3, seg);
            Mesh mesh = new Mesh { name = "Ring" };

            Vector3[] vertices = new Vector3[seg * 2];
            Vector3[] normals = new Vector3[seg * 2];
            Vector2[] uvs = new Vector2[seg * 2];
            int[] triangles = new int[seg * 6];

            for (int i = 0; i < seg; i++)
            {
                float angle = i * 2f * Mathf.PI / seg;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);

                int idxIn = i * 2;
                int idxOut = i * 2 + 1;

                vertices[idxIn] = new Vector3(sin * rIn, 0f, cos * rIn);
                vertices[idxOut] = new Vector3(sin * rOut, 0f, cos * rOut);

                normals[idxIn] = Vector3.up;
                normals[idxOut] = Vector3.up;

                uvs[idxIn] = new Vector2(sin * 0.5f + 0.5f, cos * 0.5f + 0.5f);
                uvs[idxOut] = new Vector2(sin * 0.5f + 0.5f, cos * 0.5f + 0.5f);

                int next = (i + 1) % seg;
                int nextIn = next * 2;
                int nextOut = next * 2 + 1;

                int triOffset = i * 6;
                triangles[triOffset] = idxIn;
                triangles[triOffset + 1] = idxOut;
                triangles[triOffset + 2] = nextOut;

                triangles[triOffset + 3] = idxIn;
                triangles[triOffset + 4] = nextOut;
                triangles[triOffset + 5] = nextIn;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Fan(float r, float angleDeg, int seg)
        {
            angleDeg = Mathf.Clamp(angleDeg, 1f, 360f);
            seg = Mathf.Max(2, seg);
            Mesh mesh = new Mesh { name = "Fan" };

            Vector3[] vertices = new Vector3[seg + 2];
            Vector3[] normals = new Vector3[seg + 2];
            Vector2[] uvs = new Vector2[seg + 2];
            int[] triangles = new int[seg * 3];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 0.5f);

            float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                float x = Mathf.Sin(angle) * r;
                float z = Mathf.Cos(angle) * r;

                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = Vector3.up;
                uvs[i + 1] = new Vector2(x / (2f * r) + 0.5f, z / (2f * r) + 0.5f);
            }

            for (int i = 0; i < seg; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh FanOutline(float radius, float angleDeg, int segments)
        {
            float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3 origin = Vector3.zero;
            Vector3 left = new Vector3(-Mathf.Sin(halfAngle) * radius, 0f,
                Mathf.Cos(halfAngle) * radius);
            Vector3 right = new Vector3(Mathf.Sin(halfAngle) * radius, 0f,
                Mathf.Cos(halfAngle) * radius);
            AddLine(vertices, triangles, origin, left, 0.05f);
            AddLine(vertices, triangles, origin, right, 0.05f);
            Vector3 previous = left;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
                Vector3 next = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                AddLine(vertices, triangles, previous, next, 0.05f);
                previous = next;
            }
            return BuildOutline("FanOutline", vertices, triangles);
        }

        public static Mesh RectangleOutline(float width, float length)
        {
            float halfWidth = width * 0.5f;
            Vector3 a = new Vector3(-halfWidth, 0f, 0f);
            Vector3 b = new Vector3(halfWidth, 0f, 0f);
            Vector3 c = new Vector3(halfWidth, 0f, length);
            Vector3 d = new Vector3(-halfWidth, 0f, length);
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            AddLine(vertices, triangles, a, b, 0.05f);
            AddLine(vertices, triangles, b, c, 0.05f);
            AddLine(vertices, triangles, c, d, 0.05f);
            AddLine(vertices, triangles, d, a, 0.05f);
            return BuildOutline("RectangleOutline", vertices, triangles);
        }

        private static void AddLine(List<Vector3> vertices, List<int> triangles,
            Vector3 start, Vector3 end, float width)
        {
            Vector3 side = Vector3.Cross(Vector3.up, end - start).normalized * (width * 0.5f);
            int index = vertices.Count;
            vertices.Add(start - side);
            vertices.Add(start + side);
            vertices.Add(end + side);
            vertices.Add(end - side);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);
            triangles.Add(index);
            triangles.Add(index + 3);
            triangles.Add(index + 2);
        }

        private static Mesh BuildOutline(string name, List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh DashedRing(float rIn, float rOut, int dashes)
        {
            dashes = Mathf.Max(3, dashes);
            Mesh mesh = new Mesh { name = "DashedRing" };

            const int subSeg = 2;
            int totalQuads = dashes * subSeg;
            List<Vector3> verts = new List<Vector3>(totalQuads * 4);
            List<Vector3> norms = new List<Vector3>(totalQuads * 4);
            List<Vector2> uvs = new List<Vector2>(totalQuads * 4);
            List<int> tris = new List<int>(totalQuads * 6);

            float sliceAngle = 2f * Mathf.PI / dashes;
            float dashAngle = sliceAngle * 0.65f;

            for (int d = 0; d < dashes; d++)
            {
                float dashStart = d * sliceAngle;
                for (int s = 0; s < subSeg; s++)
                {
                    float a0 = dashStart + ((float)s / subSeg) * dashAngle;
                    float a1 = dashStart + ((float)(s + 1) / subSeg) * dashAngle;

                    int startIdx = verts.Count;

                    Vector3 pIn0 = new Vector3(Mathf.Sin(a0) * rIn, 0f, Mathf.Cos(a0) * rIn);
                    Vector3 pOut0 = new Vector3(Mathf.Sin(a0) * rOut, 0f, Mathf.Cos(a0) * rOut);
                    Vector3 pOut1 = new Vector3(Mathf.Sin(a1) * rOut, 0f, Mathf.Cos(a1) * rOut);
                    Vector3 pIn1 = new Vector3(Mathf.Sin(a1) * rIn, 0f, Mathf.Cos(a1) * rIn);

                    verts.Add(pIn0);
                    verts.Add(pOut0);
                    verts.Add(pOut1);
                    verts.Add(pIn1);

                    norms.Add(Vector3.up);
                    norms.Add(Vector3.up);
                    norms.Add(Vector3.up);
                    norms.Add(Vector3.up);

                    uvs.Add(new Vector2(0f, 0f));
                    uvs.Add(new Vector2(1f, 0f));
                    uvs.Add(new Vector2(1f, 1f));
                    uvs.Add(new Vector2(0f, 1f));

                    tris.Add(startIdx);
                    tris.Add(startIdx + 1);
                    tris.Add(startIdx + 2);

                    tris.Add(startIdx);
                    tris.Add(startIdx + 2);
                    tris.Add(startIdx + 3);
                }
            }

            mesh.vertices = verts.ToArray();
            mesh.normals = norms.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
